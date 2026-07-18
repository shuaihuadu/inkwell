---
id: HD-021
title: Inkwell.WebApi 详细设计 — 会话鉴权方案 + Agent 多协议路由端点（草案，覆盖 H5 已落地部分）
stage: H3
status: draft
reviewers: []
upstream:
  - REQ-001
  - REQ-010
  - REQ-014
  - ADR-012
  - ADR-017
  - HD-006
  - HD-014
  - HD-015
  - HD-017
---

> **本文档性质说明**：与其他 HD 不同，本文档是在 H5 编码阶段（AgentRuntime 重新设计会话）**代码已实现之后**补写的设计记录，不是先设计后编码。补写目的是把已落地的两个真实决策（会话鉴权方案、Agent 路由端点）记录下来，供后续复核与扩展参考；不倒填"事前评审通过"的既定印象。`status: draft` / `reviewers: []` 如实反映——本文档尚未经过 Owner 正式评审签字，签字仍需人工完成（[AGENTS.md §5](../../../AGENTS.md)）。
>
> **决策出处标注约定**：下文每条关键决策标注真实出处——`vscode_askQuestions 真实交互`（用户在对话中通过选项确认）或 `作者判断`（未经用户显式确认的工程推理），不混同。
>
> **2026-07-12 替代性 errata（移除 RoutingAgent）**：本文原 `RoutingAgent + IAgentInvocationService` 路由方案已被新 Agent Factory 主干取代，下方对应章节仅保留历史记录。现行方向是协议入口先完成 Bearer 鉴权与 Agent 版本授权，再加载不可变 `AgentVersion`，解析工具与 Session/History 组件，通过 `IAgentFactory.BuildAsync` 得到标准 MAF `AIAgent`，最后交给对应官方 Hosting。AG-UI 的 `thread_id` 作为 Session 连续性标识处理，不再把 `conversationId` 设计为可选 URL 段；`agentId` 或发布别名仍属于路由/endpoint 选择信息。
>
> **协议边界**：AG-UI、OpenAI Chat Completions、OpenAI Responses 与 OpenAI Conversations 各自保留标准请求/响应模型，禁止先映射成一套 Inkwell 自建通用 Run DTO 再反向映射。四种协议共享的是 Factory、授权、版本快照、工具解析、Session/History 与可观测性，而不是共享传输 DTO。
>
> **2026-07-15 替代性 errata（产品 Conversation + AG-UI Thread）**：Owner 在当前对话中直接确认：REST 先创建 `AgentConversation`，MAF `AgentSession` 首次 Run 懒建；同一 Conversation 同时只允许一个 Run，冲突返回 `409 Conflict`。本 errata 取代下方 Q4/Q5“绕开 `ThreadId`/`AgentSessionStore`”和“可选 conversationId URL 段”的作者判断。当前契约是 `RunAgentInput.ThreadId = AgentConversation.Id.ToString("D")`，MAF Hosting 用该值调用自定义 `InkwellAgentSessionStore`；`threadId` 只负责定位，绝不承担授权。
>
> **2026-07-16 路由 Session 删除 errata**：`RoutingAgentSession` / `RoutingAgentSessionState` 及其版本固定、内部 Session 序列化逻辑已删除，实际 Agent Session 方案等待后续讨论。当前 `RoutingAgent` 是无状态代理：Create/Serialize/Deserialize 仅满足 MAF 抽象的空占位契约，每次 Run 按 URL 中的 `agentId` 与认证用户重新解析当前发布版本并调用实际 Agent，且不向实际 Agent 传递 Session。本行为是临时路由实现，不覆盖产品 Conversation 已保存的版本不变量，也不构成最终 Session 连续性设计。
>
> **2026-07-16 WebApi/Core 构建边界 errata**：上段“重新解析当前发布版本”由 Core `IAgentBuildService` 完成。`RoutingAgent` 只从 Route 读取 `agentId`、从 Claims 读取 `requestingUserId`，随后调用一次 `BuildPublishedAsync` 并转发 MAF Run；版本授权、Snapshot 绑定解析和 Factory 构建均不在 WebApi 实现。该边界取代下方任何由协议入口直接组合 `IAgentVersionService` / `IAgentBuildOptionsResolver` / `IAgentFactory` 的描述。
>
> **2026-07-15 技术方向与待定路由**：AG-UI 直接使用 MAF `MapAGUI`，不增加 Inkwell 自建 Run DTO、协议状态机或 SSE 编码器；会话连续性由 `RunAgentInput.ThreadId` → `AgentSessionStore` 原生链路承接。具体挂载路径尚未由 Owner 拍板：当前代码是 `/agent/{agentId}`，ADR-012 仍描述 `/api/runs`，`/api/agents/{agentId}/agui` 仅为候选，不得写入 H5 brief 作为既定契约。
>
> **2026-07-16 协议核验修正**：后端输入设计以 `@ag-ui/client@0.0.57` / `@ag-ui/core@0.0.57` 的实际发布包为准。`HttpAgent` 对标准 `RunAgentInput` 直接执行 `JSON.stringify(input)`；请求体固定包含 `threadId`、`runId`、`state`、`messages`、`tools`、`context`、`forwardedProps`，可选包含 `parentRunId`、`resume`。SDK 从 `HttpAgent` 实例取完整 `messages` 快照（仅过滤 `role = activity`），并非只发送本轮新增消息。本修正撤销下文基于异步调用链 accessor / `AsyncLocal` 传递 Run Context 的方案；服务端直接绑定 MAF 使用的 `AGUI.Abstractions.RunAgentInput`，不再设计第二套接收 Model。
>
> **MAF API 版本提示**：Inkwell 当前锁定包与代码使用 `MapAGUI`；同工作区最新 MAF `main` 源码使用 `MapAGUIServer`。实现升级后以实际恢复版本的公开 API 和编译结果为准；本文后续以最新源码名 `MapAGUIServer` 描述 endpoint builder 行为。
>
> **DTO / AgentRun 边界**：AG-UI 请求直接绑定实际 MAF 版本的 `AGUI.Abstractions.RunAgentInput`，响应直接使用 Hosting 生成的标准 SSE 事件；禁止增加 Inkwell `AgentRunRequest` / `AgentRunResponse` / `AgentRunEvent` 中转 DTO。`ExecutionId` 是服务端内部执行关联 ID，不产生产品 `AgentRun` Model、Entity 或 CRUD endpoint。运行中状态由 Conversation 租约表示，最终消息与 Session 检查点分别进入既有两张子表，诊断事件留给 Traces 模块。

## 0. 2026-07-15 当前会话与路由契约

### 0.1 REST 产品会话端点

- `POST /api/agents/{agentId}/conversations`：从已认证 Claims 提取 `OwnerUserId`，调用 `IAgentConversationService.CreateConversationAsync(agentId, ownerUserId, ...)`。Service 在创建时解析并锁定 `AgentVersionId`；响应返回 Conversation ID、标题、绑定版本与时间戳。WebApi 不直接调用 Repository。
- `GET /api/agents/{agentId}/conversations`：返回该 `OwnerUserId + AgentId` 下的历史会话列表。
- `GET /api/agents/{agentId}/conversations/{conversationId}/messages`：返回数据库中的完整历史消息；前端历史加载只走本端点，不从 AG-UI SessionState 反推。
- `DELETE /api/agents/{agentId}/conversations/{conversationId}/messages/{messageId}`：调用 Service 删除单条消息、失效 `AgentSessionState` 并重算 Conversation 派生字段；消息不属于该 Conversation 时返回 404。
- `POST /api/agents/{agentId}/conversations/{conversationId}/clear`：调用 Service 原子清空消息与 `AgentSessionState`、把 Title 置空并保留 Conversation。
- `DELETE /api/agents/{agentId}/conversations/{conversationId}`：调用 Service 删除产品会话，由数据库级联删除消息与状态。

所有端点都把 `ownerUserId + agentId + conversationId` 作为明确业务参数传给 Service。身份缺失/无效由 Authentication 产生 401；已认证但不拥有该 Conversation 或 AgentId 不匹配映射 403；资源不存在映射 404；有效 Run 租约导致的清空、删除或新 Run 冲突映射 409。WebApi 只做 Claims/路由/请求 DTO 与 HTTP 的适配，不在 Endpoint 中实现 Owner 或版本规则。

### 0.2 AG-UI Run 入口

AG-UI 挂载路径待 ADR-012 errata 或 Owner 明确拍板；无论最终路径如何，Conversation 不作为可选 URL 段。请求必须包含 `RunAgentInput.ThreadId`，其值为已由 REST 创建的 `AgentConversation.Id` 的小写或大写均可解析、输出统一为 `Guid.ToString("D")` 的稳定字符串。缺失、非 GUID 或不存在的 `ThreadId` 拒绝请求；服务端不得把 `ThreadId` 解释为“自动创建新会话”。

TypeScript SDK 的实际 JSON 契约如下；后端必须通过锁定 MAF/`AGUI.Abstractions` 版本的 `RunAgentInput` 直接接收，不复制同名 DTO：

| JSON 字段        | SDK 0.0.57 来源                                       | Inkwell 处理                                                                |
| ---------------- | ----------------------------------------------------- | --------------------------------------------------------------------------- |
| `threadId`       | `HttpAgent.threadId`                                  | 必须为已存在的 `AgentConversation.Id`；用于定位，不用于授权                 |
| `runId`          | `runAgent({ runId })` 或 SDK 自动生成 UUID            | 仅作为 AG-UI 协议关联 ID；不作为租约持有者或数据库幂等键                    |
| `parentRunId`    | 协议可选字段                                          | v1 不用于产品会话分支；存在时按 MAF 标准模型接收，不赋予授权语义            |
| `state`          | `HttpAgent.state` 完整克隆                            | 作为不可信客户端状态交给 MAF；不得覆盖服务端 `AgentSessionState` 真值       |
| `messages`       | `HttpAgent.messages` 完整克隆并过滤 `role = activity` | 按标准 `Message` 联合类型接收；后端从数据库历史去重/校验后确定本轮新增输入  |
| `tools`          | `runAgent({ tools })`，默认 `[]`                      | 仅表示客户端工具声明；服务端工具授权仍由绑定版本配置决定                    |
| `context`        | `runAgent({ context })`，默认 `[]`                    | 不可信协议上下文，不承载 Owner、AgentVersion 或权限                         |
| `forwardedProps` | `runAgent({ forwardedProps })`，默认 `{}`             | 客户端内容不可信；filter 删除并覆盖保留键 `inkwell` 后才可供内部 Agent 使用 |
| `resume`         | `runAgent({ resume })` 可选                           | v1 按 MAF 模型接收；具体 HITL 恢复能力不在 H5-005 范围内                    |

请求使用 `POST application/json`，`Accept: text/event-stream`，Bearer token 由 `HttpAgent.headers` 注入。SDK 的 `runAgent` 参数不能替换 `threadId`、`messages` 或 `state`；调用方应在创建/恢复 `HttpAgent` 时设置 `threadId`、通过 `setMessages` 恢复 REST 历史、通过 `addMessage` 追加新输入，然后运行。

这里不定义第二个“后台接收 Model”：ASP.NET Core 模型绑定得到的 `RunAgentInput` 就是协议边界。Filter 授权后写入的 `forwardedProps.inkwell` 仅是同一请求对象上的服务端内部上下文；进入 `RoutingAgent` 后解析为内部执行上下文，不暴露为 WebApi DTO，不持久化为 `AgentRun`。

`MapAGUIServer` 返回的 endpoint builder 增加 `AgentConversationRunEndpointFilter`。Filter 不重复读取原始 body，而是读取 minimal API 已完成模型绑定的 `RunAgentInput` 参数：

1. Filter 从 Claims 提取 `OwnerUserId`、从路由提取 `AgentId`、从 `RunAgentInput.ThreadId` 解析 `ConversationId`，并生成全局唯一、不可由客户端指定的服务端 `ExecutionId`。
2. Filter 调用 Service 校验会话 Owner、Agent 与不可变版本并以 `ExecutionId` 原子占用租约；竞争由 Filter 映射为 409。客户端 `runId` 只保留为 `ProtocolRunId` 参与日志与 trace 关联，不用于授权、fencing 或幂等。
3. Filter 删除客户端传入的 `forwardedProps.inkwell`，再覆盖为服务端生成的 `{ conversationId, agentId, agentVersionId, ownerUserId, executionId, protocolRunId }`。该保留对象是同一 `RunAgentInput` 上的显式服务端上下文；`RoutingAgent` 通过 MAF 已写入 `ChatOptions` 的 `RunAgentInput` 读取，不使用 `AsyncLocal`、`IHttpContextAccessor` 或其他 ambient holder。
4. MAF 当前在 endpoint mapping 阶段从根容器解析 keyed `AgentSessionStore`，因此 `InkwellAgentSessionStore` 注册为 keyed singleton并按调用创建短生命周期 scope。Store 把 MAF 传入的 conversation key 当作 opaque `SessionKey` 精确查询，不解析 GUID 或假定 key 未经 decorator 改写；首轮按 Conversation 的不可变 `AgentVersionId` 构建内部 Agent 和 wrapper Session，后续恢复检查点。它不读取 Claims、`HttpContext` 或 ambient Context。
5. `RoutingAgent` 从已验证的 `forwardedProps.inkwell` 读取本轮标识并写入 wrapper Session 的保留 StateBag key；History Provider 读取该 key，Store 保存时读取并移除后再序列化，瞬时 `ExecutionId` 不进入持久状态。Filter 对 MAF SSE `IResult` 包装租约续期与清理，并在停止或断线后已有增量时用独立短时令牌提交部分消息；不得使用已经取消的 `RequestAborted`。

若模型绑定在进入 Filter 前失败，则没有租约需要释放。Filter 从 `EndpointFilterInvocationContext.Arguments` 按类型查找唯一 `RunAgentInput`，不依赖参数下标；字段形态、camelCase JSON 和 `forwardedProps.inkwell` 覆盖行为由“TypeScript `HttpAgent` 发包 → ASP.NET Core 绑定”的跨语言契约测试保护。Filter 获取租约后的所有正常结果都必须经过 `ConversationRunLeaseResult`；长 Run 只能在原租约仍有效时由同一持有者条件续租。释放失败只记录 warning 并依赖过期回收，不覆盖内层执行异常。

### 0.3 消息发送规则

`@ag-ui/client@0.0.57` 每次 Run 会发送 `HttpAgent.messages` 的完整快照（过滤 `activity`），因此前端从 REST 恢复历史后，第二轮请求自然包含历史消息和本轮新增输入。后端以数据库 `AgentChatMessage.Message` 为历史唯一真值：覆盖 History Provider 的默认合并逻辑，逐项验证数据库规范前缀，只把严格匹配后的客户端后缀作为新增输入，最终传给模型的每条历史只出现一次。缺失或重复 MessageId、顺序改变、内容改变、截短或分叉返回稳定的 409/422。MAF `AgentSessionState` 仅是可重建检查点。

### 0.4 单 Run 与错误映射

`AcquireRunAsync` 占用失败是预期业务冲突，WebApi 返回 `409 Conflict`，Problem Details 的稳定 `type` 为 `urn:inkwell:problem:conversation-run-conflict`，不得排队或并行合并。租约释放只允许匹配的 `RunId`；进程崩溃由租约过期恢复。`threadId` 猜测、其他用户的 Conversation、相同用户但不同 `AgentId` 均不得绕过 Service 授权。

## 1. 模块概述

### 1.1 职责

`Inkwell.WebApi` 是 [ADR-019](../../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) 锁定的 HTTP 入口进程。本文档覆盖 H5 编码阶段已落地的两块能力：

- **会话鉴权**（`Authentication/` 子目录）：把 [HD-014 `IAuthService`](HD-014-Inkwell.Core.Auth.md) 签发的不透明 `SessionToken` 接入 ASP.NET Core 的 `AddAuthentication`/`[Authorize]` 体系，构造 `ClaimsPrincipal`。
- **Agent 多协议路由端点**（挂载在 `Inkwell.Core.AgentRuntime` 内的 `RoutingAgent` + `UseAgentEndpoints(...)`，`Inkwell.WebApi` 侧仅调用这一个扩展方法）：把 AG-UI（未来含 OpenAI ChatCompletions/Responses/Conversations）协议请求路由到 [HD-015 `IAgentInvocationService`](HD-015-Inkwell.Core.Agents.md)。
- **2026-07-15 当前增量**：产品 Conversation REST CRUD、AG-UI `ThreadId` 授权桥接、单 Run 租约获取/释放与 409 映射；具体契约见 §0，原 RoutingAgent 描述仅保留历史。

### 1.2 范围

**在内**：

- `Authentication/AuthenticationDefaults.cs` / `SessionAuthenticationOptions.cs` / `SessionClaimTypes.cs` / `AuthorizationPolicies.cs` / `SessionAuthenticationHandler.cs` / `AuthenticationBuilderExtensions.cs`
- `Controllers/AuthController.cs` 及 `Auth/*Request.cs` 的登录、会话、改密与 Admin 用户管理 HTTP 适配
- `Conversations/AgentConversationEndpoints.cs` / `Conversations/AgentConversationRunEndpointFilter.cs` / `Conversations/ConversationRunLeaseResult.cs`
- `Program.cs` 对上述两块能力的 DI 注册 + endpoint filter / 端点挂载

**不在内**（明确排除）：

- OpenAI ChatCompletions / Responses / Conversations 协议端点的具体挂载——[ADR-012](../../03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md) 目前只锁定"REST + AG-UI"，本文档只搭好可扩展的壳子（`UseAgentEndpoints` 内部可追加对应 `Map*` 调用），不越权臆造尚未拍板的协议端点具体路由/鉴权细节
- Agent / 工具 / 技能等非 Conversation REST CRUD 端点——留待后续任务补齐；Auth REST 端点与 Conversation REST 已落地
- `Inkwell.Core.AgentRuntime.RoutingAgent`/`AgentResponseMapper`/`AgentEndpointRouteBuilderExtensions` 的内部实现细节——物理文件位于 `Inkwell.Core`，已在 [HD-006 2026-07-10 errata](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) 提及，本文档只记录 `Inkwell.WebApi` 侧的消费方式（`UseAgentEndpoints(...)` 一行调用），不重复其内部设计

## 2. 文件结构

```text
src/core/Inkwell.WebApi/
  Authentication/                        # 新增子目录（本 HD）
    AuthenticationDefaults.cs            # 鉴权方案名称常量
    SessionAuthenticationOptions.cs       # AuthenticationSchemeOptions 具体类型占位
    SessionClaimTypes.cs                  # 自定义 Claim 类型（IsAdmin / MustChangePassword）
    AuthorizationPolicies.cs             # 授权策略名称常量（RequireAuthenticatedUser / RequireAdmin）
    SessionAuthenticationHandler.cs       # AuthenticationHandler<SessionAuthenticationOptions> 实现
    AuthenticationBuilderExtensions.cs    # AddSessionAuthentication() DI 注册入口
  Conversations/
    AgentConversationEndpoints.cs         # 产品会话 REST CRUD；只做 HTTP 适配与 Service 调用
    AgentConversationRunEndpointFilter.cs # 从已绑定 RunAgentInput 授权、占用租约并覆盖 forwardedProps.inkwell
    ConversationRunLeaseResult.cs         # 包装 SSE IResult，在 ExecuteAsync finally 释放租约
  Controllers/
    AuthController.cs                     # 登录、会话、解锁、改密与 Admin 用户管理端点
  Program.cs                             # 已有文件，本 HD 追加鉴权 + Agent 端点两处调用
```

### 2.1 认证 Claims 与授权策略

- `SessionAuthenticationHandler` 通过 `IAuthService.ValidateSessionAsync` 校验 Bearer Session Token，并签发用户标识、`IsAdmin` 与 `MustChangePassword` Claims。
- `RequireAuthenticatedUser` 要求已认证且 `MustChangePassword=false`，用于工作区功能；`RequireAdmin` 在此基础上额外要求 `IsAdmin=true`。
- `MustChangePassword=true` 的会话仍可调用 `/api/auth/session`、`/logout`、`/unlock` 与 `/password`，但不能访问工作区或账号管理端点。
- WebApi policy 负责 HTTP 边界授权；`IAuthService` 的创建账号、列表、解锁、禁用、启用和重置密码方法仍通过 `actorUserId` 在 Core 复核 Admin 身份与账号可用状态。

### 2.2 Auth 路由授权矩阵

| 方法 | 路由                                         | 授权                                                       |
| ---- | -------------------------------------------- | ---------------------------------------------------------- |
| POST | `/api/auth/login`                            | 匿名；应用 Auth rate limiter                               |
| POST | `/api/auth/logout`                           | 有效基础会话                                               |
| GET  | `/api/auth/session`                          | 有效基础会话                                               |
| POST | `/api/auth/unlock`                           | 有效基础会话；应用 Auth rate limiter                       |
| POST | `/api/auth/password`                         | 有效基础会话；允许强制改密用户调用；应用 Auth rate limiter |
| GET  | `/api/auth/accounts`                         | `RequireAdmin`                                             |
| POST | `/api/auth/accounts`                         | `RequireAdmin`                                             |
| POST | `/api/auth/accounts/{userId}/unlock`         | `RequireAdmin`                                             |
| POST | `/api/auth/accounts/{userId}/disable`        | `RequireAdmin`                                             |
| POST | `/api/auth/accounts/{userId}/enable`         | `RequireAdmin`                                             |
| POST | `/api/auth/accounts/{userId}/reset-password` | `RequireAdmin`                                             |

## 3. 关键决策记录

- **Q1（`SessionToken` 传输方式选 Bearer Header，不选 Cookie）**——**vscode_askQuestions 真实交互**（用户在本次会话中选择"Bearer Header"选项）。理由：Inkwell 客户端是 Electron 桌面应用（非浏览器页面），不需要 Cookie 的 `SameSite`/CSRF 防护约束；Bearer Header 是原生客户端场景的标准做法。
- **Q2（鉴权处理器不做 JWT 自解析，每次请求都调 `IAuthService.ValidateSessionAsync` 查缓存）**——作者判断（既定事实推导，非新决策）。理由：[HD-014 `SessionTokenGenerator`](HD-014-Inkwell.Core.Auth.md) 生成的是不透明随机字符串（32 字节随机数 + Base64Url），不是自解析 JWT；鉴权处理器的实现必须匹配这一既定事实，没有第二种选择。
- **Q3（`AuthenticateResult.NoResult()` vs `Fail()` 的区分）**——作者判断。请求未带 `Authorization` 头 → `NoResult()`；token 存在但校验失败（`UnauthorizedAccessException`）→ `Fail()`。理由：符合 ASP.NET Core `AuthenticationHandler` 惯例语义（`NoResult` = 本方案未尝试鉴权，留给下游 `[AllowAnonymous]` 端点正常放行；`Fail` = 尝试过但失败，触发 401）。
- **Q4（历史，已由 2026-07-15 errata 取代）**——原判断通过 URL 绕开 MAF `ThreadId` / `AgentSessionStore`；当前使用 `RunAgentInput.ThreadId` 驱动 `InkwellAgentSessionStore`，并在 WebApi → Service 边界显式传递调用者身份。
- **Q5（历史，已由 2026-07-15 errata 取代）**——原可选 conversationId URL 段不再使用；具体 AG-UI 挂载路径待拍板，产品 Conversation 必须先由 REST 创建并通过请求 `ThreadId` 引用。

## 4. 安全考虑

- `Authorization` 头缺失 → `NoResult()`（未鉴权，非攻击信号）；token 无效/过期 → `Fail()` → 401；两种情况均不泄漏"用户名是否存在"等信息（沿用 [HD-014 `AuthService.LoginAsync`](HD-014-Inkwell.Core.Auth.md) 已有的计时侧信道防护原则）。
- `RoutingAgent`（详见 [HD-006 errata](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md)）内部 `ResolveCallerUserId` 若取不到有效 `ClaimTypes.NameIdentifier` 直接抛 `UnauthorizedAccessException`，不允许匿名调用；AG-UI 端点已 `RequireAuthorization()`。
- **未决问题**（不在本文档回答，留待后续 REST API 整体设计时处理）：`agentId` 对应的 Agent 是否属于 `callerUserId`（授权校验）目前完全依赖 [HD-015 `AgentInvocationService.ValidateInvocationAccess`](HD-015-Inkwell.Core.Agents.md) 在业务层拦截；管理员身份使用 `IsAdmin` Claim。

## 5. 待确认的问题

- 本文档 `status: draft`，尚未经 Owner 正式评审；§3 的 Q2/Q3 仍是作者判断。Q4/Q5 已由 2026-07-15 Owner 在当前对话中直接确认的新契约取代，不再是开放项。
- Agent / 工具 / 技能等非 Conversation REST CRUD，以及 OpenAI ChatCompletions/Responses/Conversations 端点的具体路由与鉴权细节仍待后续任务；Conversation REST 与 AG-UI 路由以 §0 为准。
