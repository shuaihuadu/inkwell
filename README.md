# Inkwell

Inkwell 是一个基于 [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) 打造的、可直接投产的智能体工厂：团队可以把它部署给内部成员协作使用，个人及 OPC（One Person Company，单人公司）也可以独立部署给自己使用。核心能力是让使用者在统一的桌面客户端中自助创建、配置、使用、共享属于自己或团队的 LLM Agent——涵盖多模型接入、Function Calling、Agent Skills、知识库（RAG）、长期记忆、多模态输入、调试与评测、版本管理，以及 OpenAI ChatCompletion / Responses API、AG-UI、A2A 四种协议的对外兼容调用。

Inkwell is a production-ready "agent factory" built on the [Microsoft Agent Framework](https://github.com/microsoft/agent-framework): teams can deploy it for internal collaborative use, and individuals or OPCs (One Person Companies) can self-host it for personal use. Its core capability is letting users create, configure, use, and share their own or their team's LLM agents through a unified desktop client — covering multi-model integration, function calling, Agent Skills, knowledge bases (RAG), long-term memory, multimodal input, debugging & evaluation, version management, and multi-protocol external invocation (OpenAI ChatCompletion / Responses API, AG-UI, A2A).

## 核心能力

- **Agent 全生命周期**：创建 / 配置（Instructions、模型、工具、Skills、知识库、长期记忆）/ 使用 / 共享 / 版本管理与回滚
- **多模型接入**：v1 落地 Azure OpenAI，预留 OpenAI / Claude / Qwen / 智谱等厂商接入位
- **多模态输入**：图片、语音（ASR 转写）、文档
- **调试与评测**：全链路 trace 可视化，调试样本可保存为评测集并重放
- **对外兼容调用**：独立 API Key 管理 + OpenAI ChatCompletion API / OpenAI Responses API / AG-UI 协议 / A2A 协议四种服务端兼容格式
- **部署形态**：团队集中部署（Aspire 本地编排 / AKS + Helm 生产部署）与个人 / OPC 自部署两种场景并存

## 技术栈

- **客户端**：Electron · React 19 · Vite 6 · TypeScript · Zustand · `@xyflow/react`
- **后端**：.NET 10 · ASP.NET Core 10 · C# 14 · Microsoft Agent Framework
- **持久化**：EF Core 10（SQL Server 2025 / PostgreSQL 17，`IPersistenceProvider` 抽象可切换）
- **向量 / 缓存 / 队列 / 文件存储**：Qdrant · Redis · MinIO / Azure Blob，均通过独立 Provider 抽象可切换（dev 默认零外部依赖实现，prod 走 SDK-bound 实现）
- **可观测性**：OpenTelemetry .NET SDK → Tempo / Loki / Prometheus → Grafana（自托管）
- **部署**：Aspire AppHost（dev）· AKS + Helm（prod）
- **测试**：MSTest.Sdk（后端）· Vitest（前端）· Playwright（E2E）· Testcontainers（真实容器集成测试）

完整版本号、ADR 引用与模块边界见 [AGENTS.md](AGENTS.md)。

## 项目结构

后端按 Ports & Adapters 拓扑组织，物理上 17 个产品 csproj（`Inkwell.Abstractions` 端口层 + `Inkwell.Core` 业务层 + `providers/*` 12 个 Provider 适配器 + `Inkwell.WebApi` / `Inkwell.Worker` / `Inkwell.Migrator` 三个进程入口），另有 1 个仅用于本地编排的 `Inkwell.AppHost`；客户端位于 `src/app/`（尚未开工）。详见 [AGENTS.md §3.1](AGENTS.md#31-模块拓扑adr-017--adr-019--adr-024) 与 [docs/03-architecture/architecture.md](docs/03-architecture/architecture.md)。

## 文档入口

- AI 协作单一事实源：[AGENTS.md](AGENTS.md)
- 需求 / 架构 / 详细设计 / 测试 / 评审等全部阶段产出：[docs/](docs/)
- ADR 目录：[docs/03-architecture/adr/](docs/03-architecture/adr/)

## 当前状态

- **后端**：`Inkwell.Abstractions` / `Inkwell.Core` / 12 个 Provider 适配器 / `Inkwell.WebApi` / `Inkwell.Worker` / `Inkwell.Migrator` 已实现并通过 `dotnet build`；Postgres / Redis / MinIO / AzureBlob / Qdrant 均有真实 Testcontainers 集成测试覆盖。
- **详细设计（H3）**：`Inkwell.Abstractions` 全部端口已评审通过；`Inkwell.Core` 业务模块多数已评审通过，少数仍在草稿阶段。
- **客户端 / 部署栈**：Electron 客户端已形成可运行开发基线；Aspire AppHost 已编排 Electron desktop、独立视觉设计原型、PostgreSQL 17 / SQL Server 2025 / pgAdmin / 双 Provider Migrator / WebApi / Worker，Helm 生产部署配置尚未搭建。

## Roadmap

- ✅ H1 需求（reviewed）/ H2 架构（approved）
- ✅ H3 端口层详细设计（`Inkwell.Abstractions` 全部 7 份 HD 已 reviewed）
- ✅ H5 后端核心实现：`Inkwell.Abstractions` / `Inkwell.Core` / 12 个 Provider 适配器 / `Inkwell.WebApi` / `Inkwell.Worker` / `Inkwell.Migrator`，Postgres / Redis / MinIO / AzureBlob / Qdrant 均有真实容器集成测试
- 🚧 H3 剩余业务模块详细设计：`Models` / `Skills` 已起草待评审；`KnowledgeBase` / `Memory` / `PublicApi` / `Traces` / `Versioning` / `Multimodal` / `Health` 尚未起草
- 🚧 H4 测试用例设计（尚未开始）
- 🚧 `Inkwell.WebApi` ↔ `Inkwell.Worker` 跨服务集成用例（enqueue → consume → ack，覆盖知识库入库、DurableTask 场景）
- 🚧 Electron + React 客户端（已具备登录、Agent 工作区与聊天开发基线，其他功能持续实现）
- 🚧 Aspire AppHost（dev）已完成首批编排；Helm Chart（prod）尚未搭建

## 本地启动

本机需安装 .NET 10 SDK 和 Docker 兼容容器运行时。启动 AppHost：

```bash
dotnet run --project src/core/Inkwell.AppHost
```

Aspire Dashboard 会显示 Electron desktop、视觉设计原型、PostgreSQL 17、SQL Server 2025、pgAdmin、LiteLLM、两个 Provider 各自的 Migrator、WebApi 和 Worker。视觉设计原型作为独立 Vite 资源启动，不等待后端资源；Electron desktop 通过 `electron-vite dev` 启动，在 WebApi 就绪后打开原生窗口，并由 AppHost 注入 WebApi 地址。两个 Migrator 分别应用独立的 EF Core migration；WebApi 与 Worker 仅在两者均成功完成 Migration + Seed 且 LiteLLM 可用后启动，当前业务运行时仍以 PostgreSQL 为主库。

本地端口集中配置在 `src/core/Inkwell.AppHost/appsettings.json`，默认使用视觉设计原型 `6800`、WebApi `6801`、pgAdmin `6802`、SQL Server `6803`、LiteLLM `6804`；可通过对应的 `Ports__Prototype`、`Ports__WebApi` 等环境变量覆盖。Electron desktop 是原生进程，不占用固定的外部 HTTP 端口。访问 `http://localhost:6800` 打开视觉设计原型，`http://localhost:6801` 会跳转到 Scalar API 文档页面，`http://localhost:6802` 提供 pgAdmin 数据库管理页面；健康检查位于 `/healthz`，OpenAPI 文档位于 `/openapi/v1.json`。两套物理数据库均名为 `Inkwell`；Aspire 中以 `postgres-database` / `sqlserver-database` 两个唯一资源标识区分。PostgreSQL 使用小写 snake_case 物理标识符和 `jsonb`，SQL Server 使用 PascalCase 物理标识符和 SQL Server 2025 原生 `json`，两套 adapter 各自维护独立 migration。

SQL Server 2025 Linux 容器仅官方支持 x86-64。在 Apple Silicon 上，Docker Desktop 可能通过 amd64 模拟运行该镜像，但此路径不属于 Microsoft 官方支持范围。SQL Server 2025 的原生向量能力不改变 v1 的存储边界：关系数据可使用 PostgreSQL 或 SQL Server，向量数据仍由 Qdrant Provider 承载。

明确推迟到 v2、v1 不做（详见 [AGENTS.md §3.3](AGENTS.md#33-禁区v1-显式不做--不引入)）：

- 触发器（Triggers）与多 Agent 协作编排（Orchestrations）+ 编排画布
- 客户端 i18n / RBAC / 多租户 / OAuth2 / SSO / 自助注册
- Azure Key Vault、Redis Cluster / Sentinel HA、跨 region active-passive

## 许可证

[MIT License](LICENSE)
