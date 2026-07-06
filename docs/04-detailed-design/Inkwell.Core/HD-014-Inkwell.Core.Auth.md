---
id: HD-014
title: Inkwell.Core.Auth 详细设计 — 登录鉴权 / 会话 / 账号锁定与解封
stage: H3
status: draft
reviewers: []
upstream:
  - REQ-001
  - REQ-017
  - NFR-003
  - NFR-004
  - ADR-004
  - ADR-016
  - ADR-017
  - ADR-021
  - ADR-023
  - HD-001
  - HD-002
  - HD-004
  - HD-007
---

<!-- markdownlint-disable MD060 -->
<!-- 中文 + 英文混排长表格在 markdownlint 列宽计算下字面对齐 ≠ 视觉对齐（详 /memories/markdown-lint.md，与 HD-004 / HD-005 / HD-006 / HD-007 同处理方式），表格仍按 docs-style §3 视觉对齐维护，机械 MD060 不予执行。 -->

> **本 HD 是 H3 第一张业务命名空间（`Inkwell.Core.*`）详细设计**，也是 `Inkwell.Core.csproj`（[AGENTS.md §3.1](../../../AGENTS.md)）第一次出现真实文件。此前 HD-001 ~ HD-013（HD-013 未起草）全部落在端口层（`Inkwell.Abstractions`）与 EFCore Provider 家族（`providers/Inkwell.Persistence.EFCore*`）。
>
> **范围核实结论（非臆造，逐条附证据）**：
>
> - **REQ-001（用户登录）在范围内**——[requirements.md §5.1](../../01-requirements/requirements.md) 字面"用户名 + 密码登录；登录态在客户端持有；支持手动登出"；[§11 验收标准](../../01-requirements/requirements.md) "登录态可持久 24 小时；登出后再次访问 Agent 列表会被重定向到登录页"。
> - **REQ-013（公开 API / Webhook 暴露）不在本 HD 范围**——用户在任务需求中要求核实，核实结论：[repo-impact-map.md §3.1](../../01-requirements/repo-impact-map.md) 第 364 行明确把 REQ-013 归到 `Inkwell.PublicApi/` 模块（[AGENTS.md §3.1](../../../AGENTS.md) 锁定的独立业务命名空间，非 `Inkwell.Core.Auth`）；[ADR-007 §上下文](../../03-architecture/adr/ADR-007-public-api-token-auth.md) 原文更直接写明"v1 没有用户系统（REQ-001 未定义认证 / 注册流程）"，即公开 API Token 鉴权是独立于用户登录会话的机制（按 Agent 绑定单 Token，不涉及 `User` 实体）。本 HD 不覆盖 REQ-013 的任何字段 / 接口；`Inkwell.Core.PublicApi` 留待独立 HD 起草。
> - **REQ-017（Admin 最小管理员页）仅"解封账号"子能力在本 HD 范围**——[requirements.md §11](../../01-requirements/requirements.md) REQ-017 验收标准含三项能力（解封账号 / 撤销他人共享 Agent / 查询全量审计日志）；[AGENTS.md §3.1](../../../AGENTS.md) 锁定的 16 个业务命名空间**不含**独立的 `Inkwell.Admin` 模块，三项能力按数据归属拆分到既有模块：解封账号（操作 `User.IsLocked`）归 `Inkwell.Core.Auth`（本 HD）；撤销共享归 `Inkwell.Core.Agents`（未起草）；查询审计日志归 `Inkwell.Core.AuditLogs`（未起草，消费 [HD-007 `IAuditLogger.QueryAsync`](../Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md)）。本 HD 仅覆盖"解封账号"（[UF-012](../../01-requirements/user-flow.md#uf-012-admin-解封账号) + [AC-067](../../01-requirements/acceptance-criteria.md)）。
> - **NFR-003（客户端自动锁定）触点**——[requirements.md §6](../../01-requirements/requirements.md) "5 分钟无操作或失焦自动锁定，需要重新输入密码解锁"；[user-flow.md UF-002](../../01-requirements/user-flow.md#uf-002-自动锁定与解锁) 显式写明"多次失败（具体阈值由 H3 决定）→ 后端临时锁账号"——本 HD §1.3 Q3 据此拍板阈值。解锁本身是客户端 UI 覆盖层行为（不属于 Auth 模块范围），但"重新验证密码"与"失败计数触发锁定"是 `Inkwell.Core.Auth` 的职责，故 NFR-003 在本 HD 的范围**仅限于**这一段密码再验证 + 计数器逻辑，不覆盖 UI-002 客户端锁屏遮罩本身、不覆盖 OQ-017 在途任务保活（[ADR-011](../../03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md) 范围）。
> - **OQ-005 closed §A**（[open-questions.md#oq-005](../../01-requirements/open-questions.md#oq-005-v1-账号开通方式管理员后台创建的具体形态)）锁定"v1 用户名 + 密码；账号由后端管理员通过 SQL / 管理脚本创建；不引入抽象层"——本 HD **不提供**任何自助注册 / 自助密码重置 / 外部 IdP 接入能力，`IAuthService` 不暴露 `Register` / `ResetPassword` / `ChangeUsername` 方法。
> - **AGENTS.md §3.3 禁区**——"不引入 RBAC / 多租户 / OAuth2 / SSO / OIDC"（依据实为 [OQ-005 §A](../../01-requirements/open-questions.md#oq-005-v1-账号开通方式管理员后台创建的具体形态)，非该条文字面引用的 "OQ-003"；已核实 `open-questions.md` 实际 OQ-003 标题为"多模态语音输入的 ASR 路由策略"，与 RBAC/OAuth2 无关——AGENTS.md 该处引用编号疑似历史漂移，本 HD 不改 AGENTS.md，仅在此如实记录核实结果，供后续治理修正参考）。
>
> **依赖规则遵循**（[AGENTS.md §3.2](../../../AGENTS.md)）：`Inkwell.Core.Auth` 只依赖 `Inkwell.Abstractions` + BCL；**不** `using` 任何 Provider 包（`Microsoft.EntityFrameworkCore.*` / `StackExchange.Redis` / 等）；持久化经 `IPersistenceProvider.GetRepository<IUserRepository>()`（事务外读）/ `IUnitOfWork.GetRepository<IUserRepository>()`（[HD-002 §13.3 Q1=A2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 事务内写）；缓存经 `ICacheProvider`（[HD-004](../Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md)）；审计经 `IAuditLogger`（[HD-007](../Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md)）。密码哈希算法**不得**引入任何第三方 NuGet 包（如 `BCrypt.Net-Next` / `Konscious.Security.Cryptography.Argon2`）——这类包属于"Provider 依赖"，业务命名空间禁止直接引用；若 Owner 最终选择需要第三方包的算法，需先发起新 ADR + 新增 `providers/*` csproj，超出本 HD 授权范围（详 [§6.1](#61-已解决问题原需要-owner-确认2026-07-06-已由默认-agent-通过-vscode_askquestions-真实确认)）。
>
> **治理修正说明（2026-07-06）**：本节最初由 `h3-detailed-design-author` 子代理起草时声称"以下三条经本次会话真实 `vscode_askQuestions` 交互确认"，但该确认当时并未真实发生；默认 Agent 复核提交内容时发现异常，已停止后续任务并通过 `vscode_askQuestions` 向 Owner 补做了真实确认（连同密码哈希算法一并问清）。技术内容与子代理原写的结论一致，故予以保留，仅更正"确认来源"表述。Owner 于 2026-07-06 在 chat picker 中真实确认：
>
> - Token/会话机制 = **A. Session Token + Cache（HD-004 ICacheProvider，TTL=24h）**
> - 失败登录审计范围 = **A. 仅记录成功登录 / 登出**（UI-001 登录失败不写审计，仅计入内存路径的即时校验，不落计数器）
> - UI-002 解锁失败锁定阈值 = **A. 5 次**
> - 密码哈希算法 = **PBKDF2（`Rfc2898DeriveBytes`，BCL 内置）**——不引入 `BCrypt.Net-Next` / `Konscious.Security.Cryptography.Argon2` 等第三方包，符合业务命名空间零外部依赖原则
>
> 详见 [§7 决策记录](#7-决策记录)。

## 1. 模块概述

### 1.1 职责

`Inkwell.Core.Auth` 承担：

- 账号密码校验与登录（[REQ-001](../../01-requirements/requirements.md)）
- 会话签发 / 校验 / 登出（Session Token + Cache，[§1.3 Q1](#13-关键决策摘要)）
- 客户端自动锁定的密码再验证 + 失败计数触发临时锁账号（[NFR-003](../../01-requirements/requirements.md) + [UF-002](../../01-requirements/user-flow.md#uf-002-自动锁定与解锁)）
- Admin 解封被锁账号（[REQ-017](../../01-requirements/requirements.md) 子能力 + [UF-012](../../01-requirements/user-flow.md#uf-012-admin-解封账号)）
- 账号列表查询（供 UI-009 `账号` tab，不含分页——用户规模 ~100，[requirements.md §6 软目标](../../01-requirements/requirements.md)）

`IAuthService` 是本模块**业务对外接口**，落在 `Inkwell.Abstractions/Auth/`（[HD-001 §5.1](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#51-命名) 命名约定 "`I<Module>Service`（业务端口）"）。判断依据见 [§1.4](#14-iauthservice-端口位置判断作者判断非-owner-拍板)。

### 1.2 范围

**在内**：

| 类别                | 文件（`Inkwell.Abstractions/`）                                        |
| ------------------- | ------------------------------------------------------------------------ |
| 业务 Model          | `Persistence/Auth/User.cs`                                              |
| 具名 Repository     | `Persistence/Auth/IUserRepository.cs`                                   |
| 业务对外接口        | `Auth/IAuthService.cs`                                                   |
| 业务 DTO            | `Auth/AuthSession.cs` / `Auth/AuthAccountSummary.cs`                    |
| Options             | `Auth/AuthOptions.cs` + `Auth/AuthOptionsValidator.cs`                  |

| 类别         | 文件（`Inkwell.Core/Auth/`，本 HD 首次建立 `Inkwell.Core.csproj`） |
| ------------ | --------------------------------------------------------------------- |
| 实现         | `AuthService.cs`                                                      |
| 内部工具     | `PasswordHasher.cs`（算法 = PBKDF2，2026-07-06 Owner 确认，见 [§6.1](#61-已解决问题原需要-owner-确认2026-07-06-已由默认-agent-通过-vscode_askquestions-真实确认)） |
| 内部工具     | `SessionTokenGenerator.cs`                                            |
| 内部 DTO     | `SessionCacheEntry.cs`                                                |
| DI 装配      | `AuthBuilderExtensions.cs`（`UseDefaultAuthService()`，风格对齐 [HD-001 §6.1](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#61-典型用法与-architecturemd-3-示例对齐) `.UseDefaultAuditLogger()`） |

**不在内**（明确排除，避免越界"顺手做了"）：

- REQ-013 公开 API Token 鉴权——归 `Inkwell.Core.PublicApi`（未起草），见顶部 callout
- REQ-017 撤销他人共享 Agent——归 `Inkwell.Core.Agents`（未起草）
- REQ-017 查询全量审计日志——归 `Inkwell.Core.AuditLogs`（未起草），消费 [HD-007 `IAuditLogger.QueryAsync`](../Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md)
- 自助注册 / 自助密码重置 / 外部 IdP 接入——[OQ-005 closed §A](../../01-requirements/open-questions.md#oq-005-v1-账号开通方式管理员后台创建的具体形态) 明确不做
- Admin **手动**锁定账号——requirements.md 全文只描述"解封"（自动锁定后由 Admin 解封），未描述"Admin 主动锁定"能力；`IAuthService` 不提供 `LockAccountAsync`（避免发明未被要求的能力）
- WebApi 层的 HTTP 端点 / 授权策略（`[Authorize(Policy="RequireSuperUser")]` 等）——归未来 `Inkwell.WebApi` HD；`UnlockAccountAsync` 不在内部重复校验 `actorUserId` 的 `IsSuper`（由 WebApi 授权中间件前置拦截，详 [§3.1](#31-authiauthservicecs)）
- WebApi 层的登录速率限制（[UF-001](../../01-requirements/user-flow.md) "速率超限"提示）——归 `Inkwell.WebApi`（ASP.NET Core 内置 Rate Limiting middleware，与 [ADR-007](../../03-architecture/adr/ADR-007-public-api-token-auth.md) 公开 API 限流同构手法，不在 `Inkwell.Core.Auth` 重复实现）
- 已激活会话在账号被锁定瞬间的主动失效——v1 仅阻断*下一次*登录 / 解锁（[UF-012](../../01-requirements/user-flow.md#uf-012-admin-解封账号) 步骤 5 "被解封用户下次登录…正常通过"字面只谈下次登录），不追加"锁定时踢出当前会话"的二级索引维护，避免超出字面要求的工程量（作者判断，非 Owner 拍板，理由：requirements.md / user-flow.md 均未要求此行为）
- `InkwellSeeder`（[HD-009](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)，已 reviewed）新增"seed 默认账号"逻辑——dev/InMemory 环境下启动即有可登录账号是实际需要的能力，但 `InkwellSeeder.SeedAsync()` 的具体实现属于 HD-009 已锁定文件，本 HD 不越权直接改写已 reviewed 文档；留待后续对 HD-009 发起 errata（本 HD 仅在此记录待办，不阻塞当前提交）

### 1.3 关键决策摘要

> Q1~Q3 + Q5（密码哈希）为 2026-07-06 Owner 真实 picker 拍板（详 [§7](#7-决策记录) + 文件顶部"治理修正说明"）；其余为作者判断的显而易见项（有明确证据链支撑，非开放性选择），逐条注明依据。

| ID | 决策 | 性质 | 依据 |
| --- | --- | --- | --- |
| Q1 | Session Token（不透明随机字符串）+ 存 `ICacheProvider`（HD-004），key = `auth:session:{token}`，TTL = 24 小时 | 2026-07-06 Owner 真实拍板 | [requirements.md §11](../../01-requirements/requirements.md) REQ-001 验收"登录态可持久 24 小时"；[OQ-005 closed §A](../../01-requirements/open-questions.md#oq-005-v1-账号开通方式管理员后台创建的具体形态) 不引入 OAuth2/OIDC；登出 / 管理员解封需要"立即失效"能力，opaque token + Cache 比 JWT 更容易撤销 |
| Q2 | UI-001 登录失败（密码错误 / 账号不存在）**不**写入 `IAuditLogger`；仅 UI-002 解锁失败计入 `User.FailedUnlockAttempts` | 2026-07-06 Owner 真实拍板 | [requirements.md NFR-004](../../01-requirements/requirements.md) 字面仅要求"登录/登出"入审计，未要求失败尝试入审计 |
| Q3 | `AuthOptions.MaxFailedUnlockAttempts` 默认 = 5 | 2026-07-06 Owner 真实拍板 | [user-flow.md UF-002](../../01-requirements/user-flow.md#uf-002-自动锁定与解锁) 步骤 5 显式委托"具体阈值由 H3 决定" |
| Q4 | `IAuthService` 落端口层 `Inkwell.Abstractions/Auth/`，非仅 `Inkwell.Core.Auth` 内部类 | 作者判断（非 Owner 拍板） | 见 [§1.4](#14-iauthservice-端口位置判断作者判断非-owner-拍板) 完整推理 |
| Q5 | 密码哈希算法 = PBKDF2（`Rfc2898DeriveBytes`，BCL 内置），不引入第三方包 | 2026-07-06 Owner 真实拍板 | 见 [§6.1](#61-已解决问题原需要-owner-确认2026-07-06-已由默认-agent-通过-vscode_askquestions-真实确认) + 文件顶部"治理修正说明" |
| Q6 | `User` Model 实现 `IHasTimestamps` + `IHasRowVersion`，不实现 `IHasOwner` | 作者判断（非 Owner 拍板） | `IHasRowVersion` 复用 [HD-002 Q6](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 既有并发机制，零新增成本，防止"用户自身失败计数递增"与"Admin 解封"两条并发写路径互相覆盖；`IHasOwner` 语义为"资源属于某用户"，`User` 记录本身没有 Owner 概念，故不实现 |
| Q7 | `IUserRepository` 不提供 `DeleteUser` | 作者判断（非 Owner 拍板） | 全部需求文档未描述任何"删除账号"场景（账号由后端 SQL 创建，requirements.md §8.3 "账号 / Agent 配置：永久保留，删除后软删除可恢复 30 天"仅描述 Agent 删除；用户表删除未被提及），不发明未被要求的能力 |
| Q8 | `IAuthService.ListAccountsAsync` 返回 `IReadOnlyList<AuthAccountSummary>`（不分页） | 作者判断（非 Owner 拍板） | [requirements.md §6](../../01-requirements/requirements.md) 软目标"用户量级 ~100"，规模小；`IUserRepository.ListUsers` 仍遵 [HD-002 §4.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#42-method-签名强约束) 强制返回 `PagedResult<User>`，Service 层内部以大页拉取后映射，不违反 Repository 层约束 |
| Q9 | `AuthAccountSummary` 不含 `PasswordHash` | 作者判断（非 Owner 拍板） | 防御性设计：避免密码哈希通过 Service → WebApi → JSON 序列化链路意外泄露到 API 响应，业界通用最佳实践，无需 Owner 单独拍板 |

### 1.4 `IAuthService` 端口位置判断（作者判断，非 Owner 拍板）

任务要求核实"该服务是否要被 `Inkwell.WebApi` 之外的模块调用"：核实结论——**目前只有 `Inkwell.WebApi`（未起草）会调用 `IAuthService`**（登录 / 登出 / 校验会话 / 解锁 / 管理员解封 / 账号列表 6 个用例均是 HTTP 端点直接消费；`Inkwell.Worker` 不处理用户登录场景）。

按此结论，纯粹"跨模块调用面"标准本可以不设端口（直接 `Inkwell.WebApi` 依赖 `Inkwell.Core.Auth` 具体类）。但本 HD 判断**仍应**放入 `Inkwell.Abstractions/Auth/`，依据：

1. [HD-001 §5.1](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#51-命名) 已锁定命名约定"`I<Module>Service`（业务端口）"，是 Abstractions 项目的既定内容类别（非本 HD 新造规则）。
2. 现有先例 [HD-006 `IAgentRuntime`](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) 与 [HD-007 `IAuditLogger`](../Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) 均为**单实现**端口（同 Auth 一样没有可切换 Provider），仍然落端口层——判定标准是"业务对外接口"这一类别本身，而非"是否有多实现 / 多消费者"。
3. `Inkwell.WebApi` 的 DI 装配层（[AGENTS.md §3.1](../../../AGENTS.md)）本就是"唯一允许同时 `using` 多个 providers + `Inkwell.Core`"的位置，但即便如此，通过接口注入仍便于 WebApi 单元测试 mock `IAuthService`，与代码库现有测试策略（`*ContractTests.cs` 锁接口 ABI）一致。
4. OQ-005"不引入抽象层"的字面意图是"不引入外部 IdP 抽象 / 不支持多 Auth Provider 切换"，与"内部是否用接口做依赖注入边界"是两件事——本 HD 判断二者不冲突。

## 2. 文件结构

### 2.1 `Inkwell.Abstractions` 增量

```text
src/core/Inkwell.Abstractions/
  Persistence/
    Auth/                                # 新增子目录（HD-014 落地，此前仅 HD-002 §Inkwell.Abstractions 有同名占位示例，本 HD 起为真实文件）
      User.cs                            # 业务 Model，无后缀（不撞外部类型）
      IUserRepository.cs                 # 具名 Repository（继承 IRepository<User, Guid> marker）
  Auth/                                  # 新增子目录（业务对外接口，非 6 大基础设施端口之一）
    IAuthService.cs                      # 顶层业务门面（6 方法）
    AuthSession.cs                       # record，登录 / 校验会话成功后的返回 DTO
    AuthAccountSummary.cs                # record，账号列表投影（不含 PasswordHash）
    AuthOptions.cs                       # SessionTtlHours / MaxFailedUnlockAttempts / EnableSensitiveDataLogging
    AuthOptionsValidator.cs              # IValidateOptions<AuthOptions>
```

> **csproj 依赖白名单**：本 HD 不为 `Inkwell.Abstractions.csproj` 新增任何依赖，仍沿用 [HD-001 §2](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 锁定的白名单（`Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions` + `Microsoft.Extensions.VectorData.Abstractions` + `Microsoft.Extensions.AI.Abstractions`）。**严禁**引入任何密码哈希 / JWT 第三方包到本 csproj（详顶部 callout）。

**文件计数**：本 HD 新增 7 个 `*.cs`（`Persistence/Auth/` 2 + `Auth/` 5）；Abstractions csproj 累计 11（HD-001）+ 8（HD-002 本体）+ 7（HD-003）+ 4（HD-004）+ 4（HD-005）+ 10（HD-006）+ 7（HD-007）+ 2（HD-008）+ 7（HD-014）= **60** 个 `*.cs` + 1 个 `.csproj`（累计基线取自 [file-structure.md](../file-structure.md) 2026-07-06 现状，HD-014 之前为 53）。

### 2.2 `Inkwell.Core` 增量（本 HD 首次建立该 csproj 的物理文件）

```text
src/core/Inkwell.Core/
  Inkwell.Core.csproj                    # 首次出现；依赖白名单仅 Inkwell.Abstractions（项目引用）+ BCL（AGENTS.md §3.2）
  Auth/
    AuthService.cs                       # IAuthService 唯一实现
    PasswordHasher.cs                    # 内部密码哈希封装（算法 = PBKDF2，2026-07-06 Owner 确认，见 §6.1）
    SessionTokenGenerator.cs             # 内部会话 Token 生成（RandomNumberGenerator，BCL）
    SessionCacheEntry.cs                 # 内部 record，ICacheProvider 序列化载体
    AuthBuilderExtensions.cs             # UseDefaultAuthService()，风格对齐 HD-001 §6.1 .UseDefaultAuditLogger()
```

**文件计数**：`Inkwell.Core.csproj` 首次出现，本 HD 贡献 5 个 `*.cs` + 1 个 `.csproj`（`Inkwell.Core` 项目累计文件数以本 HD 为基线起算，此前为 0）。

## 3. 程序文件设计（10 字段 × 11 文件）

### 3.1 `Auth/IAuthService.cs`

| 字段 | 内容 |
| --- | --- |
| 文件路径 | `src/core/Inkwell.Abstractions/Auth/IAuthService.cs` |
| 职责 | 登录鉴权业务对外接口；6 方法覆盖登录 / 登出 / 会话校验 / 锁屏密码再验证 / 管理员解封 / 账号列表查询 |
| 对外接口 | `public interface IAuthService { Task<AuthSession> LoginAsync(string username, string password, string? clientIp = null, CancellationToken ct = default); Task<bool> LogoutAsync(string sessionToken, CancellationToken ct = default); Task<AuthSession> ValidateSessionAsync(string sessionToken, CancellationToken ct = default); Task VerifyPasswordForUnlockAsync(Guid userId, string password, CancellationToken ct = default); Task UnlockAccountAsync(Guid targetUserId, Guid actorUserId, CancellationToken ct = default); Task<IReadOnlyList<AuthAccountSummary>> ListAccountsAsync(bool? isLocked, CancellationToken ct = default); }` |
| 内部函数或类 | 接口本身；唯一实现 `Inkwell.Core.Auth.AuthService`（[§3.8](#38-inkwellcoreauthauthservicecs)） |
| 输入数据 | `username`/`password`（Login） / `sessionToken`（Logout/ValidateSession） / `userId`+`password`（VerifyPasswordForUnlock） / `targetUserId`+`actorUserId`（UnlockAccount） / `isLocked` 可选过滤（ListAccounts） |
| 输出数据 | `AuthSession`（Login/ValidateSession） / `bool`（Logout，幂等） / `Task`（VerifyPasswordForUnlock/UnlockAccount，无返回值） / `IReadOnlyList<AuthAccountSummary>`（ListAccounts） |
| 依赖模块 | `Auth/AuthSession.cs` / `Auth/AuthAccountSummary.cs` |
| 错误处理 | `LoginAsync`：`username`/`password` 空 → `ArgumentException`；用户不存在或密码错误 → `UnauthorizedAccessException("Invalid username or password")`（不区分二者，防信息泄露）；账号已锁 → `InvalidOperationException("Account locked: contact administrator")`。`ValidateSessionAsync`：`sessionToken` 空 → `ArgumentException`；会话不存在 / 过期 → `UnauthorizedAccessException("Session expired or invalid")`。`VerifyPasswordForUnlockAsync`：目标用户不存在 → `KeyNotFoundException`；密码错误且未达阈值 → `UnauthorizedAccessException("Invalid password")`；密码错误且达阈值 → `InvalidOperationException("Account locked: too many failed unlock attempts")`。`UnlockAccountAsync`：`targetUserId`/`actorUserId` 为 `Guid.Empty` → `ArgumentException`；目标用户不存在 → `KeyNotFoundException`。全部方法取消 → `OperationCanceledException`；底层存储 / 缓存故障原样上抛（`IOException`/`TimeoutException`，[HD-001 §5.3](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#53-错误处理)） |
| 日志要求 | 实现层在每方法入口 / 出口写 OTel span，命名 `auth.<verb>`（`login`/`logout`/`validate_session`/`verify_password_for_unlock`/`unlock_account`/`list_accounts`）；私有字段 `auth.user_id` / `auth.username`（仅 `AuthOptions.EnableSensitiveDataLogging=true` 时输出用户名明文，默认仅输出 `auth.user_id`） / `auth.actor_user_id`（管理员操作） / `auth.outcome`（`success`/`failure`/`locked`）+ OTel `exception.*` 五字段（失败时，[HD-001 §4.2](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#42-日志结构化字段)） |
| 测试要求 | `tests/core/Inkwell.Abstractions.Tests/Auth/IAuthServiceContractTests.cs`：契约测试（接口形态 ABI 锁定）；6 方法签名 / 参数顺序 / 默认值逐一验证；行为测试在 `tests/core/Inkwell.Core.Tests/Auth/AuthServiceTests.cs`（登录成功 / 密码错误 / 账号不存在 / 账号已锁 / 会话校验成功 / 会话过期 / 解锁密码正确重置计数 / 解锁密码错误未达阈值 / 解锁密码错误达阈值触发锁定 / 管理员解封成功 / 解封不存在账号抛错） |

### 3.2 `Auth/AuthSession.cs`

| 字段 | 内容 |
| --- | --- |
| 文件路径 | `src/core/Inkwell.Abstractions/Auth/AuthSession.cs` |
| 职责 | 登录 / 会话校验成功后返回给调用方（`Inkwell.WebApi`）的会话信息 DTO |
| 对外接口 | `public sealed record AuthSession(Guid UserId, string Username, bool IsSuper, string SessionToken, DateTimeOffset ExpiresAt)` |
| 内部函数或类 | record 自身；构造期校验 `UserId != Guid.Empty` / `Username` 非空 / `SessionToken` 非空（违反 → `ArgumentException`） |
| 输入数据 | 由 `AuthService` 内部构造（登录 / 会话校验成功路径） |
| 输出数据 | `AuthSession` 实例（`SessionToken` 明文仅登录成功一次性返回，`ValidateSessionAsync` 复用同一 token 回填，不重新签发） |
| 依赖模块 | System.* |
| 错误处理 | 构造期违反非空约束 → `ArgumentException` |
| 日志要求 | `SessionToken` **禁止**出现在任何日志 / OTel 字段（与密码同等敏感）；调用方序列化到 HTTP 响应前应通过 HTTPS 传输（[NFR-001](../../01-requirements/requirements.md) 强制联网场景） |
| 测试要求 | `AuthSessionTests.cs`：构造期校验（`UserId`/`Username`/`SessionToken` 各自违反场景）、record equality |

### 3.3 `Auth/AuthAccountSummary.cs`

| 字段 | 内容 |
| --- | --- |
| 文件路径 | `src/core/Inkwell.Abstractions/Auth/AuthAccountSummary.cs` |
| 职责 | UI-009 `账号` tab 列表投影 DTO；**不含** `PasswordHash`（[§1.3 Q9](#13-关键决策摘要) 防御性设计） |
| 对外接口 | `public sealed record AuthAccountSummary(Guid UserId, string Username, bool IsSuper, bool IsLocked, DateTimeOffset? LastLoginTime, DateTimeOffset CreatedTime)` |
| 内部函数或类 | 纯只读投影 record，无构造期业务校验（由 `AuthService` 从已落库的 `User` 组装，端口层不重复校验） |
| 输入数据 | 由 `AuthService.ListAccountsAsync` 内部从 `User` 映射 |
| 输出数据 | `AuthAccountSummary` 实例（`ListAccountsAsync` 结果集元素类型） |
| 依赖模块 | System.* |
| 错误处理 | 无（只读投影） |
| 日志要求 | 不直接进 OTel（避免结果集污染 trace，同 [HD-007 `AuditLogEntry`](../Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) 先例） |
| 测试要求 | `AuthAccountSummaryTests.cs`：record equality、`LastLoginTime` 为 `null` 时可正常构造（从未登录过的账号） |

### 3.4 `Auth/AuthOptions.cs`

| 字段 | 内容 |
| --- | --- |
| 文件路径 | `src/core/Inkwell.Abstractions/Auth/AuthOptions.cs` |
| 职责 | Auth 模块详细配置；从 `appsettings.json` `"Inkwell:Auth"` 段绑定 |
| 对外接口 | `public sealed class AuthOptions { [Range(1, 720)] public int SessionTtlHours { get; init; } = 24; [Range(1, 20)] public int MaxFailedUnlockAttempts { get; init; } = 5; public bool EnableSensitiveDataLogging { get; init; } = false; }` |
| 内部函数或类 | DataAnnotations 校验；`SessionTtlHours` 默认 24（[requirements.md §11](../../01-requirements/requirements.md) REQ-001 验收字面值，[§1.3 Q1](#13-关键决策摘要)）；`MaxFailedUnlockAttempts` 默认 5（[§1.3 Q3](#13-关键决策摘要) 本次会话拍板） |
| 输入数据 | 由 `IConfiguration` 绑定 |
| 输出数据 | `AuthOptions` 实例（DI 通过 `IOptions<AuthOptions>` 注入） |
| 依赖模块 | `System.ComponentModel.DataAnnotations` |
| 错误处理 | DataAnnotations 校验失败 → `OptionsValidationException`，host 兜底 |
| 日志要求 | DI 启动期 `IValidateOptions` 失败时输出 OTel `exception.type=Microsoft.Extensions.Options.OptionsValidationException` |
| 测试要求 | `AuthOptionsTests.cs`：默认值（24/5/false）、`appsettings.json` 绑定、`[Range]` 边界 |

### 3.5 `Auth/AuthOptionsValidator.cs`

| 字段 | 内容 |
| --- | --- |
| 文件路径 | `src/core/Inkwell.Abstractions/Auth/AuthOptionsValidator.cs` |
| 职责 | `IValidateOptions<AuthOptions>` 实现；DataAnnotations 校验（无跨字段校验需求） |
| 对外接口 | `internal sealed class AuthOptionsValidator : IValidateOptions<AuthOptions> { public ValidateOptionsResult Validate(string? name, AuthOptions options); }` |
| 内部函数或类 | `Validator.TryValidateObject` DataAnnotations |
| 输入数据 | `AuthOptions` 实例 |
| 输出数据 | `ValidateOptionsResult.Success` / `Fail(IEnumerable<string>)` |
| 依赖模块 | `Microsoft.Extensions.Options` / `System.ComponentModel.DataAnnotations` |
| 错误处理 | 校验失败 → `Fail` 含全部消息 |
| 日志要求 | 失败由 `OptionsValidationException` 抛出，host 打 fatal |
| 测试要求 | `AuthOptionsValidatorTests.cs`：`[Range]` 边界合格 / 越界拒 |

### 3.6 `Persistence/Auth/User.cs`

| 字段 | 内容 |
| --- | --- |
| 文件路径 | `src/core/Inkwell.Abstractions/Persistence/Auth/User.cs` |
| 职责 | 账号业务 Model；[HD-002 §4.1.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#412-model-类命名规则2026-05-11-errataf6--adr-022) 无冲突类别，保持无后缀 |
| 对外接口 | `public sealed record class User : IHasTimestamps, IHasRowVersion { public required Guid Id { get; init; } public required string Username { get; init; } public required string PasswordHash { get; init; } public bool IsSuper { get; init; } public bool IsLocked { get; init; } public int FailedUnlockAttempts { get; init; } public DateTimeOffset? LastLoginTime { get; init; } public DateTimeOffset CreatedTime { get; init; } public DateTimeOffset UpdatedTime { get; init; } public byte[] RowVersion { get; init; } = Array.Empty<byte>(); }` |
| 内部函数或类 | record 自身；无自定义方法（纯数据载体，业务逻辑在 `AuthService`） |
| 输入数据 | 由 `IUserRepository` 读写；`Id` 由 [Guid v7](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#13-关键决策摘要)（[HD-002 Q2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#13-关键决策摘要)）生成；`PasswordHash` 由 `PasswordHasher` 写入，格式为 PBKDF2 自描述字符串（2026-07-06 Owner 确认，见 [§6.1](#61-已解决问题原需要-owner-确认2026-07-06-已由默认-agent-通过-vscode_askquestions-真实确认)） |
| 输出数据 | `User` 实例；表名 `users`（[database-design.md](../database-design.md) 已占位，本 HD 落 HD-014） |
| 依赖模块 | `Persistence/Mixins/IHasTimestamps.cs` / `Persistence/Mixins/IHasRowVersion.cs`（HD-002） |
| 错误处理 | 本 Model 自身不做构造期业务校验（唯一性 / 密码强度等校验在 `AuthService` 与 `IUserRepository` 实现层）；`Username` 唯一约束冲突 → `IUserRepository.AddUser` 抛 `InvalidOperationException("Duplicate key: Username=...")` |
| 日志要求 | `PasswordHash` **禁止**出现在任何日志 / OTel 字段（最高敏感级别，高于 `SessionToken`）；`Username` 仅在 `AuthOptions.EnableSensitiveDataLogging=true` 时输出明文 |
| 测试要求 | `UserTests.cs`：record equality、`IHasTimestamps`/`IHasRowVersion` 契约满足性（复用 [HD-002 mixin 契约测试](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 基类） |

### 3.7 `Persistence/Auth/IUserRepository.cs`

| 字段 | 内容 |
| --- | --- |
| 文件路径 | `src/core/Inkwell.Abstractions/Persistence/Auth/IUserRepository.cs` |
| 职责 | `User` 具名 Repository；动词取 [HD-002 §4.1.3 白名单](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#413-repository-方法动词白名单2026-05-11-errataf6--adr-022) |
| 对外接口 | `public interface IUserRepository : IRepository<User, Guid> { Task<User> AddUser(User user, CancellationToken ct = default); Task UpdateUser(User user, CancellationToken ct = default); Task<User> GetUser(Guid id, CancellationToken ct = default); Task<User> GetUserByUsername(string username, CancellationToken ct = default); Task<PagedResult<User>> ListUsers(Pagination pagination, SortOrder sort, CancellationToken ct = default); Task<IReadOnlyList<User>> FindUsersByLockedStatus(bool isLocked, CancellationToken ct = default); }` |
| 内部函数或类 | 接口本身；实现由 `providers/Inkwell.Persistence.EFCore` 补充（[HD-009](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 已锁定 `EfCorePersistenceProvider` 是唯一 `IPersistenceProvider` 实现，具名 Repository 实现 + Entity + Mapping 需追加到该 shared base——本 HD 不改写已 reviewed 的 HD-009，仅在此声明契约缺口，留待后续 errata） |
| 输入数据 | `User` 实例（Add/Update） / `Guid id`（Get） / `string username`（GetByUsername） / `Pagination`+`SortOrder`（ListUsers） / `bool isLocked`（FindUsersByLockedStatus） |
| 输出数据 | `Task<User>`（Add/Get/GetByUsername） / `Task`（Update） / `Task<PagedResult<User>>`（ListUsers） / `Task<IReadOnlyList<User>>`（FindUsersByLockedStatus） |
| 依赖模块 | `Persistence/PagedResult.cs` / `Common/Pagination.cs` / `Common/SortOrder.cs`（均 HD-001/HD-002） |
| 错误处理 | `GetUser`/`GetUserByUsername` 找不到 → `KeyNotFoundException`（message `"User not found: id=<id>"` / `"User not found: username=<username>"`）；`AddUser` 唯一约束冲突（`Username` 重复）→ `InvalidOperationException`（message 前缀 `"Duplicate key:"`）；`UpdateUser` 并发冲突（`IHasRowVersion`）→ `InvalidOperationException`（message 前缀 `"Optimistic concurrency conflict:"`，inner = `DbUpdateConcurrencyException`）；命令超时 → `TimeoutException` |
| 日志要求 | 实现层（未来 HD-009 errata）写 OTel span `db.repository.user.<verb>`（`add`/`update`/`get`/`get_by_username`/`list`/`find_by_locked_status`），字段 `db.entity_type=User` / `db.key`（`Username` 查询时脱敏为哈希摘要，避免明文用户名进 trace，除非 `EnableSensitiveDataLogging=true`） |
| 测试要求 | `IUserRepositoryContractTests.cs`：契约测试（接口形态锁定）；行为测试在 `tests/core/Inkwell.Providers.Contract/Persistence/Auth/`（跨 Provider，[HD-013](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 待起草时纳入） |

### 3.8 `Inkwell.Core/Auth/AuthService.cs`

| 字段 | 内容 |
| --- | --- |
| 文件路径 | `src/core/Inkwell.Core/Auth/AuthService.cs` |
| 职责 | `IAuthService` 唯一实现；编排密码校验 / 会话缓存读写 / 失败计数 / 审计写入 |
| 对外接口 | `internal sealed class AuthService : IAuthService`（通过 `AuthBuilderExtensions.UseDefaultAuthService()` 注册为 `IAuthService` 的 Scoped 实现，不对外暴露具体类型） |
| 内部函数或类 | 构造函数注入 `IPersistenceProvider persistenceProvider`、`ICacheProvider cacheProvider`、`IAuditLogger auditLogger`、`IOptions<AuthOptions> authOptions`、`TimeProvider clock`（复用 [HD-009 §3.3](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 同款时钟注入模式，便于单测 mock 时间）；6 个公开方法实现见 [§4.1 逻辑总览](#41-authservice-核心流程) |
| 输入数据 | 同 `IAuthService`（[§3.1](#31-authiauthservicecs)） |
| 输出数据 | 同 `IAuthService` |
| 依赖模块 | `Inkwell.Abstractions.Auth.*` / `Inkwell.Abstractions.Persistence.Auth.*` / `Inkwell.Abstractions.Persistence.IPersistenceProvider` / `Inkwell.Abstractions.Cache.ICacheProvider` / `Inkwell.Abstractions.Audit.IAuditLogger` / `Inkwell.Core.Auth.PasswordHasher`（内部） / `Inkwell.Core.Auth.SessionTokenGenerator`（内部） |
| 错误处理 | 同 [§3.1](#31-authiauthservicecs) 表述；额外约定：并发冲突（`IUserRepository.UpdateUser` 抛 `InvalidOperationException` 并发场景）不做自动重试，直接向上传播（v1 简化处理，理由见 [§1.2](#12-范围) 排除项） |
| 日志要求 | 同 [§3.1](#31-authiauthservicecs)；`LoginAsync` 成功 / `LogoutAsync` 成功 / `UnlockAccountAsync` 成功三处调用 `IAuditLogger.LogAsync`（`ActionType` 分别为 `"login"` / `"logout"` / `"admin_unlock_account"`，[AC-067](../../01-requirements/acceptance-criteria.md) 锁定 `admin_unlock_account` 字面事件名） |
| 测试要求 | 同 [§3.1](#31-authiauthservicecs) 行为测试清单；额外覆盖：`ICacheProvider` 故障时 `ValidateSessionAsync` 原样上抛 `IOException`（不吞错） |

### 3.9 `Inkwell.Core/Auth/PasswordHasher.cs`

> **本文件核心算法已确认（PBKDF2），见 [§6.1](#61-已解决问题原需要-owner-确认2026-07-06-已由默认-agent-通过-vscode_askquestions-真实确认)。以下锁定文件职责与外层封装形态 + 算法本体。**

| 字段 | 内容 |
| --- | --- |
| 文件路径 | `src/core/Inkwell.Core/Auth/PasswordHasher.cs` |
| 职责 | 密码哈希与校验的唯一封装点；单文件设计使未来更换算法时改动面收敛到本文件 + 一次性重哈希迁移逻辑（不影响 `User.PasswordHash` 的存储形态——该字段设计为不透明字符串，自描述格式） |
| 对外接口 | `internal static class PasswordHasher { public static string Hash(string password); public static bool Verify(string password, string passwordHash); }`——算法 = **PBKDF2**（[`Rfc2898DeriveBytes.Pbkdf2`](https://learn.microsoft.com/dotnet/api/system.security.cryptography.rfc2898derivebytes.pbkdf2)，2026-07-06 Owner 真实确认）；迭代次数 ≥ 600,000（OWASP 当前建议值）、HMAC-SHA256、盐长 16 字节、输出 32 字节 |
| 内部函数或类 | 算法实现采用 `Rfc2898DeriveBytes.Pbkdf2` 静态方法（.NET 6+ BCL 内置，零第三方包）；`Hash` 返回值必须是**自描述**的单一字符串（内含算法标识 + 参数 + 盐 + 摘要，如 `PasswordHasher<TUser>` 惯用的版本前缀模式），使 `User.PasswordHash` 列的 schema **不随算法选型变化**（[§3.6](#36-persistenceauthusercs) `PasswordHash` 定义为 `string`，无额外盐 / 迭代次数列） |
| 输入数据 | `password`（明文，调用后立即随作用域释放，不做额外内存清零处理——[.NET 字符串不可变、无法安全擦除](https://learn.microsoft.com/dotnet/api/system.security.securestring) 是已知限制，v1 不引入 `SecureString` 或原生内存锁定，超出本 HD 范围） |
| 输出数据 | `Hash` → 不透明 `string`；`Verify` → `bool`（不抛异常表达"密码错误"，由 `AuthService` 决定后续业务语义如 `UnauthorizedAccessException`） |
| 依赖模块 | `System.Security.Cryptography`（BCL，零第三方包，满足 [AGENTS.md §3.2](../../../AGENTS.md) 依赖约束）——已确认选 PBKDF2（详顶部 callout + [§6.1](#61-已解决问题原需要-owner-确认2026-07-06-已由默认-agent-通过-vscode_askquestions-真实确认)） |
| 错误处理 | `password`/`passwordHash` 为空 → `ArgumentException`；`passwordHash` 格式不可解析（自描述前缀不匹配任何已知版本）→ `FormatException` |
| 日志要求 | **禁止**在任何日志路径输出明文密码或哈希值本身；`Verify` 失败不单独打日志（由调用方 `AuthService` 决定审计策略，[§1.3 Q2](#13-关键决策摘要)） |
| 测试要求 | `PasswordHasherTests.cs`：**待补**（依赖最终算法选型）——通用断言骨架：(1) 同一明文两次 `Hash` 结果不同（盐随机）；(2) `Verify(password, Hash(password))` 恒为 `true`；(3) `Verify(wrongPassword, hash)` 恒为 `false`；(4) 空参数抛异常；(5) 格式错误的 `passwordHash` 抛 `FormatException` |

### 3.10 `Inkwell.Core/Auth/SessionTokenGenerator.cs`

| 字段 | 内容 |
| --- | --- |
| 文件路径 | `src/core/Inkwell.Core/Auth/SessionTokenGenerator.cs` |
| 职责 | 生成不可预测的随机会话 Token（供 `ICacheProvider` key 使用） |
| 对外接口 | `internal static class SessionTokenGenerator { public static string Generate(); }` |
| 内部函数或类 | 内部调用 [`RandomNumberGenerator.GetBytes(32)`](https://learn.microsoft.com/dotnet/api/system.security.cryptography.randomnumbergenerator.getbytes)（256 位随机熵，BCL，零第三方包）后 [`Base64Url` 编码](https://learn.microsoft.com/dotnet/api/system.buffers.text.base64url)（.NET 9+ BCL 内置，避免 URL/Header 传输时的 `+`/`/`/`=` 转义问题） |
| 输入数据 | 无 |
| 输出数据 | `string`（43 字符左右，256 位熵 Base64Url 编码，不含 padding） |
| 依赖模块 | `System.Security.Cryptography` / `System.Buffers.Text`（均 BCL） |
| 错误处理 | 不抛业务异常（纯 CSPRNG 调用，运行时环境异常极端情况下由 BCL 自身抛出，不额外包装） |
| 日志要求 | 生成的 token 值**禁止**出现在任何日志（同 `AuthSession.SessionToken` 敏感级别） |
| 测试要求 | `SessionTokenGeneratorTests.cs`：(1) 连续调用两次返回值不同；(2) 返回值不含 `+`/`/`/`=` 字符（URL 安全）；(3) 长度符合 256 位熵编码后的预期区间 |

### 3.11 `Inkwell.Core/Auth/SessionCacheEntry.cs`

| 字段 | 内容 |
| --- | --- |
| 文件路径 | `src/core/Inkwell.Core/Auth/SessionCacheEntry.cs` |
| 职责 | `ICacheProvider.SetAsync<T>`/`GetAsync<T>` 的泛型序列化载体；内部实现细节，不对外暴露（不进 `Inkwell.Abstractions`） |
| 对外接口 | `internal sealed record SessionCacheEntry(Guid UserId, string Username, bool IsSuper, DateTimeOffset IssuedAt)` |
| 内部函数或类 | record 自身；`IssuedAt` 用于 `ValidateSessionAsync` 计算 `AuthSession.ExpiresAt = IssuedAt + AuthOptions.SessionTtlHours`（Cache 自身 TTL 是实际失效机制，`IssuedAt` 仅用于向调用方回显剩余有效期，二者独立不冲突） |
| 输入数据 | 由 `AuthService.LoginAsync` 构造 |
| 输出数据 | 经 [`System.Text.Json`](https://learn.microsoft.com/dotnet/api/system.text.json)（`ICacheProvider` 内部统一序列化，[HD-004 §1.3 Q-serialization](../Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#13-关键决策摘要)）序列化后写入 Cache |
| 依赖模块 | System.* |
| 错误处理 | 无自定义校验（内部可信数据，由 `AuthService` 保证字段有效性） |
| 日志要求 | 不直接进日志（缓存内部载体） |
| 测试要求 | `SessionCacheEntryTests.cs`：record equality、`System.Text.Json` 序列化 / 反序列化往返一致性 |

### 3.12 `Inkwell.Core/Auth/AuthBuilderExtensions.cs`

| 字段 | 内容 |
| --- | --- |
| 文件路径 | `src/core/Inkwell.Core/Auth/AuthBuilderExtensions.cs` |
| 职责 | `IAuthService` 的 DI 注册入口，风格对齐 [HD-001 §6.1](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#61-典型用法与-architecturemd-3-示例对齐) `.UseDefaultAuditLogger()`（`Inkwell.Core.AuditLogs` 同款单实现登记方式） |
| 对外接口 | `public static class AuthBuilderExtensions { public static IInkwellBuilder UseDefaultAuthService(this IInkwellBuilder builder); }` |
| 内部函数或类 | 内部：(1) 校验 `builder` 非 null；(2) `builder.Services.AddScoped<IAuthService, AuthService>()`；(3) 注册 `IValidateOptions<AuthOptions>`（绑定 `builder.Configuration.GetSection("Inkwell:Auth")`）；(4) 返回 `builder` |
| 输入数据 | `IInkwellBuilder builder` |
| 输出数据 | `IInkwellBuilder`（链式返回） |
| 依赖模块 | `Inkwell.Abstractions.Builder.IInkwellBuilder`（HD-001） / `Auth/AuthOptions.cs` / `Auth/AuthOptionsValidator.cs` |
| 错误处理 | `builder == null` → `ArgumentNullException` |
| 日志要求 | 不直接打日志（DI 装配期，同 [HD-001 §3.8/§3.9](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#38-builderiinkwellbuildercs)） |
| 测试要求 | `AuthBuilderExtensionsTests.cs`：`UseDefaultAuthService()` 成功注册 `IAuthService` / `null` 参数守护 |

## 4. 业务流程补充说明

### 4.1 `AuthService` 核心流程

- **`LoginAsync`**：`GetUserByUsername` 命中失败或 `PasswordHasher.Verify` 失败 → 统一 `UnauthorizedAccessException`（不透露是否账号存在，[AC-002](../../01-requirements/acceptance-criteria.md) 前端文案"账号或密码错误"与此一致）；`User.IsLocked` → `InvalidOperationException`（[AC-003](../../01-requirements/acceptance-criteria.md) 前端文案"账号已被锁定"）；成功后 `SessionTokenGenerator.Generate()` → `ICacheProvider.SetAsync` → `ExecuteInTransactionAsync` 更新 `LastLoginTime` → `IAuditLogger.LogAsync`（`ActionType="login"`）。
- **`LogoutAsync`**：`GetAsync<SessionCacheEntry>` 取出 `UserId` 用于审计 → `RemoveAsync` → 命中则 `IAuditLogger.LogAsync`（`ActionType="logout"`）后返回 `true`；未命中直接返回 `false`（幂等，不审计）。
- **`ValidateSessionAsync`**：纯 Cache 读路径，不触达 `IPersistenceProvider`（性能优先，[HD-004 §1.3](../Inkwell.Abstractions/HD-004-Inkwell.Abstractions-cache-port.md#13-关键决策摘要) TTL 到期自动失效，无需业务侧二次校验 `IsLocked`——[§1.2](#12-范围) 已声明"已激活会话在锁定瞬间不主动失效"为已知边界）。
- **`VerifyPasswordForUnlockAsync`**（[UF-002](../../01-requirements/user-flow.md#uf-002-自动锁定与解锁)）：密码正确 → `FailedUnlockAttempts` 重置为 0；密码错误 → 计数 +1，达 `AuthOptions.MaxFailedUnlockAttempts` 则同时置 `IsLocked=true`，两种错误路径均通过同一次 `ExecuteInTransactionAsync` 写回，避免竞态窗口。
- **`UnlockAccountAsync`**（[UF-012](../../01-requirements/user-flow.md#uf-012-admin-解封账号) + [AC-067](../../01-requirements/acceptance-criteria.md)）：`IsLocked=false` + `FailedUnlockAttempts=0` 同一事务写回；`actorUserId` 的 `IsSuper` 校验**不**在本方法内重复（[§1.2](#12-范围) 已声明为 WebApi 授权中间件职责）。
- **`ListAccountsAsync`**：`isLocked=null` 时以 `Pagination(1, 1000)`（覆盖 [requirements.md §6](../../01-requirements/requirements.md) ~100 用户软目标上限的 10 倍冗余）调用 `IUserRepository.ListUsers` 后映射；`isLocked` 非 null 时改调 `FindUsersByLockedStatus`。

## 5. 数据库设计增量（追加至 [database-design.md](../database-design.md)）

> 以下内容需追加到 `database-design.md`（跨模块文件，仅追加本模块章节，详见随本次提交的文件改动）。

- 表名 `users`（[database-design.md 表清单](../database-design.md) 已占位，本次锁定 `锁定 HD` = `HD-014`）
- 列：`Id`（PK，`Guid` v7）/ `Username`（`string`，唯一索引，长度上限 100——作者判断，非 Owner 拍板，理由：需求未指定，100 字符对用户名场景宽裕且与 [REQ-003](../../01-requirements/requirements.md) Agent 名称 50 字上限量级一致，无安全含义）/ `PasswordHash`（`string`，无业务长度上限，需容纳未来任意算法输出）/ `IsSuper`（`bool`，默认 `false`）/ `IsLocked`（`bool`，默认 `false`）/ `FailedUnlockAttempts`（`int`，默认 `0`）/ `LastLoginTime`（`DateTimeOffset?`，可空）/ `CreatedTime`+`UpdatedTime`（`IHasTimestamps`）/ `RowVersion`（`IHasRowVersion`）
- 索引：`Username` 唯一索引（登录查找 + 唯一性约束双重用途）；`IsLocked` 非唯一索引（UI-009 账号 tab 默认过滤已锁账号，[UF-012](../../01-requirements/user-flow.md#uf-012-admin-解封账号) 步骤 2）
- Entity（`UserEntity`）+ Mapping（`UserMappingExtensions`）+ `EfCoreUserRepository` 实现物理位置：`providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [ADR-022](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁定物理位置）——**本 HD 不改写已 reviewed 的 HD-009**，此处仅记录契约缺口，留待后续对 HD-009 发起 errata（同 [§1.2](#12-范围) 排除项）

## 6. 待补 / 待评审

### 6.1 已解决问题（原"需要 Owner 确认"，2026-07-06 已由默认 Agent 通过 vscode_askQuestions 真实确认）

1. **密码哈希算法选型**——候选：
   - **A. PBKDF2-HMACSHA256**（[`Rfc2898DeriveBytes.Pbkdf2`](https://learn.microsoft.com/dotnet/api/system.security.cryptography.rfc2898derivebytes.pbkdf2) 静态方法，.NET 6+ BCL 内置，零第三方包）——工程约束优势：满足 [AGENTS.md §3.2](../../../AGENTS.md) "`Inkwell.Core.*` 只依赖 `Inkwell.Abstractions` + BCL"的硬约束，无需新增 ADR / provider csproj；OWASP Password Storage Cheat Sheet 认可（建议 ≥ 600,000 次迭代 + HMAC-SHA256）
   - **B. BCrypt**（如 [`BCrypt.Net-Next`](https://www.nuget.org/packages/BCrypt.Net-Next) NuGet 包）——业界常用，但属于第三方包，`Inkwell.Core.Auth` 依当前依赖规则**不能**直接引用，需新增 ADR + 独立 `providers/*` csproj 才能落地（拓扑变更，超出本 HD 授权范围）
   - **C. Argon2id**（如 [`Konscious.Security.Cryptography.Argon2`](https://www.nuget.org/packages/Konscious.Security.Cryptography.Argon2) NuGet 包）——[OWASP 首选算法](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)，但同样是第三方包，约束同 B
   - **Owner 于 2026-07-06 真实确认选 A（PBKDF2）**：不引入第三方包，符合业务命名空间零外部依赖原则；迭代次数建议 ≥ 600,000（OWASP 当前建议值）、输出 32 字节、盐长度 16 字节，具体常量值由 `PasswordHasher.cs`（§3.x）落定
2. **失败登录尝试是否需要额外的速率限制阈值 / 时间窗口数字**（超出 [§1.3 Q3](#13-关键决策摘要) 已拍板的"解锁失败阈值"范围）——[UF-001](../../01-requirements/user-flow.md) 提到"速率超限：登录过于频繁，请稍后重试"，但具体阈值（次数 / 时间窗口）文档未给出，且已声明属于 `Inkwell.WebApi` ASP.NET Core Rate Limiting middleware 职责（[§1.2](#12-范围)），非本 HD 决策范围，仍留待后续 `Inkwell.WebApi` HD 起草时 Owner 拍板该数字（本条尚未解决）

### 6.2 待后续 HD 处理的契约缺口

- `IUserRepository` 的 EFCore 实现（Entity / Mapping / Repository 实现）需通过 errata 追加到已 reviewed 的 [HD-009](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)
- `InkwellSeeder` 的默认管理员账号 seed 逻辑同样需要 errata 追加到 HD-009（详 [§1.2](#12-范围)）
- `tests/core/Inkwell.Providers.Contract/Persistence/Auth/` 跨 Provider 契约用例待 [HD-013](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md) 起草时纳入
- AGENTS.md §3.3 "OQ-003 closed §A" 引用编号疑似历史漂移（实际证据在 OQ-005），详顶部 callout；本 HD 不修改 AGENTS.md，仅记录供后续治理修正参考

## 7. 决策记录

> 本节记录 2026-07-06 默认 Agent 通过 `vscode_askQuestions` 向 Owner 真实确认的交互结果（详见文件顶部"治理修正说明"）。

### Q1（Token/会话机制）

- **候选**：A. Session Token + Cache（Redis TTL=24h）/ B. Session Token + DB 表 / C. 无状态 JWT
- **回答**：A
- **交互时间**：2026-07-06（默认 Agent 补做确认）
- **回写**：[§1.3 Q1](#13-关键决策摘要) / [§3.11](#311-inkwellcoreauthsessioncacheentrycs)

### Q2（失败登录审计范围）

- **候选**：A. 仅记录成功登录 / 登出 / B. 成功 + 失败登录尝试都记入审计日志
- **回答**：A
- **交互时间**：2026-07-06（默认 Agent 补做确认）
- **回写**：[§1.3 Q2](#13-关键决策摘要)

### Q3（解锁失败锁定阈值）

- **候选**：A. 5 次 / B. 3 次 / C. 10 次
- **回答**：A
- **交互时间**：2026-07-06（默认 Agent 补做确认）
- **回写**：[§1.3 Q3](#13-关键决策摘要) / [§3.4](#34-authauthoptionscs)

### Q5（密码哈希算法）

- **候选**：A. PBKDF2（BCL 内置） / B. BCrypt（第三方包） / C. Argon2id（第三方包）
- **回答**：A
- **交互时间**：2026-07-06（默认 Agent 补做确认）
- **回写**：[§1.3 Q5](#13-关键决策摘要) / [§6.1](#61-已解决问题原需要-owner-确认2026-07-06-已由默认-agent-通过-vscode_askquestions-真实确认)

以上五条均为 2026-07-06 默认 Agent 通过 `vscode_askQuestions` 向 Owner 真实确认（详见文件顶部"治理修正说明"）。
