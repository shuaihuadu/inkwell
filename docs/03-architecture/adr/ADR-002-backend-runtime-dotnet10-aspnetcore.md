---
id: ADR-002-backend-runtime-dotnet10-aspnetcore
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [ Inkwell ]
created: 2026-05-08
updated: 2026-05-08
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
downstream:
  - ADR-003
  - ADR-004
  - ADR-007
  - ADR-011
  - ADR-012
  - ADR-013
---

# ADR-002 后端运行时：.NET 10 + ASP.NET Core

## 上下文

Inkwell 后端需要承担 [§3.1 模块拓扑](../../01-requirements/repo-impact-map.md) 中的 17 个 `Inkwell.*` 模块，覆盖 REQ-001 ~ REQ-017 + NFR-001 ~ NFR-006。后端必须同时满足：

- [REQ-007 工具调用](../../01-requirements/requirements.md) / [REQ-008 Skills](../../01-requirements/requirements.md) / [REQ-012 编排](../../01-requirements/requirements.md) / [REQ-014 trace](../../01-requirements/requirements.md) 都对接 Microsoft Agent Framework（[ADR-003](./ADR-003-agent-engine-microsoft-agent-framework.md)），该框架的一等公民 SDK 是 .NET。
- [REQ-016 多模态](../../01-requirements/requirements.md) 需要后端持有 Azure Speech 凭据并代理请求。
- [REQ-013 公开 API](../../01-requirements/requirements.md) 需要标准 HTTP 服务器、Token 鉴权 middleware、rate limit。
- [NFR-005 对话历史](../../01-requirements/requirements.md) 需要持久化层与 [ADR-004 EF Core Provider](./ADR-004-data-store-provider-switchable-ef-core.md) 配套。
- [Q-A5 部署形态](../open-questions-arch.md) 锁定 Docker Compose（dev） + AKS（prod），必须可容器化。

第三步反问 Q-A2 用户答".NET 10 + ASP.NET Core"。

## 决策

**后端运行时锁定为：.NET 10 + ASP.NET Core 10，目标框架 `net10.0`，C# 14 语言版本（与 .NET 10 同步发布，[microsoft/agent-framework global.json](../../../../../microsoft/agent-framework/dotnet/global.json) 已锁 `10.0.200`）。**

MAF是微软官方发布的Agent框架，https://github.com/microsoft/agent-framework
源码也在工作区，agent-framework下面，我们主要关注dotnet的实现和示例，可够后续开发参考

- API 层：[Minimal API](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis) + [ASP.NET Core Middleware Pipeline](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/)。
- 异步 / 流式：原生 `IAsyncEnumerable<T>` + `System.Text.Json` 异步序列化（用于 [ADR-012 AG-UI Protocol](./ADR-012-client-server-protocol-rest-agui.md) 事件流）。
- 配置：[Microsoft.Extensions.Configuration](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration) + [User Secrets / Azure Key Vault](https://learn.microsoft.com/aspnet/core/security/key-vault-configuration)（详见 [OQ-A006](../open-questions-arch.md)）。
- DI：[Microsoft.Extensions.DependencyInjection](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)，[ADR-004](./ADR-004-data-store-provider-switchable-ef-core.md) 三种 Provider 通过 `services.AddInkwellDbContext(provider: "PostgreSQL")` 显式注入。
- 测试：[xUnit](https://xunit.net/) + [Microsoft.AspNetCore.Mvc.Testing](https://learn.microsoft.com/aspnet/core/test/integration-tests)（待 [OQ-A007](../open-questions-arch.md) 接受默认值后正式锁）。

## 备选项

### 备选 A：Python (FastAPI) + LangGraph

- **放弃理由**：(1) 与已锁的 [ADR-003 Microsoft Agent Framework](./ADR-003-agent-engine-microsoft-agent-framework.md) 不在同一生态——MAF 的 .NET 实现成熟度（含 [Microsoft.Agents.AI.Hosting.AGUI.AspNetCore](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.Hosting.AGUI.AspNetCore/) / [Microsoft.Agents.AI.Workflows](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.Workflows/) / [Microsoft.Agents.AI.DurableTask](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/)）目前显著优于 Python 实现。(2) 团队 .NET 经验大于 Python；FastAPI 路径需要重学异步 + 类型生态。(3) Python 在多 worker 进程下做"AG-UI 主连接持有 + Run resume"的复杂度高于 ASP.NET Core 单进程多副本模型。

### 备选 B：Node.js (NestJS) + LangChain.js

- **放弃理由**：(1) MAF 的 Node.js 实现还在 alpha 阶段，与 ADR-003 不匹配。(2) Node.js 单线程模型对 [REQ-009 知识库 RAG](../../01-requirements/requirements.md) 大文件解析性能敏感（PDF / Office 解析普遍是 IO + CPU 密集）。(3) 与 [Microsoft.Agents.AI.DurableTask](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/) 持久工作流编排无对应 Node 实现。

### 备选 C：Java (Spring Boot) + Spring AI

- **放弃理由**：(1) 与团队技术栈不符；(2) Spring AI 对 OpenAI / Azure OpenAI 的工具调用支持落后于 MAF 半年以上；(3) JVM 容器镜像偏大（≥ 200 MB），AKS 部署成本高于 .NET 自包含发布（≈ 80 MB）。

## 后果

### 正面

- 与 [ADR-003 Microsoft Agent Framework](./ADR-003-agent-engine-microsoft-agent-framework.md) 同生态，可直接 `<PackageReference Include="Microsoft.Agents.AI" />` 引用。
- ASP.NET Core 自带 [SignalR](https://learn.microsoft.com/aspnet/core/signalr/introduction) / [SSE 支持](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/responses) / [Rate Limiter](https://learn.microsoft.com/aspnet/core/performance/rate-limit) / [Authentication](https://learn.microsoft.com/aspnet/core/security/authentication/)，REQ-013 公开 API + ADR-012 AG-UI 协议路径短。
- C# 14 + .NET 10 的 nullable reference types + record + 扩展成员（extension members）+ pattern matching 直接降低 H3 详细设计的"边界值校验"代码量。
- 容器镜像 ≈ 80 MB（.NET 10 self-contained），AKS Pod 启动快，符合 [Q-A5](../open-questions-arch.md) 部署诉求。

### 负面

- .NET 10 已 GA（2025-11 发布，距今 6 个月），生态多数主流库已声明 net10.0 支持，但少数尾部第三方库（如部分 PDF / Office 解析包）可能仍只声明 net8.0 LTS — 评估时逐库验证，必要时锁 net8.0 兼容包。
- 团队若有非 .NET 经验工程师，需要 ramp-up（Pattern Matching / Source Generators / `IAsyncEnumerable` 学习曲线）。

### 中性

- 编译期类型检查严格 → 减少运行时错误，但首次实现节奏比 Python 慢。
- ASP.NET Core 的 minimal API 需要团队熟悉与传统 MVC controller 不同的端点组织模式。

## 状态

- **状态**：accepted
- **首次发布**：2026-05-08
- **关联**：supersedes 无；下游 [ADR-003 ~ ADR-013](./README.md)（除 ADR-001 / ADR-005 / ADR-014）
- **置信度**：high（与 ADR-003 Microsoft Agent Framework .NET 一等公民地位强配套）
