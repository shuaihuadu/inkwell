---
id: ADR-004
title: 平台登录采用 dev mock + 生产 OIDC 双模式
stage: H2
status: accepted
authors:
  - name: H2-ArchitectAdvisor
    role: agent
date: 2026-05-07
upstream:
  - architecture-custom-agent
  - tech-selection-custom-agent
supersedes: []
superseded-by: []
---

# ADR-004：平台登录采用 dev mock + 生产 OIDC 双模式

## 上下文

[ND-009 + R7](../../01-requirements/requirements.md) 已锁定"不内置登录，依赖平台登录子系统"。

[GAP-003](../../01-requirements/repo-impact-map.md#3-缺失发现43) 平台登录子系统的具体协议 / IdP / token 主键 claim 名等本期未定。

H2 反问 Q5 用户答：
> 目前先模拟登录，后续实现

## 决策

- 引入 `IPlatformAuthenticator` 接口 + 双 handler 实现
- **dev 模式**：`Authentication:Mode=DevMock` + `DevModeAuthenticationHandler`，读 cookie / header `X-Dev-User-Id` 自动生成 `ClaimsPrincipal`
- **生产模式**：`Authentication:Mode=Oidc` + `Microsoft.AspNetCore.Authentication.OpenIdConnect`，具体 IdP 待 [OQ-A-001](../open-questions-arch.md) 关闭
- **强制隔离**：
  1. dev mock handler 用 `#if DEBUG` 排除生产二进制
  2. `Authentication:Mode=DevMock` 检测在生产构建启动时主动 throw `InvalidOperationException`
  3. CI release build smoke test 验证 `Authentication:Mode=DevMock` 启动失败
  4. K8s Helm chart 默认 `Authentication.Mode=Oidc`，禁止覆盖

## 备选项

| 备选 | 放弃理由 |
| --- | --- |
| 本期就接入某个 IdP | 平台 IdP 未定（GAP-003），强行落地会被 H3 / H5 反复推翻 |
| 本期完全不做登录（裸 API） | [PB-001 私有 + 跨用户隔离](../../01-requirements/requirements.md) 测试无法验证 |
| 本期自建 ASP.NET Core Identity | 与 [ND-009](../../01-requirements/requirements.md) 冲突 |
| 仅 dev mock，不预留生产 handler | 生产部署时仍需重新设计鉴权管道，等于把 GAP-003 又压回 |

## 后果

### 正面

- ASP.NET Core 鉴权管道支持 multiple scheme，dev / prod 切换零代码改动
- dev mock 让本期 H4 测试 / H5 编码 / H6 演示完整可跑
- `IPlatformAuthenticator` 抽象让生产 handler 接入零侵入应用层

### 负面

- dev mock 错误带到生产 = 无鉴权 = 全部 REQ 失守（[RISK-004](../risk-analysis.md)）→ 已通过 4 重隔离缓解
- OIDC IdP 选定（[GAP-003 / OQ-A-001](../open-questions-arch.md)）前生产无法上线 — 这是延后决策的代价

### 中性

- 双 handler 增加测试矩阵（dev mock + OIDC mock 两份），但每份成本低

## 状态

`accepted` · 2026-05-07
