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

## 1. 模块概述

### 1.1 职责

`Inkwell.WebApi` 是 [ADR-019](../../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) 锁定的 HTTP 入口进程。本文档覆盖 H5 编码阶段已落地的两块能力：

- **会话鉴权**（`Authentication/` 子目录）：把 [HD-014 `IAuthService`](HD-014-Inkwell.Core.Auth.md) 签发的不透明 `SessionToken` 接入 ASP.NET Core 的 `AddAuthentication`/`[Authorize]` 体系，构造 `ClaimsPrincipal`。
- **Agent 多协议路由端点**（挂载在 `Inkwell.Core.AgentRuntime` 内的 `RoutingAgent` + `UseAgentEndpoints(...)`，`Inkwell.WebApi` 侧仅调用这一个扩展方法）：把 AG-UI（未来含 OpenAI ChatCompletions/Responses/Conversations）协议请求路由到 [HD-015 `IAgentInvocationService`](HD-015-Inkwell.Core.Agents.md)。

### 1.2 范围

**在内**：

- `Authentication/AuthenticationDefaults.cs` / `SessionAuthenticationOptions.cs` / `SessionClaimTypes.cs` / `AuthorizationPolicies.cs` / `SessionAuthenticationHandler.cs` / `AuthenticationBuilderExtensions.cs`
- `Program.cs` 对上述两块能力的 DI 注册 + 中间件 / 端点挂载

**不在内**（明确排除）：

- OpenAI ChatCompletions / Responses / Conversations 协议端点的具体挂载——[ADR-012](../../03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md) 目前只锁定"REST + AG-UI"，本文档只搭好可扩展的壳子（`UseAgentEndpoints` 内部可追加对应 `Map*` 调用），不越权臆造尚未拍板的协议端点具体路由/鉴权细节
- REST CRUD 端点（Agent/会话/工具/技能管理等）——留待后续任务补齐，本文档不越权臆造
- `Inkwell.Core.AgentRuntime.RoutingAgent`/`AgentResponseMapper`/`AgentEndpointRouteBuilderExtensions` 的内部实现细节——物理文件位于 `Inkwell.Core`，已在 [HD-006 2026-07-10 errata](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) 提及，本文档只记录 `Inkwell.WebApi` 侧的消费方式（`UseAgentEndpoints(...)` 一行调用），不重复其内部设计

## 2. 文件结构

```text
src/core/Inkwell.WebApi/
  Authentication/                        # 新增子目录（本 HD）
    AuthenticationDefaults.cs            # 鉴权方案名称常量
    SessionAuthenticationOptions.cs       # AuthenticationSchemeOptions 具体类型占位
    SessionClaimTypes.cs                  # 自定义 Claim 类型（IsSuper）
    AuthorizationPolicies.cs             # 授权策略名称常量（RequireSuperUser）
    SessionAuthenticationHandler.cs       # AuthenticationHandler<SessionAuthenticationOptions> 实现
    AuthenticationBuilderExtensions.cs    # AddSessionAuthentication() DI 注册入口
  Program.cs                             # 已有文件，本 HD 追加鉴权 + Agent 端点两处调用
```

## 3. 关键决策记录

- **Q1（`SessionToken` 传输方式选 Bearer Header，不选 Cookie）**——**vscode_askQuestions 真实交互**（用户在本次会话中选择"Bearer Header"选项）。理由：Inkwell 客户端是 Electron 桌面应用（非浏览器页面），不需要 Cookie 的 `SameSite`/CSRF 防护约束；Bearer Header 是原生客户端场景的标准做法。
- **Q2（鉴权处理器不做 JWT 自解析，每次请求都调 `IAuthService.ValidateSessionAsync` 查缓存）**——作者判断（既定事实推导，非新决策）。理由：[HD-014 `SessionTokenGenerator`](HD-014-Inkwell.Core.Auth.md) 生成的是不透明随机字符串（32 字节随机数 + Base64Url），不是自解析 JWT；鉴权处理器的实现必须匹配这一既定事实，没有第二种选择。
- **Q3（`AuthenticateResult.NoResult()` vs `Fail()` 的区分）**——作者判断。请求未带 `Authorization` 头 → `NoResult()`；token 存在但校验失败（`UnauthorizedAccessException`）→ `Fail()`。理由：符合 ASP.NET Core `AuthenticationHandler` 惯例语义（`NoResult` = 本方案未尝试鉴权，留给下游 `[AllowAnonymous]` 端点正常放行；`Fail` = 尝试过但失败，触发 401）。
- **Q4（`callerUserId`/`agentId`/`conversationId` 均通过 URL 路由值传递，不依赖 MAF AG-UI 的 `ThreadId`/`AgentSessionStore` 机制）**——作者判断。理由：该链路未注册自定义 `AgentSessionStore`（详见 [HD-006 2026-07-10 errata](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md)），`NoopAgentSessionStore` 默认行为未经验证，直接用路由值更简单、更可控，避免验证不足的假设。
- **Q5（端点路由形如 `/api/agents/{agentId}/conversations/{conversationId?}/agui`）**——作者判断。`conversationId?` 可选段对应 [HD-006 `AgentRunRequest.ConversationId`](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) 语义（`null` = 新对话）；具体路径前缀待正式 REST API 设计统一收敛，当前仅为占位。

## 4. 安全考虑

- `Authorization` 头缺失 → `NoResult()`（未鉴权，非攻击信号）；token 无效/过期 → `Fail()` → 401；两种情况均不泄漏"用户名是否存在"等信息（沿用 [HD-014 `AuthService.LoginAsync`](HD-014-Inkwell.Core.Auth.md) 已有的计时侧信道防护原则）。
- `RoutingAgent`（详见 [HD-006 errata](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md)）内部 `ResolveCallerUserId` 若取不到有效 `ClaimTypes.NameIdentifier` 直接抛 `UnauthorizedAccessException`，不允许匿名调用；AG-UI 端点已 `RequireAuthorization()`。
- **未决问题**（不在本文档回答，留待后续 REST API 整体设计时处理）：`agentId` 对应的 Agent 是否属于 `callerUserId`（授权校验）目前完全依赖 [HD-015 `AgentInvocationService.ValidateInvocationAccess`](HD-015-Inkwell.Core.Agents.md) 在业务层拦截，`Inkwell.WebApi` 层不重复校验——与 [HD-014 §1.2](HD-014-Inkwell.Core.Auth.md) 已确立的"WebApi 层不重复校验 `IsSuper`，交给业务层"先例一致。

## 5. 待确认的问题

- 本文档 `status: draft`，尚未经 Owner 正式评审；§3 决策记录中标注"作者判断"的条目（Q2 ~ Q5）未经 `vscode_askQuestions` 真实确认，如与产品实际预期不符，需要 Owner 明确指出后修订。
- REST CRUD 端点、OpenAI ChatCompletions/Responses/Conversations 端点的具体路由与鉴权细节完全未起草，留待后续任务。
