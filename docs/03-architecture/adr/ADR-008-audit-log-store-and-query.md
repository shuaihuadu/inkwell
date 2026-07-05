---
id: ADR-008-audit-log-store-and-query
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
  - ADR-004
downstream: []
---

# ADR-008 审计日志：主 DB 表 + UI 检索（v1 不导出）

## 上下文

[NFR-004 审计日志](../../01-requirements/requirements.md) 要求记录："谁 / 何时 / 在哪个 Agent / 做了什么 / 结果"。[OQ-020 closed §B](../../01-requirements/open-questions.md) 已锁"v1 仅支持 UI 检索 + 时间窗口过滤，不提供导出能力"。

审计日志的写入来源：

- [REQ-013 公开 API](../../01-requirements/requirements.md) 调用（详见 [ADR-007](./ADR-007-public-api-token-auth.md)）
- 用户主动操作（创建 / 编辑 / 删除 Agent / Skill / 知识库 / 编排）
- 系统操作（Agent 版本快照创建、Skill 加载失败、Run 终止）
- [EX-005 异常 / 鉴权失败 / 触发限流](../../01-requirements/requirements.md)

## 决策

**审计日志存储在与业务数据同一个关系数据库（[ADR-004](./ADR-004-data-store-provider-switchable-ef-core.md) 配置的 Provider）的独立表 `inkwell_audit_logs`，按时间分区；UI 仅在 [UI-009 §9.4](../../01-requirements/ui-spec.md) 提供检索 + 时间窗口过滤；v1 不实现导出。**

- 表结构关键字段：`id` (GUID) / `event_type` / `actor_type` (`user` / `token` / `system`) / `actor_id` / `agent_id` (nullable) / `target_kind` / `target_id` / `payload` (JSON) / `result_code` / `error_code` (nullable) / `request_id` (trace id) / `created_at` (UTC, 索引)。
- 写入：通过 `IAuditLogger` 接口在 ASP.NET Core middleware + 业务 use case 中显式调用。
- 检索：UI 表单接受 actor / event_type / agent / 时间窗（v1 默认最大 7 天）四组过滤；分页 50 条 / 页。
- 保留期：v1 默认保留 90 天，通过后台清理任务（[Microsoft.Extensions.Hosting BackgroundService](https://learn.microsoft.com/dotnet/core/extensions/workers)）按 `created_at` 删除。
- 写入失败处理：使用本地内存队列 + 重试 3 次；3 次失败后写入磁盘 fallback 文件，并触发告警。
- 时间分区：v1 不分表，使用 `created_at` 索引 + 90 天保留期足矣；v2 数据量大可分月。

> **2026-07-05 errata**（H3 [HD-007 IAuditLogger Port](../../04-detailed-design/Inkwell.Abstractions/HD-007-Inkwell.Abstractions-audit-logger-port.md) 起草期发现 H1/H2 分歧）：上方"保留期：v1 默认保留 90 天"与 [requirements.md §8.3 数据生命周期](../../01-requirements/requirements.md) "审计日志：至少保留 6 个月（v1 默认值，可配置）"（该条经 §8.1 "审计数据：见 NFR-004" 关联到 [NFR-004 审计日志](../../01-requirements/requirements.md)）字面冲突。Owner picker 拍板：对齐 H1 requirements.md 的硬性合规要求，**保留期由 90 天修订为 180 天（约 6 个月）**。本 ADR 主决策正文与下方"状态"区块保留原文不改，实际生效值以本 errata 为准：后台清理任务（`BackgroundService`）的保留期配置默认值须为 **180 天**；[`docs/03-architecture/tech-selection.md` §8](./../tech-selection.md) 同步加 errata。

## 备选项

### 备选 A（OQ-020 §A 写入主 DB + 异步索引到 ELK）：双写到 Elasticsearch / OpenSearch

- **放弃理由**：(1) 与 [Q-A8](../open-questions-arch.md) "不引入 ELK" 一致 — 我们用 [Loki](https://grafana.com/oss/loki/)（[ADR-013](./ADR-013-observability-otel-self-hosted-grafana.md)）做 telemetry 日志，不再单独引入 ES；(2) ELK 运维成本高（mapping / index lifecycle / 节点配置）；(3) v1 数据规模（< 100 万条 / 月）DB 直查就够。

### 备选 B（OQ-020 §C 双 sink）：写主 DB 表 + 异步导出到 [Azure Blob](../open-questions-arch.md)

- **放弃理由**：(1) v1 没有合规审计要求（[NFR-006 安全](../../01-requirements/requirements.md) 未指定数据保留法规）；(2) Azure Blob 二级存储引入查询能力差（要拉回来才能搜）；(3) v2 加导出能力时再决定 sink 形态。

### 备选 C：审计日志走 [OpenTelemetry Logs](https://opentelemetry.io/docs/specs/otel/logs/) 直进 [Loki](https://grafana.com/oss/loki/)

- **放弃理由**：(1) Loki 适合 unstructured / semi-structured 应用日志，不适合做"按字段过滤 + 长时间保留 + UI 检索"的合规级审计 — 查询语法学习曲线高，列式索引能力弱；(2) 业务数据库的事务一致性（审计写入与业务操作同事务）在 Loki 上无法保证 — 业务成功但审计失败的场景会丢；(3) 审计需要"长期可证"，Loki 默认保留 7-30 天，与 90 天目标不一致。

## 后果

### 正面

- 与 [ADR-004 EF Core](./ADR-004-data-store-provider-switchable-ef-core.md) 同 Provider，不引入新依赖。
- 写入路径与业务事务可同步（确保业务成功必有审计）；通过 [IAuditLogger](../../01-requirements/repo-impact-map.md) 抽象保留扩展点。
- UI 检索使用 EF Core LINQ + 索引扫描，性能可控（≤ 100 万条数据查询 < 200 ms）。
- 实现路径短：1 个表 + 1 个 middleware + 1 个 UI 页面。

### 负面

- 审计表与业务表共库，写入热点会传导到业务库 IO；通过专用索引 + 异步补偿队列缓解。
- v1 不支持导出 → 客户合规需求会推动 v2 引入 §B 双 sink；H3 详细设计预留 IAuditLogger 接口扩展点。
- 90 天保留期 + 单表无分区 → 数据量增长后查询变慢；保留 90 天周期 + 后台清理 + 索引优化是最简单可行方案。

### 中性

- v1 的"按 actor / event / agent / 时间窗"四组过滤覆盖 80% 检索场景；其他场景（按 IP / payload 内容子串）走 v2。
- 审计日志的 PII 字段（IP / Token Hash）不脱敏 → 由 [NFR-006 安全](../../01-requirements/requirements.md) 通过权限控制访问 UI 实现。

## 状态

- **状态**：accepted
- **首次发布**：2026-05-08
- **关联**：supersedes 无；上游 [ADR-002](./ADR-002-backend-runtime-dotnet10-aspnetcore.md) / [ADR-004](./ADR-004-data-store-provider-switchable-ef-core.md) / [OQ-020](../../01-requirements/open-questions.md)
- **置信度**：high（OQ-020 closed；规模假设保守）
