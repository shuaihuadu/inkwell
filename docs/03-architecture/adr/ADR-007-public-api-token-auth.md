---
id: ADR-007-public-api-token-auth
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
  - ADR-002
downstream: []
---

# ADR-007 公开 API 鉴权：单 Token

## 上下文

[REQ-013 公开 API](../../01-requirements/requirements.md) + [UI-010 / UF-010](../../01-requirements/ui-spec.md) 要求每个 Agent 可以"对外开放 HTTP 端点"，由 v1 用户使用浏览器以外的程序（脚本 / 工作流 / 第三方系统）调用。

[OQ-004 closed §A](../../01-requirements/open-questions.md) 已锁"v1 单 Token 鉴权 + 显式 rate limit"。[EX-005](../../01-requirements/requirements.md) 要求公开 API 异常 / 鉴权失败 / 触发限流都返回标准错误码。

[Q-A4-followup](../open-questions-arch.md) 默认值 A 锁定缓存 / 限流由 ASP.NET Core 内置组件实现（不引入 Redis）。

## 决策

**公开 API 鉴权采用：单 Token + Bearer scheme + ASP.NET Core 自定义 AuthenticationHandler；rate limit 使用 ASP.NET Core 内置 [Rate Limiting](https://learn.microsoft.com/aspnet/core/performance/rate-limit) middleware。**

- Token 签发：管理员在 Agent 详情页一键生成 ≥ 32 字节随机 Token（GUID + 加盐 SHA-256），保存的是 Token 哈希；明文只在生成时显示一次。
- Token 撤销：管理员可在 UI 中失效任何 Token；失效后请求立即拒绝。
- Token 范围：单 Token 关联单 Agent + 单租户，无超管 Token；不支持 RBAC（v1 范围裁剪）。
- 限流：每 Token 默认 60 req/min（可配置上限）；触发限流返回 HTTP 429 + `Retry-After` header。

## 备选项

### 备选 A（OQ-004 §B 多 Token + 角色）：每个 Token 绑定 RBAC 角色

- **放弃理由**：(1) 与 [OQ-006 closed §A](../../01-requirements/open-questions.md) v1 范围风险冲突，RBAC 模型设计 + UI 管理面工作量大；(2) v1 用户场景（Agent 内部接入）不需要细分角色 — 一个 Agent 一个 Token 的语义足够清晰；(3) 真要细分角色，等 v2 在引入用户系统时一起设计。

### 备选 B（OQ-004 §C Token + TTL）：所有 Token 必须设置过期时间

- **放弃理由**：(1) v1 用户场景中 Token 多用在"内部系统对接"，强制 TTL 增加运维负担（业主需要定期轮换）；(2) 不强制 TTL 不影响安全 — 管理员可以手动撤销；(3) 这是 v2 可加强的能力，不是 v1 必须。

### 备选 C：使用 OAuth2 / OIDC 标准

- **放弃理由**：(1) v1 没有用户系统（[REQ-001](../../01-requirements/requirements.md) 未定义认证 / 注册流程）；(2) OAuth2 引入授权服务器 → IdP 选型 + 部署；(3) 与"对外开放 HTTP 端点给程序调用"的简单场景不匹配。

### 备选 D：使用 mTLS 双向证书

- **放弃理由**：(1) 客户端证书分发 + 轮换是企业 IT 痛点；(2) 与 v1 易用性目标冲突。

## 后果

### 正面

- 实现路径短：ASP.NET Core 自定义 AuthenticationHandler ≈ 100 行代码 + 内置 RateLimiter middleware。
- Token 哈希存储 → 数据库泄露不直接暴露 Token 明文。
- 单 Token / 单 Agent 语义明确，符合"接入第三方"主场景。
- 触发 [REQ-013 公开 API](../../01-requirements/requirements.md) + [EX-005 鉴权失败 / 限流](../../01-requirements/requirements.md) 全部场景。

### 负面

- 没有 RBAC：Token 持有者可以调用 Agent 全部能力；通过"一个 Agent 一个 Token + 谁分发谁负责"的运维约束缓解。
- 没有 TTL：Token 一旦泄露需要管理员手动撤销；通过 UI 一键撤销缓解。
- v2 升级到 RBAC / TTL 会涉及 schema 迁移；现在的 token table 需要预留 `role` / `expires_at` 字段（v1 NULL）。

### 中性

- 限流粒度仅到 Token 级别；细粒度（Agent + Endpoint + IP 复合）留 v2。
- Token 不支持 scope（什么端点可调）；v1 只有"全部 Agent 能力"。

## 状态

- **状态**：accepted
- **首次发布**：2026-05-08
- **关联**：supersedes 无；上游 [ADR-002](./ADR-002-backend-runtime-dotnet10-aspnetcore.md) / [OQ-004](../../01-requirements/open-questions.md)
- **置信度**：high（OQ-004 closed；ASP.NET Core 标准 middleware 经过广泛验证）
