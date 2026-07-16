# Inkwell

Inkwell 是一个基于 [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) 打造的智能体工作空间：团队可以把它部署给内部成员协作使用，个人及 OPC（One Person Company，单人公司）也可以独立部署给自己使用。

Inkwell is an agent workspace built on the [Microsoft Agent Framework](https://github.com/microsoft/agent-framework): teams can deploy it for internal collaborative use, and individuals or OPCs (One Person Companies) can self-host it for personal use.

## 核心能力

- **Agent 管理**：创建、配置和管理 Agent
- **模型接入**：统一管理并选用不同模型
- **Agent 对话**：通过桌面客户端与 Agent 交互并查看会话记录
- **版本管理**：发布、查看和回滚 Agent 版本
- **本地运行**：使用 Aspire 一键启动完整开发环境

## 技术栈

- **客户端**：Electron · React · TypeScript
- **后端**：.NET · ASP.NET Core · Microsoft Agent Framework
- **数据与基础设施**：EF Core · PostgreSQL / SQL Server · Qdrant · Redis · MinIO / Azure Blob
- **开发与部署**：Aspire · Docker · Kubernetes · Helm
- **测试**：MSTest · Vitest · Playwright · Testcontainers

## 本地启动

本机需安装 .NET 10 SDK 和 Docker Desktop。运行 Aspire AppHost 即可启动本地开发环境：

```bash
git clone https://github.com/shuaihuadu/inkwell.git
cd inkwell
dotnet run --project src/core/Inkwell.AppHost
```

启动后可通过 Aspire Dashboard 查看和管理各项本地服务。默认管理员账号和密码均为 `admin`。

常用本地地址：

- Aspire Dashboard：<https://localhost:15888>
- 视觉原型设计：<http://localhost:6800>

端口配置位于 `src/core/Inkwell.AppHost/appsettings.json`。

## Roadmap

- ✅ H1 需求（reviewed）/ H2 架构（approved）
- ✅ H3 端口层详细设计（`Inkwell.Abstractions` 全部 7 份 HD 已 reviewed）
- ✅ H5 后端核心实现：`Inkwell.Abstractions` / `Inkwell.Core` / 12 个 Provider 适配器 / `Inkwell.WebApi` / `Inkwell.Worker` / `Inkwell.Migrator`，Postgres / Redis / MinIO / AzureBlob / Qdrant 均有真实容器集成测试
- 🚧 H3 剩余业务模块详细设计：`Models` / `Skills` 已起草待评审；`KnowledgeBase` / `Memory` / `PublicApi` / `Traces` / `Versioning` / `Multimodal` / `Health` 尚未起草
- 🚧 H4 测试用例设计（尚未开始）
- 🚧 `Inkwell.WebApi` ↔ `Inkwell.Worker` 跨服务集成用例（enqueue → consume → ack，覆盖知识库入库、DurableTask 场景）
- 🚧 Electron + React 客户端（已具备登录、Agent 工作区与聊天开发基线，其他功能持续实现）
- 🚧 Aspire AppHost（dev）已完成首批编排；Helm Chart（prod）尚未搭建

## 许可证

[MIT License](LICENSE)
