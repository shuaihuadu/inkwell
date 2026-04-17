# Inkwell Docker 部署指南

## 前置条件

- Docker Desktop（Windows / macOS）或 Docker Engine（Linux）
- Docker Compose V2（Docker Desktop 自带）
- Azure OpenAI 资源（可选，不配置时 Agent 功能不可用，但服务能正常启动）

## 服务架构

```
┌─────────────┐     ┌──────────────┐     ┌──────────────────┐
│   Webapp    │────▶│   WebApi     │────▶│  SQL Server 2025 │
│  :3000      │     │  :5000       │     │  :1433           │
│  (nginx)    │     │  (.NET 10)   │     │  (可选)          │
└─────────────┘     └──────┬───────┘     └──────────────────┘
                          │ OTLP
                    ┌──────┴───────┐     ┌──────────────────┐
                    │  Aspire      │     │  DTS Emulator    │
                    │  Dashboard   │     │  :8080 API       │
                    │  :18888      │     │  :8082 管理面板  │
                    └──────────────┘     └──────────────────┘
                    ┌──────────────┐     ┌──────────────────┐
                    │  A2A Server  │     │  DurableHost     │
                    │  :5100       │     │  (后台 Worker)   │
                    │  (.NET 10)   │     └──────────────────┘
                    └──────────────┘
```

## 服务说明

| 服务             | 容器名                   | 端口        | 说明                                        |
| ---------------- | ------------------------ | ----------- | ------------------------------------------- |
| WebApi           | inkwell-webapi           | 5000        | 主 API 服务（AGUI、会话、知识库）           |
| Webapp           | inkwell-webapp           | 3000        | React SPA 前端（nginx 托管 + API 反向代理） |
| A2A Server       | inkwell-a2a-server       | 5100        | Agent-to-Agent 协议服务                     |
| DTS Emulator     | inkwell-dts-emulator     | 8080/8082   | DurableTask Scheduler 本地模拟器            |
| DurableHost      | inkwell-durable-host     | -           | DurableTask 后台 Worker                     |
| SQL Server       | inkwell-sqlserver        | 1433        | SQL Server 2025 数据库                      |
| Aspire Dashboard | inkwell-aspire-dashboard | 18888/18889 | 可观测性面板（日志、追踪、指标）            |

## 快速开始

### 1. 配置环境变量

```bash
cd docker
cp .env.example .env
```

编辑 `.env` 文件，填入 Azure OpenAI 配置：

```env
# Azure OpenAI 配置
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your-api-key-here
AZURE_OPENAI_DEPLOYMENT=gpt-4o
AZURE_OPENAI_SECONDARY_DEPLOYMENT=gpt-4o-mini
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=text-embedding-3-large

# SQL Server
MSSQL_SA_PASSWORD=Inkwell@2026!
```

环境变量说明：

| 变量 | 用途 | 默认值 |
|------|------|--------|
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI 服务端点 | （必填） |
| `AZURE_OPENAI_API_KEY` | Azure OpenAI API 密钥 | （必填） |
| `AZURE_OPENAI_DEPLOYMENT` | 主模型部署名（写作、审核） | `gpt-4o` |
| `AZURE_OPENAI_SECONDARY_DEPLOYMENT` | 辅助模型部署名（分析、翻译、协调） | `gpt-4o-mini` |
| `AZURE_OPENAI_EMBEDDING_DEPLOYMENT` | Embedding 模型部署名（知识库、记忆） | `text-embedding-3-large` |
| `MSSQL_SA_PASSWORD` | SQL Server SA 密码 | `Inkwell@2026!` |

> 不配置 Azure OpenAI 时，WebApi 仍可启动，但 Agent 对话功能不可用。

### 2. 启动服务

#### 启动全部服务（默认）

```bash
cd docker
docker compose up -d
```

#### 按需启动部分服务

```bash
# 仅核心服务（WebApi + Webapp + A2A）
docker compose up -d webapi webapp a2a-server

# 最小化（前端 + 后端）
docker compose up -d webapi webapp

# 核心 + SQL Server
docker compose up -d webapi webapp a2a-server sqlserver

# 核心 + 可观测性
docker compose up -d webapi webapp a2a-server aspire-dashboard

# 核心 + DurableTask
docker compose up -d webapi webapp a2a-server dts-emulator durable-host
```

> Aspire Dashboard 接收 WebApi 通过 OTLP 协议发送的日志、追踪和指标数据。
> WebApi 已预配置 `OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889`，自动生效。

### 3. 访问服务

| 服务             | 地址                         |
| ---------------- | ---------------------------- |
| 前端界面         | http://localhost:3000        |
| WebApi           | http://localhost:5000        |
| WebApi 健康检查  | http://localhost:5000/health |
| A2A Server       | http://localhost:5100        |
| DTS 管理面板     | http://localhost:8082        |
| Aspire Dashboard | http://localhost:18888       |

## 常用操作

### 重新构建并启动（代码修改后）

```bash
cd docker
docker compose up -d --build
```

### 查看服务日志

```bash
# 查看所有服务日志
docker compose logs -f

# 查看单个服务日志
docker logs inkwell-webapi -f
docker logs inkwell-durable-host -f
docker logs inkwell-dts-emulator -f
docker logs inkwell-sqlserver -f
```

### 查看服务状态

```bash
cd docker
docker compose ps
```

### 停止所有服务

```bash
cd docker
docker compose down
```

### 停止并清除数据卷（SQL Server 数据会丢失）

```bash
cd docker
docker compose down -v
```

### 单独重启某个服务

```bash
docker compose restart webapi
docker compose restart webapp
```

## 镜像说明

| 镜像                                               | 用途                                      |
| -------------------------------------------------- | ----------------------------------------- |
| `mcr.microsoft.com/dotnet/sdk:10.0`                | .NET 10 构建阶段                          |
| `mcr.microsoft.com/dotnet/aspnet:10.0`             | WebApi / A2A Server 运行时                |
| `mcr.microsoft.com/dotnet/runtime:10.0`            | DurableHost 运行时                        |
| `node:20-slim`                                     | Webapp 构建阶段                           |
| `nginx:alpine`                                     | Webapp 运行时（静态文件托管）             |
| `mcr.microsoft.com/dts/dts-emulator:latest`        | DurableTask Scheduler 本地模拟器          |
| `mcr.microsoft.com/mssql/server:2025-latest`       | SQL Server 2025 Developer Edition         |
| `mcr.microsoft.com/dotnet/aspire-dashboard:latest` | Aspire Dashboard（可观测性面板，v13.2.0） |

## 网络与代理

所有容器在 `inkwell-network` 桥接网络中通信。

Webapp（nginx）配置了反向代理，将 `/api/*` 请求转发到 `webapi:5000`，前端通过相对路径访问 API，无需跨域。

本地开发（不用 Docker）时，前端默认连接 `http://localhost:5000`，通过环境变量 `VITE_API_BASE` 控制。

## 数据持久化

- **SQL Server**：数据存储在 Docker volume `sqlserver-data` 中，`docker compose down` 不会丢失数据，`docker compose down -v` 会清除。
- **DTS Emulator**：内存存储，重启后数据丢失（仅用于开发调试）。
- **WebApi（InMemory 模式）**：默认使用 InMemory 数据库，重启后数据丢失。配置 SQL Server 连接字符串后可持久化。

## 故障排查

### WebApi 启动失败

```bash
docker logs inkwell-webapi
```

常见原因：
- Azure OpenAI Endpoint/ApiKey 未配置或无效
- 端口 5000 被占用

### DTS Emulator 健康检查失败

```bash
docker logs inkwell-dts-emulator
docker exec inkwell-dts-emulator bash -c "echo > /dev/tcp/localhost/8080 && echo OK"
```

### DurableHost 无法连接 DTS

```bash
docker logs inkwell-durable-host
```

确认 DTS Emulator 健康后 DurableHost 才会启动（`depends_on` + `condition: service_healthy`）。

### SQL Server 连接问题

```bash
# 检查 SQL Server 状态
docker logs inkwell-sqlserver

# 容器内测试连接
docker exec inkwell-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Inkwell@2026!" -C -Q "SELECT @@VERSION"
```

### 清理并重建所有镜像

```bash
cd docker
docker compose down
docker compose build --no-cache
docker compose up -d
```
