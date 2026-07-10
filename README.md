# Inkwell

Inkwell 是一个基于 [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) 打造的、可直接投产的智能体工厂：团队可以把它部署给内部成员协作使用，个人及 OPC（One Person Company，单人公司）也可以独立部署给自己使用。核心能力是让使用者在统一的桌面客户端中自助创建、配置、使用、共享属于自己或团队的 LLM Agent——涵盖多模型接入、Function Calling、Agent Skills、知识库（RAG）、长期记忆、多模态输入、调试与评测、版本管理，以及 OpenAI ChatCompletion / Responses API、AG-UI、A2A 四种协议的对外兼容调用。

Inkwell is a production-ready "agent factory" built on the [Microsoft Agent Framework](https://github.com/microsoft/agent-framework): teams can deploy it for internal collaborative use, and individuals or OPCs (One Person Companies) can self-host it for personal use. Its core capability is letting users create, configure, use, and share their own or their team's LLM agents through a unified desktop client — covering multi-model integration, function calling, Agent Skills, knowledge bases (RAG), long-term memory, multimodal input, debugging & evaluation, version management, and multi-protocol external invocation (OpenAI ChatCompletion / Responses API, AG-UI, A2A).

## 核心能力

- **Agent 全生命周期**：创建 / 配置（Instructions、模型、工具、Skills、知识库、长期记忆）/ 使用 / 共享 / 版本管理与回滚
- **多模型接入**：v1 落地 Azure OpenAI，预留 OpenAI / Claude / Qwen / 智谱等厂商接入位
- **多模态输入**：图片、语音（ASR 转写）、文档
- **调试与评测**：全链路 trace 可视化，调试样本可保存为评测集并重放
- **对外兼容调用**：独立 API Key 管理 + OpenAI ChatCompletion API / OpenAI Responses API / AG-UI 协议 / A2A 协议四种服务端兼容格式
- **部署形态**：团队集中部署（Docker Compose / AKS + Helm）与个人 / OPC 自部署两种场景并存

## 技术栈

- **客户端**：Electron · React 19 · Vite 6 · TypeScript · Zustand · `@xyflow/react`
- **后端**：.NET 10 · ASP.NET Core 10 · C# 14 · Microsoft Agent Framework
- **持久化**：EF Core 10（SQL Server 2025 / PostgreSQL 17，`IPersistenceProvider` 抽象可切换）
- **向量 / 缓存 / 队列 / 文件存储**：Qdrant · Redis · MinIO / Azure Blob，均通过独立 Provider 抽象可切换（dev 默认零外部依赖实现，prod 走 SDK-bound 实现）
- **可观测性**：OpenTelemetry .NET SDK → Tempo / Loki / Prometheus → Grafana（自托管）
- **部署**：Docker Compose（dev）· AKS + Helm（prod）
- **测试**：MSTest.Sdk（后端）· Vitest（前端）· Playwright（E2E）· Testcontainers（真实容器集成测试）

完整版本号、ADR 引用与模块边界见 [AGENTS.md](AGENTS.md)。

## 项目结构

后端按 Ports & Adapters 拓扑组织，物理上 17 个 csproj（`Inkwell.Abstractions` 端口层 + `Inkwell.Core` 业务层 + `providers/*` 12 个 Provider 适配器 + `Inkwell.WebApi` / `Inkwell.Worker` / `Inkwell.Migrator` 三个进程入口）；客户端位于 `src/app/`（尚未开工）。详见 [AGENTS.md §3.1](AGENTS.md#31-模块拓扑adr-017--adr-019--adr-024) 与 [docs/03-architecture/architecture.md](docs/03-architecture/architecture.md)。

## 文档入口

- AI 协作单一事实源：[AGENTS.md](AGENTS.md)
- 需求 / 架构 / 详细设计 / 测试 / 评审等全部阶段产出：[docs/](docs/)
- ADR 目录：[docs/03-architecture/adr/](docs/03-architecture/adr/)

## 当前状态

- **后端**：`Inkwell.Abstractions` / `Inkwell.Core` / 12 个 Provider 适配器 / `Inkwell.WebApi` / `Inkwell.Worker` / `Inkwell.Migrator` 已实现并通过 `dotnet build`；Postgres / Redis / MinIO / AzureBlob / Qdrant 均有真实 Testcontainers 集成测试覆盖。
- **详细设计（H3）**：`Inkwell.Abstractions` 全部端口已评审通过；`Inkwell.Core` 业务模块多数已评审通过，少数仍在草稿阶段。
- **客户端 / 部署栈**：Electron 客户端尚未开工；Docker Compose / Helm 部署配置尚未搭建。

## Roadmap

- ✅ H1 需求（reviewed）/ H2 架构（approved）
- ✅ H3 端口层详细设计（`Inkwell.Abstractions` 全部 7 份 HD 已 reviewed）
- ✅ H5 后端核心实现：`Inkwell.Abstractions` / `Inkwell.Core` / 12 个 Provider 适配器 / `Inkwell.WebApi` / `Inkwell.Worker` / `Inkwell.Migrator`，Postgres / Redis / MinIO / AzureBlob / Qdrant 均有真实容器集成测试
- 🚧 H3 剩余业务模块详细设计：`Models` / `Skills` 已起草待评审；`KnowledgeBase` / `Memory` / `PublicApi` / `Traces` / `Versioning` / `Multimodal` / `Health` 尚未起草
- 🚧 H4 测试用例设计（尚未开始）
- 🚧 `Inkwell.WebApi` ↔ `Inkwell.Worker` 跨服务集成用例（enqueue → consume → ack，覆盖知识库入库、DurableTask 场景）
- ⬜ Electron + React 客户端（尚未开工）
- ⬜ Docker Compose（dev）/ Helm Chart（prod）部署栈（尚未搭建）

明确推迟到 v2、v1 不做（详见 [AGENTS.md §3.3](AGENTS.md#33-禁区v1-显式不做--不引入)）：

- 触发器（Triggers）与多 Agent 协作编排（Orchestrations）+ 编排画布
- 客户端 i18n / RBAC / 多租户 / OAuth2 / SSO / 自助注册
- Azure Key Vault、Redis Cluster / Sentinel HA、跨 region active-passive

## 许可证

[MIT License](LICENSE)
