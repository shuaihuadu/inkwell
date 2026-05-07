---
id: architecture-custom-agent
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers:
  - name: self-review
    decision: approved
    date: 2026-05-07
created: 2026-05-07
updated: 2026-05-07
upstream:
  - requirements-custom-agent
  - ui-spec-custom-agent
  - acceptance-criteria-custom-agent
  - repo-impact-map-custom-agent
downstream: []
---

# 自定义 Agent 功能 · H2 架构说明

> 本文配合 [tech-selection.md](./tech-selection.md)（每条选型六字段）/ [risk-analysis.md](./risk-analysis.md)（RISK-NNN 缓解）/ [adr/](./adr/)（关键决策 ADR）/ [open-questions-arch.md](./open-questions-arch.md)（H2 待澄清）一并阅读。

## 0. 上游约束概要

| 约束 | 来源 | 锁定值 |
| --- | --- | --- |
| 不内置登录 | [requirements.md ND-009 / R7](../01-requirements/requirements.md) | 已锁定（dev 期 Mock + 生产期 OIDC 待平台确认） |
| 不做内容审核 | [requirements.md ND-008 / E4 / R3](../01-requirements/requirements.md) | 已锁定 |
| 不做运行时观测 | [requirements.md ND-007](../01-requirements/requirements.md) | 已锁定（仅出 OTel signals 给平台运维消费） |
| MVP 规模 ≤ 100 同时在线 | [requirements.md NFR-002](../01-requirements/requirements.md) | 已锁定 |
| MVP 模型固定 `gpt-4.1` | [requirements.md REQ-005](../01-requirements/requirements.md) | 已锁定（走 Azure OpenAI） |
| 试运行下半屏本期 A 同页 React 组件 | [OQ-030](../01-requirements/open-questions.md) / [评审记录 D-1](../07-reviews/2026-05-07-h1-prototype-custom-agent.md) | 已锁定（vNext 视平台聊天页面成熟度切 iframe） |
| Tool 首批仅 T-1 / T-3 | [requirements.md AC-011-1 / ND-013](../01-requirements/requirements.md) | 已锁定（T-1 本期 Mock，接口预留） |
| 登录 / 模型 / 搜索 GAP-003~005 答复 | 用户 2026-05-07 H2 启动反问 | dev mock / Azure OpenAI / Mock 搜索 |

## 1. 总体架构

### 1.1 架构图（前后端分离 + 双协议 + 多 provider）

```
┌──────────────────────────── 浏览器 ────────────────────────────┐
│  React 18 + TypeScript + AntD 5（AntD Pro 后台模板视觉骨架）   │
│  ┌──────────────────────────┐  ┌─────────────────────────────┐ │
│  │ AppShell / 路由守卫      │  │ P2 试运行下半屏              │ │
│  │ P1 列表 / P2 编辑 / P3   │  │ AG-UI 客户端（@ag-ui/client │ │
│  │ Skill 抽屉 / P4 删除弹层 │  │ 或 CopilotKit React SDK）   │ │
│  │ ── REST(JSON, axios) ──> │  │ ── AG-UI(SSE) ──>          │ │
│  └──────────────────────────┘  └─────────────────────────────┘ │
└────────────────────────────────────────────────────────────────┘
            │ HTTPS                              │ HTTPS + SSE
            ▼                                    ▼
┌──────────────────────────── 后端 ──────────────────────────────┐
│  ASP.NET Core Web API（.NET 9 LTS+，Minimal API + Controllers）│
│  ┌────────────────────────┐  ┌──────────────────────────────┐  │
│  │ REST 控制器：           │  │ AG-UI 端点（POST /agui/run） │  │
│  │ /api/agents · skills · │  │ 经 MAF + AG-UI 1st-party     │  │
│  │ tools · uploads        │  │ integration 输出 16 种事件流 │  │
│  └─────────┬──────────────┘  └────────┬─────────────────────┘  │
│            │                          │                        │
│            ▼                          ▼                        │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ 应用服务层（Application Service）                         │  │
│  │   AgentService · SkillService · ToolRegistry · ChatOrch  │  │
│  │   IPlatformAuthenticator（登录抽象，dev/prod 双实现）     │  │
│  └────────┬─────────────────────────────────────────────────┘  │
│           │                                                    │
│           ▼                                                    │
│  ┌────────────────────────────────────────────────────────┐    │
│  │ 基础设施抽象（IRepository / IQueue / ICache /          │    │
│  │ IObjectStorage / IWebSearchTool）                       │    │
│  │ Provider 切换：appsettings:Storage:Provider=...        │    │
│  └─┬────────┬────────┬──────────┬─────────────┬───────────┘    │
└────┼────────┼────────┼──────────┼─────────────┼────────────────┘
     ▼        ▼        ▼          ▼             ▼
   EF Core   Queue   Cache     ObjectStg    WebSearch
   ┌────┐  ┌─────┐  ┌─────┐    ┌─────┐    ┌─────────┐
   │ Mem│  │ Mem │  │ Mem │    │Local│    │  Mock   │
   │ SQL│  │Redis│  │Redis│    │Azure│    │（Bing/  │
   │PG  │  │     │  │     │    │Blob │    │ 自建待   │
   └────┘  └─────┘  └─────┘    └─────┘    │ GAP-005）│
                                          └─────────┘
                                          
   ┌─ 外部依赖 ───────────────────────────────────────┐
   │ Azure OpenAI（gpt-4.1，region 待 OQ-A-006 确定） │
   │ 平台登录子系统（OIDC/SAML 待 GAP-003 确定）       │
   │ agentskills.io 规范（外部 spec）                  │
   └──────────────────────────────────────────────────┘
```

### 1.2 关键设计原则

1. **协议分裂**：数据面用 REST（CRUD / 文件上传 / Skill 解析），对话面用 AG-UI（事件流 / 工具调用 / 中间状态）。理由见 [ADR-002](./adr/ADR-002-agui-as-chat-protocol.md)。
2. **多 provider 抽象（M1 模式）**：DB / 队列 / 缓存 / 对象存储四套均运行时按配置切换；理由与代价见 [ADR-003](./adr/ADR-003-multi-provider-m1.md)。
3. **延迟决策**：登录子系统与联网搜索本期 Mock + 接口预留，**不**强行落地未确定的依赖；理由见 [ADR-004](./adr/ADR-004-platform-login-dev-mock.md) / [ADR-005](./adr/ADR-005-web-search-mock-first.md)。
4. **部署形态分裂**：开发用 docker-compose 速度优先，生产用 K8s + Helm 稳定优先。理由见 [ADR-006](./adr/ADR-006-deployment-split.md)。
5. **不越界**：本特性内部**不**实现可观测性后端、不做内容审核、不内置登录——这些边界由 ND-007/008/009 锁定，本架构遵守。

## 2. 前端架构

### 2.1 技术栈

| 层级 | 选型 | 约束来源 |
| --- | --- | --- |
| 框架 | React 18 | 用户偏好 1（硬约束） |
| 语言 | TypeScript 5.x（strict mode） | 用户偏好 1 |
| UI 库 | Ant Design 5（含 Pro Components） | 用户偏好 1 + [ui-spec.md 第 0 节](../01-requirements/ui-spec.md) "AntD Pro 后台模板视觉骨架" |
| 路由 | React Router v6 | 业内主流，AntD Pro 默认 |
| 状态管理 | Zustand（简单全局态）+ React Query / TanStack Query（服务器态） | H3 阶段细化；本节仅声明分层 |
| 表单 | AntD Form + Zod（Schema 校验） | AntD Form 已与 ui-spec 对齐 |
| HTTP | axios（拦截器统一错误 / 401 跳 P5） | 业内主流 |
| AG-UI 客户端 | `@ag-ui/client`（HttpAgent + SSE）+ React 自建对话视图，**不**强绑 CopilotKit | 见 [ADR-002](./adr/ADR-002-agui-as-chat-protocol.md)；AntD 已提供完整 UI 套件，引入 CopilotKit UI 会与 AntD 冲突 |
| 构建 | Vite 5 | 主流；具体由 H3 决定 |
| 语言 / 主题 | i18n: zh-CN / en-US；ConfigProvider 主题 | NFR-003 |
| 无障碍 | WCAG 2.1 AA | NFR-003 |

### 2.2 模块划分

```
src/
├── app/             # AppShell / 路由 / 全局守卫 / ConfigProvider
├── pages/
│   ├── agents/      # P1 列表 / P2 编辑 / P4 删除弹层
│   ├── skills/      # P3 Skill 抽屉
│   └── auth/        # P5 未登录跳转
├── features/
│   ├── agent-editor/     # P2 三栏编辑器 + Tab 容器
│   ├── instructions/     # Instructions 子区
│   ├── skill-tab/        # Skill tab + P3 抽屉触发
│   ├── tool-tab/         # Tool tab + E6 用户须知
│   ├── mcp-tab/          # MCP 占位 tab
│   └── runtime-chat/     # 试运行下半屏 · AG-UI 客户端
├── api/             # axios 实例 + REST 端点封装
├── agui/            # AG-UI 协议适配（@ag-ui/client 包装 + 错误重试）
├── shared/          # 通用组件 / Hook / 工具
└── i18n/            # zh-CN / en-US 资源
```

### 2.3 关键交互

- **dirty 守卫**：P2 表单 dirty 时离开路由弹"放弃修改？"（AC-002 / AC-003-2）；通过 React Router `useBlocker` 实现。
- **未登录回跳**：axios 拦截 401 → `Navigate to /login?return=/agents...`；接口异常不带 return 防 401 循环（[ui-spec.md 第 2.5 节](../01-requirements/ui-spec.md)）。
- **试运行下半屏**：本期 A 同页组件（[OQ-030](../01-requirements/open-questions.md)）；通过 AG-UI 客户端订阅事件流，渲染 16 种 EventType 中的 lifecycle / text-message / tool-call 子集，初版可不渲染 state 事件。
- **AG-UI 与同页 / iframe 切换的兼容**：AG-UI 客户端层抽象为 `RuntimeChatHost`，UI 形态变更时只换容器、不换协议（vNext 切 iframe 仅相当于换 host iframe，AG-UI 客户端保留）。

## 3. 后端架构

### 3.1 技术栈

| 层级 | 选型 | 约束来源 |
| --- | --- | --- |
| 运行时 | .NET 9 LTS 候选 / 或 .NET 8 LTS | [.github/copilot-instructions.md](../../.github/copilot-instructions.md) `dotnet test` |
| Web 框架 | ASP.NET Core Web API（Minimal API 优先 + 必要时 MVC Controller） | 用户偏好 2 |
| ORM | EF Core 9（与 .NET 同步）；启用 `Microsoft.EntityFrameworkCore.InMemory` / `.SqlServer` / `.PostgreSQL` 三 provider | 用户偏好 2 + Q3=M1 |
| Agent 框架 | Microsoft Agent Framework（NuGet `Microsoft.Agents.AI` GA 1.4.0+） | 用户偏好 2 + [AGENTS.md L14](../../AGENTS.md) dogfooding 签字 |
| AG-UI 集成 | MAF 1st-party `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`（服务端）+ `Microsoft.Agents.AI.AGUI`（共享协议层） | 见 [ADR-002](./adr/ADR-002-agui-as-chat-protocol.md)；[OQ-A-003](./open-questions-arch.md) 已关闭 |
| 模型客户端 | Azure OpenAI（`Azure.AI.OpenAI` SDK；endpoint 与 region 待 OQ-A-006） | Q6=A |
| 鉴权（dev 期） | 自实现 `DevModeAuthenticationHandler`（cookie 写测试用户 ID） | Q5 = dev mock |
| 鉴权（生产期） | OIDC `Microsoft.AspNetCore.Authentication.OpenIdConnect`（具体 IdP 待 GAP-003） | 见 [ADR-004](./adr/ADR-004-platform-login-dev-mock.md) |
| HTTP 序列化 | `System.Text.Json`（默认 camelCase） | .NET 默认 |
| 校验 | FluentValidation 或 DataAnnotations（H3 决） | — |
| 测试 | MSTest（用户加项） + Moq + WebApplicationFactory | Q8 加项 |

### 3.2 项目结构（建议，H3 细化）

```
src/
├── Inkwell.Web/              # ASP.NET Core 启动项目
│   ├── Endpoints/            # REST 端点（按聚合分组）
│   ├── Agui/                 # AG-UI POST /agui/run 端点 + 事件流序列化
│   ├── Auth/                 # IPlatformAuthenticator + DevModeHandler / OidcHandler
│   ├── DependencyInjection/  # provider 工厂注册
│   └── Program.cs
├── Inkwell.Application/      # 应用服务层（无 EF Core 引用）
│   ├── Agents/               # AgentService / 命令查询
│   ├── Skills/               # SkillParser / SkillService
│   ├── Tools/                # ToolRegistry / IBuiltInTool / IWebSearchTool
│   └── Runtime/              # ChatOrchestrator（基于 MAF）
├── Inkwell.Domain/           # 实体 / 值对象 / 领域事件
├── Inkwell.Infrastructure/   # IRepository / IQueue / ICache / IObjectStorage 实现
│   ├── Persistence/          # EF Core DbContext + 三 provider 配置
│   ├── Queue/                # InMemory + Redis
│   ├── Cache/                # InMemory + Redis
│   ├── ObjectStorage/        # Local + Azure Blob
│   └── Search/               # WebSearchTool（Mock + 真实预留）
└── Inkwell.Contracts/        # DTO / API 契约（前后端共享 schema 候选源）

tests/
├── Inkwell.UnitTests/        # MSTest 单元
├── Inkwell.IntegrationTests/ # MSTest + WebApplicationFactory + Testcontainers（PG / SQL Server / Redis）
└── Inkwell.E2ETests/         # Playwright（前端） + 后端启动
```

### 3.3 REST API 概要（H3 细化）

- `GET    /api/agents`（用户视角，分页 / 软删除 tab）
- `POST   /api/agents`（创建）
- `GET    /api/agents/{id}`、`PUT /api/agents/{id}`、`DELETE /api/agents/{id}`、`POST /api/agents/{id}/restore`
- `POST   /api/agents/{id}/copy`
- `POST   /api/uploads/avatar`（multipart，对象存储）
- `GET    /api/skills`、`POST /api/skills`、`PUT /api/skills/{id}`、`DELETE /api/skills/{id}`
- `POST   /api/skills/import`（multipart，SKILL.md 解析）
- `GET    /api/tools`（平台预置清单）
- `POST   /api/agents/{id}/tools/{toolId}/acknowledge`（E6 首次启用须知）

### 3.4 AG-UI 端点

- `POST /api/agui/run`：请求体 = `RunAgentInput`（thread/run/messages/tools/context），响应 = SSE 流（`Content-Type: text/event-stream`），事件遵循 AG-UI 16 种 EventType 子集（本期实现 lifecycle / text-message / tool-call / messages-snapshot / RUN_ERROR；STATE_* 留 vNext）。
- 端点内部由 MAF `ChatOrchestrator` 驱动：组装 system prompt（Instructions + 已勾选 Skill）→ 注册 Tool（T-1 Mock / T-3）→ 调 Azure OpenAI → 输出事件流。
- 端点注册走 `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` 提供的 `AGUIEndpointRouteBuilderExtensions`（详见第 3.5 节），不自实现 SSE。

### 3.5 AG-UI hosting 包接入（OQ-A-003 已关闭）

服务端按官方 ASP.NET Core 模式接入：

```csharp
// Program.cs（伪代码，H3 详细设计具体化）
builder.Services.AddAgent<ChatOrchestrator>();   // ServiceCollectionExtensions（来自 hosting 包）
// ...
app.MapAGUIAgent("/api/agui/run");               // AGUIEndpointRouteBuilderExtensions（来自 hosting 包）
```

- 包：`Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`（[源码](https://github.com/microsoft/agent-framework/tree/main/dotnet/src/Microsoft.Agents.AI.Hosting.AGUI.AspNetCore)），默认输出 `AGUIServerSentEventsResult`（transport = SSE，与第 1.2 节协议分裂前提一致）
- 配套：`Microsoft.Agents.AI.AGUI` 提供共享协议类型（事件 / 序列化），客户端如有 .NET 调用方场景也用此包
- 状态：[Microsoft Learn Integrations](https://learn.microsoft.com/en-us/agent-framework/integrations/) 列 "AG UI · Preview"——非 Released；版本变化由 [RISK-001](./risk-analysis.md) 跟踪
- 具体扩展方法签名 / 中间件参数 / session storage 配置：H3 详细设计阶段细化

## 4. 数据库选型（multi-provider M1）

### 4.1 选型

EF Core 9 + 三 provider 同时支持运行时切换：

| Provider | 用途 | 局限 |
| --- | --- | --- |
| `InMemory` | 单元测试 / 本地快速启动 / 演示 | **无事务隔离**、**无关系约束**、**不能用于多副本生产**（数据在进程内） |
| `SqlServer` | Windows 生态 / 企业 IT | 计费 license；Azure 友好 |
| `PostgreSQL` (Npgsql) | Linux 容器 / 自托管 / 公有云通用 | 默认推荐 |

详见 [ADR-003](./adr/ADR-003-multi-provider-m1.md) 与 [tech-selection.md 第 3 节](./tech-selection.md)。

### 4.2 关键约定

- **DbContext 单一**：`InkwellDbContext` 同一份；provider 仅在 `Program.cs` 通过 `services.AddDbContext` 配置切换。
- **迁移多份**：每个生产 provider 独立 `Migrations/{Provider}/`（EF Core 不支持单迁移跨 provider）。
- **跨 provider 行为差异**：
  - 大小写敏感性：SQL Server 默认 case-insensitive，PG 默认 case-sensitive → 应用层显式 `.ToLower()` 或 `EF.Functions.Like`。
  - JSON 列：本期不使用 JSON 列，避免兼容性问题（Skill body / Instructions 直存 `nvarchar(max)` / `text`）。
  - 事务：InMemory 无事务，集成测试关键路径必须跑 PG / SQL Server 真实容器。
- **软删除（DB-004）**：`DeletedAt` 列 + 全局 query filter；硬删除走后台扫描任务（队列触发）。

### 4.3 主要表（H3 细化）

`Users` / `Agents`（含 DeletedAt）/ `Instructions` / `Skills`（用户级，DB-006）/ `AgentSkillRefs`（含 SkillVersion，DB-006）/ `AgentTools`（DB-007）/ `ToolAcknowledgements`（E6）/ `OutboxMessages`（事件 outbox，避免双写）。

## 5. 缓存策略（multi-provider）

| Provider | 用途 |
| --- | --- |
| `InMemory`（`IMemoryCache`） | 单机 / 测试 |
| `Redis`（`StackExchange.Redis` + `Microsoft.Extensions.Caching.StackExchangeRedis`） | 多副本生产；Sentinel 主从 |

抽象接口：`ICache` 暴露 `GetOrSetAsync<T>`、`InvalidateAsync(key)`、`InvalidateByTagAsync(tag)`。

**本期使用范围**：
- Skill `name` → `Id` 索引（导入查重，AC-010-4）
- 平台 Tool 元数据清单（`/api/tools`）
- gpt-4.1 token 配额查询结果（NFR-005，TTL ≤ 60s）

**不缓存**：用户级 Agent / Instructions（直接走 DB；编辑频次极低）。

## 6. 消息机制（队列 multi-provider）

| Provider | 用途 |
| --- | --- |
| `InMemory`（`Channel<T>` + 后台 HostedService） | 单机 / 测试；**多副本不可用** |
| `Redis Streams`（基于 `StackExchange.Redis`） | 多副本生产 |

抽象接口：`IQueue<TMessage>` 暴露 `EnqueueAsync` / `IConsumerRegistration<TMessage>`。

**本期使用范围**：
- Outbox 模式（DB → Queue → 处理器）：避免"DB 写成功但事件丢失"的双写问题
- 7 天软删除 → 硬删除的延迟扫描任务（DB-004）
- E6 用户须知确认事件（用于将来审计回放）
- AG-UI 长时对话的失败通知（重试 3 次仍失败时落审计）

**不进队列**：用户实时操作的 CRUD（直接同步写 DB）。

## 7. 鉴权与权限模型

### 7.1 双模式

| 模式 | 触发 | 实现 |
| --- | --- | --- |
| **dev mock**（本期 + 开发 + 集成测试） | `Authentication:Mode=DevMock` | `DevModeAuthenticationHandler` 读 cookie / header 中的 `X-Dev-User-Id`，自动生成 `ClaimsPrincipal`（含固定 role=`agent-author`） |
| **生产 OIDC** | `Authentication:Mode=Oidc`（待 GAP-003 确定 IdP） | `Microsoft.AspNetCore.Authentication.OpenIdConnect`；具体 IdP 由平台决定 |

详见 [ADR-004](./adr/ADR-004-platform-login-dev-mock.md)。

### 7.2 授权

- 资源所有者校验：`AgentService.LoadFor(userId)` 带 `Where(a => a.OwnerId == userId)`；其他用户访问 → 404（不暴露存在性）。
- 软删除可见性：`Where(a => a.DeletedAt == null || a.DeletedAt > now-7d)`。
- 跨用户分享 / 公开（vNext）：本期不实现，[PB-002 / PB-005](../01-requirements/requirements.md) 已锁定快照语义。
- 速率限制：`/api/agui/run` 与文件上传按 userId 限流（具体阈值 H3 决）。

## 8. 文件存储方案

| Provider | 用途 |
| --- | --- |
| `Local`（appsettings 配置 root path） | 开发 / 单机演示 |
| `Azure Blob Storage`（`Azure.Storage.Blobs`） | 生产 |

抽象接口：`IObjectStorage` 暴露 `UploadAsync(stream, path, contentType, metadata)` / `GetReadUrlAsync(path, ttl)` / `DeleteAsync(path)`。

**本期使用范围**：
- Agent 头像（REQ-002）：`avatars/{userId}/{agentId}.{ext}`
- 不存储 SKILL.md（结构化字段进 DB；体积小）
- 不存储对话历史（运行时观测 ND-007 已声明不做）

**安全**：
- Azure Blob 用 SAS（短期 Read URL，TTL ≤ 60min），不开公网容器
- Local 由后端代理读取 + 鉴权（不直接暴露文件系统路径）

详见 [tech-selection.md 第 6 节](./tech-selection.md) 与 [ADR-008](./adr/ADR-008-object-storage-azure-blob-only.md)。

## 9. 部署方式

### 9.1 双形态

| 形态 | 触发 | 内容 |
| --- | --- | --- |
| **开发 / 演示**（dev） | `docker-compose up` | postgres 容器 + redis 容器 + minio 容器（local 替代）+ 后端镜像 + 前端镜像 + dev mock 登录 |
| **生产**（prod） | Helm chart 部署到 K8s | 后端 Deployment（≥ 2 副本）+ 前端 Deployment + Ingress（启用 SSE 反代）+ Redis Sentinel + 数据库走外部托管（Azure Database for PG / SQL Server）+ Azure Blob 走外部托管 |

详见 [ADR-006](./adr/ADR-006-deployment-split.md)。

### 9.2 关键细节

- **SSE 反代**：Ingress（如 NGINX Ingress Controller）需配 `proxy_buffering off`、`proxy_read_timeout 1h`，否则 AG-UI 事件流会被截断。
- **WebSocket 备选 transport**：AG-UI 支持 SSE 与 WebSocket 双向选择；本期默认 SSE（更简单 + 客户端原生支持），WebSocket 留作 vNext 备选。
- **健康探针**：`/healthz`（liveness）/ `/readyz`（readiness，含 DB / Redis ping）。
- **滚动更新**：`maxSurge=25%, maxUnavailable=0`；prod 默认 ≥ 2 副本。
- **配置**：环境变量 + ConfigMap + Secret（连接串 / API Key 走 Secret）。

## 10. 可观测性方案

按 [ND-007](../01-requirements/requirements.md)，本特性**不**承担运行时观测后端，但**必须出 OTel signals 给平台运维消费**：

- **Trace**：MAF 内置 OpenTelemetry instrumentation 启用；ASP.NET Core / EF Core / HttpClient 自带 instrumentation 一并启用
- **Metrics**：`Microsoft.Extensions.Diagnostics.HealthChecks` 出 `/healthz`；OTel Metrics 出 HTTP / DB / Queue 标准指标
- **Logs**：结构化日志（Serilog 或 `Microsoft.Extensions.Logging` + JSON formatter）写 stdout，由平台运维收集
- **导出**：OTLP 协议默认导出到 `OTEL_EXPORTER_OTLP_ENDPOINT`（环境变量），具体目的地由平台运维决定
- **不实现**：评测样本、Prompt 版本对比、A/B 测试、用户行为分析（这些都属业务级监控，不在本特性内）

## 11. 性能目标

> [NFR-002](../01-requirements/requirements.md) 仅给出"≤ 100 同时在线"上限；本节给出 H2 级目标。

| 指标 | 目标 | 来源 |
| --- | --- | --- |
| `GET /api/agents` 列表 P95 | ≤ 300ms（含网络） | MVP 内部小范围 |
| `POST /api/agents`（创建） P95 | ≤ 500ms | 含元数据校验 + DB 写 |
| `POST /api/agui/run` 首字节延迟 P95 | ≤ 2s | Azure OpenAI 首字节延迟主导 |
| AG-UI SSE 单 token 间隔 P95 | ≤ 200ms | OpenAI 网关流式吐字 |
| 头像上传 ≤ 2MB P95 | ≤ 1.5s | 含对象存储写 |
| 同时在线 | ≤ 100 | NFR-002 |
| 单 Agent 试运行并发上限 | ≤ 3 | 防滥用 + 简化 token 桶 |

> 性能指标置信度均为 `medium`：基于业内公认基线 + Azure OpenAI 基础 SLA；首次部署后由 H6 阶段实测回写。

## 12. 扩展性设计

- **多 provider M1**：DB / 队列 / 缓存 / 对象存储 在不动应用代码的前提下切换 → 支持私有 / 公有云 / 自托管多场景
- **AG-UI 协议扩展**：vNext 加 STATE_SNAPSHOT / STATE_DELTA 事件支持后，无需改 API 契约 → 支持"Agent 状态共享"等长尾场景
- **MAF Workflow**：本期单 Agent 链路；vNext 接 MAF Workflow 即可支持多 Agent 编排 → 不破坏单 Agent 接口
- **Tool / MCP**：`IBuiltInTool` 接口 + ToolRegistry → vNext 接 MCP server（[ND-010](../01-requirements/requirements.md)）走同一接口
- **多租户 / 团队**（vNext，[PB-003](../01-requirements/requirements.md)）：Agent 实体已有 `OwnerId`，引入 `TeamId` 列即可；本期不预留太多

## 13. 安全设计

- **传输**：全程 HTTPS / TLS 1.2+；内网走 mTLS（H3 决）
- **鉴权**：dev mock 仅 `Authentication:Mode=DevMock` 时启用，生产构建拒绝该 mode
- **数据驻留**：Azure OpenAI region 与 Azure Blob region 必须同区（[NFR-004](../01-requirements/requirements.md) 数据合规）；具体 region 待 OQ-A-006
- **API Key 与 Secret**：走 K8s Secret / Azure Key Vault；不入 Git
- **CORS**：仅放行平台前端 origin
- **CSRF**：REST 端点用 CSRF token 或 SameSite Lax cookie
- **SSE / WebSocket**：CORS + Origin 校验
- **Skill body 注入风险**（[R3](../01-requirements/requirements.md)）：Skill body 作为 system prompt 拼接，已知风险已用户接受；本架构不增加事前审核（与 ND-008 一致）
- **文件上传**：Content-Type / 后缀双重校验；头像走 ImageSharp 解析后再写盘（防恶意文件）；大小 ≤ 2MB
- **限流**：`/api/agui/run` 单用户单时刻 ≤ 3 并发；REST 接口默认 60 RPS / IP（H3 决）

## 14. 主要技术风险

详见 [risk-analysis.md](./risk-analysis.md)。本节仅列编号：

`RISK-001` MAF AG-UI hosting 包 Preview 状态跟踪 · `RISK-002` MAF Foundry 集成 prerelease · `RISK-003` M1 多 provider 抽象层成本 · `RISK-004` 平台登录子系统延后决策 · `RISK-005` 联网搜索 Mock → 真实切换 · `RISK-006` K8s SSE 反代调优 · `RISK-007` Azure Blob 单点（私有部署不友好） · `RISK-008` 多副本部署下 InMemory provider 误用 · ~~`RISK-009`~~ 已并入 `RISK-001`（OQ-A-003 关闭后无独立 risk）

## 15. 替代方案比较

详见 [tech-selection.md](./tech-selection.md) 每条选型的"替代方案" + "放弃原因"列。本节不重复，避免漂移。

## 16. 成本估算（含付费云资源）

> 按 stages.md 第 5.4 节"涉及付费云资源、商业 license 或大规模采购则必须给出"。

| 项 | 单价（Azure 美东 2026 公开 SKU） | MVP 估算 / 月（≤ 100 同时在线） | 备注 |
| --- | --- | --- | --- |
| Azure OpenAI gpt-4.1 | input $1.25/MTok · output $5/MTok（PTU 与 PAYG 不同） | $50 ~ $300（按业务调用量浮动；试运行天然不密集） | 实际由 NFR-005 配额限制兜底 |
| Azure Blob Hot | $0.018/GB/月 + 出口 $0.087/GB | < $5（头像 ≤ 2MB × 用户数 < 100GB） | 含存储 + 流量 |
| Azure Database for PG（推荐 Burstable B1ms） | ~$15/月（1vCPU/2GB） | $15 | 生产；测试环境单独 |
| Redis（推荐 Azure Cache for Redis Basic C0） | ~$16/月（250MB） | $16 | 生产 |
| 出口流量（Ingress 公网） | $0.087/GB | < $10 | AG-UI SSE 流量较大但用户少 |
| K8s 集群（AKS）| 控制面免费；Node Pool $30~$60/月起 | $30 ~ $60 | 单 Node SKU 起 |
| **合计 MVP** | — | **~$120 ~ $400 / 月** | 浮动主要来自 OpenAI |

成本风险见 [RISK-007](./risk-analysis.md)（Azure Blob 单点 + 私有部署场景待定）。

## 17. 与下游的交付物

- **给 H3-DesignReviewer**：本图所有"H3 细化"占位由 H3 阶段产出 `docs/04-detailed-design/<feature>/` 详细设计；ADR-001~006 编号在 H3 设计文档的 frontmatter `upstream` 中显式引用
- **给 H4-TestCaseAuthor**：本图第 11 节性能目标 + 第 13 节安全设计构成 NFR 测试用例的输入；E6 首次启用须知、E7 SKILL 解析失败、AC-011-3/4 等异常路径已在第 3.4 节 AG-UI 事件覆盖范围内
- **给 H5-CodingExecutor**：本图第 3.2 节项目结构 + 第 4 节 multi-provider 是 H5 任务卡"允许修改文件"的依据；具体路径在 H3 落点后由 RepoImpactMap v0.2 输出
- **给 H6-ReleaseNoteWriter**：本图第 16 节成本估算与第 9 节双部署形态在 H6 release notes 中作为"运维须知"章节

## 18. 变更记录

| 版本 | 日期       | 变更人              | 变更内容 |
| ---- | ---------- | ------------------- | --- |
| 0.1  | 2026-05-07 | H2-ArchitectAdvisor | 首版。覆盖 stages.md 第 5.4 节全 15 章节 + 成本估算；引用 7 份独立 ADR；登记 9 条 RISK 与 8 条 OQ-A。 |
| 0.2  | 2026-05-07 | H2-ArchitectAdvisor | OQ-A-003 关闭：固化 MAF AG-UI hosting 包 `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` + `Microsoft.Agents.AI.AGUI`；新增第 3.5 节；RISK-009 并入 RISK-001。 |
