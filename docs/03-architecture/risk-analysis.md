---
id: risk-analysis-inkwell-agent-platform
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [ Inkwell ]
created: 2026-05-08
updated: 2026-05-10
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
downstream:
  - architecture-inkwell-agent-platform
---

<!-- 2026-05-10 ADR-017 / ADR-018 / ADR-019 / ADR-020 / ADR-021 引入后：RISK-001 缓解方案第 3 条重写（csproj 硬边界 → lint + 接口收敛软边界）；RISK-014 随 [OQ-A008 closed §B](./open-questions-arch.md) 从「占位」激活为「已激活」（v1 同期交付 RedisStreamQueueProvider，残余风险 = observability 指标补齐 / Redis 实例多负载）；RISK-015 随 [ADR-019 进程拓扑](./adr/ADR-019-process-topology-webapi-worker-split.md) 新增（WebApi / Worker 双进程版本漂移 + OTel 双 source），2026-07-09 随 [ADR-024 Migrator](./adr/ADR-024-database-migration-seed-standalone-job.md) 扩展为三产物（WebApi / Worker / Migrator）同镜像 tag 同步；RISK-016 随 [ADR-020 向量存储](./adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 新增（InMemoryVectorStore 与 Qdrant 语义偏移 / M.E.VectorData 上游 NuGet breaking change 传导）；RISK-017 随 [ADR-021 EFCore Persistence 共享层](./adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 新增（DataSeed 幂等性 / SqlServer × Postgres 最小公倍数 schema 漂移 / EFCore family 其他 family 的依赖例外蔓延）。
     2026-05-11 ADR-022 引入后：RISK-018 新增占位（手写 mapper 模式下新 mixin 扫漏机制未激活；v1 不阻塞，v2 可选激活 `MissingMixinFieldAnalyzer`）。
     人工评审动作：确认 status 仍保持 reviewed（incremental update）或翻为 draft 重新过评审。 -->

<!-- markdownlint-disable MD060 -->
<!-- 中文 + 英文混排长表格在 markdownlint 列宽计算下字面对齐 ≠ 视觉对齐（详 /memories/markdown-lint.md），表格仍按 docs-style §3 视觉对齐维护，机械 MD060 不予执行。 -->

# Inkwell Agent 平台 · 风险分析

> 本文档对应 [stages.md §5.4](../../../.he/docs/stages.md) "主要技术风险" 与 [agents/architect-advisor/AGENT.md §4.3](../../../.he/agents/architect-advisor/AGENT.md) 的字段要求：风险编号 / 类别 / 触发条件 / 影响范围 / 缓解方案 / 残余风险。每条 RISK-NNN 至少要有一条可执行的缓解动作。

## 0. 风险摘要

| 编号                                                                           | 类别            | 主题                                                     | 严重度 | 关联                                                                                                                                                                                                                                                                   |
| ------------------------------------------------------------------------------ | --------------- | -------------------------------------------------------- | ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [RISK-001](#risk-001-microsoft-agent-framework-成熟度)                         | 依赖成熟度      | MAF 仍在演进                                             | 中     | [ADR-003](./adr/ADR-003-agent-engine-microsoft-agent-framework.md) · [ADR-017](./adr/ADR-017-backend-module-topology-ports-and-adapters.md)                                                                                                                            |
| [RISK-002](#risk-002-ipersistenceprovider-切换抽象漏出)                        | 数据层          | IPersistenceProvider 抽象漏出                            | 中     | [ADR-004](./adr/ADR-004-data-store-provider-switchable-ef-core.md)                                                                                                                                                                                                     |
| [RISK-003](#risk-003-nfr-003-字面与-oq-017-文字差异-w-003)                     | 文档一致性      | NFR-003 字面与 OQ-017 差异 W-003                         | 低     | [NFR-003](../01-requirements/requirements.md) / [OQ-017](../01-requirements/open-questions.md)                                                                                                                                                                         |
| [RISK-004](#risk-004-aks-单-region-可用性)                                     | 可用性          | AKS 单 region 不具备跨区高可用                           | 中     | [ADR-005](./adr/ADR-005-deployment-docker-compose-aks.md)                                                                                                                                                                                                              |
| [RISK-005](#risk-005-azure-speech-依赖--成本)                                  | 依赖成本        | Azure Speech 调用量与可用性                              | 低     | [ADR-009](./adr/ADR-009-multimodal-azure-speech.md)                                                                                                                                                                                                                    |
| [RISK-006](#risk-006-自托管-grafana-栈数据保留--运维)                          | 运维            | Grafana 栈数据保留 + 备份                                | 中     | [ADR-013](./adr/ADR-013-observability-otel-self-hosted-grafana.md)                                                                                                                                                                                                     |
| [RISK-007](#risk-007-主进程长-sse-跨锁屏可靠性)                                | 可靠性          | 主进程长 SSE 跨锁屏休眠重连                              | 中     | [ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md) / [ADR-012](./adr/ADR-012-client-server-protocol-rest-agui.md)                                                                                                                                       |
| [RISK-008](#risk-008-v1-范围裁剪压力)                                          | 进度 / 范围     | OQ-006 范围裁剪是否兜得住                                | 中     | [OQ-006](../01-requirements/open-questions.md)                                                                                                                                                                                                                         |
| [RISK-009](#risk-009-skill-加载错误传播到对话)                                 | 体验 / 错误     | Skill 加载失败影响对话                                   | 低     | [ADR-010](./adr/ADR-010-skill-loading-static-only-v1.md) / [EX-008](../01-requirements/requirements.md)                                                                                                                                                                |
| [RISK-010](#risk-010-v1-不引入-i18n-的-v2-重做成本)                            | 技术债          | v2 引入 i18n 重构成本                                    | 低     | [ADR-014](./adr/ADR-014-i18n-out-of-scope-v1.md)                                                                                                                                                                                                                       |
| [RISK-011](#risk-011-文件存储三-provider-contract-漏出)                        | 数据层 / 测试   | 文件存储三 Provider contract 测试漏出                    | 中     | [ADR-015](./adr/ADR-015-object-storage-provider-switchable.md)                                                                                                                                                                                                         |
| [RISK-012](#risk-012-redis-单点与缓存-invalidation-一致性)                     | 数据层 / 一致性 | Redis 单点 + 多副本 invalidation                         | 中     | [ADR-016](./adr/ADR-016-cache-provider-redis.md)                                                                                                                                                                                                                       |
| [RISK-013](#risk-013-v1-未引入-key-vault-的凭据轮换与隔离弱化)                 | 安全 / 合规     | K8s Secret + .env 弱于 Key Vault                         | 中     | [ADR-005](./adr/ADR-005-deployment-docker-compose-aks.md) / [OQ-A006 closed §B](./open-questions-arch.md)                                                                                                                                                              |
| [RISK-014](#risk-014-redisstreamqueueprovider-运维代价)                        | 运维 / 一致性   | observability 补齐 + Redis 多负载                        | 中     | [ADR-018](./adr/ADR-018-queue-abstraction-channels-default.md) / [OQ-A008 closed §B](./open-questions-arch.md)                                                                                                                                                         |
| [RISK-015](#risk-015-webapi--worker--migrator-三产物版本漂移与-otel-双-source) | 部署 / 可观测性 | WebApi / Worker / Migrator 同 image tag + OTel 双 source | 中     | [ADR-019](./adr/ADR-019-process-topology-webapi-worker-split.md) / [ADR-005](./adr/ADR-005-deployment-docker-compose-aks.md) / [ADR-013](./adr/ADR-013-observability-otel-self-hosted-grafana.md) / [ADR-024](./adr/ADR-024-database-migration-seed-standalone-job.md) |
<!-- markdownlint-disable-next-line MD051 -->
| [RISK-016](#risk-016-inmemoryvectorstore-与-qdrant-语义偏移-microsoftextensionsvectordata-上游变化)        | 环境差异 / 上游依赖     | vector contract 用例包 + M.E.VectorData NuGet 锁定 + Qdrant only feature 标注 | 中     | [ADR-020](./adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) / [ADR-004](./adr/ADR-004-data-store-provider-switchable-ef-core.md)            |
| [RISK-017](#risk-017-efcore-family-幂等-dataseed--schema-最小公倍数--family-例外蔓延)        | 数据层 / 依赖拓扑     | DataSeed 幂等性 + EFCore-Conditional schema + family 例外锁定 | 中     | [ADR-021](./adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) / [ADR-004](./adr/ADR-004-data-store-provider-switchable-ef-core.md) / [ADR-017](./adr/ADR-017-backend-module-topology-ports-and-adapters.md)            |
| [RISK-018](#risk-018-mixin-体系演进扫漏手写-mapper-模式下)        | 技术债 / 静态检查     | `MissingMixinFieldAnalyzer` 未激活占位 | 低     | [ADR-022](./adr/ADR-022-entity-domain-mapper-selection.md) / HD-009            |
| [RISK-019](#risk-019-litellm-关键依赖与模型目录路由漂移)        | 可用性 / 配置一致性   | 网关单点 + Model Registry 与路由双配置漂移 | 中     | [ADR-026](./adr/ADR-026-model-gateway-litellm.md) / HD-019                    |

## RISK-001 Microsoft Agent Framework 成熟度

- **类别**：依赖成熟度
- **触发条件**：MAF 在 v1 开发周期内发布 breaking change（接口签名变更 / NuGet 包重组）；或 MAF 中关键能力（Workflows / DurableTask / AGUI）在生产场景出现稳定性问题。
- **影响范围**：[ADR-003](./adr/ADR-003-agent-engine-microsoft-agent-framework.md) 引入的全部业务模块（[ADR-017](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) 下 `Inkwell.Core.*` 业务命名空间）。
- **缓解方案（可执行）**：
  1. 锁定具体 NuGet 版本（pin 到 patch level）在 `Directory.Packages.props` — 不使用 `*` 通配符。
  2. 升级 MAF 之前必须跑全量 H4 用例 + 集成测试，通过后再合并。
  3. **接口收敛 + lint 软边界**（[ADR-017](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) 后代替原“独立 csproj 硬边界”）：对外只暴露 `IAgentRuntime` 接口（在 `Inkwell.Abstractions`）；MAF API 调用集中在 `Inkwell.Core.AgentRuntime` 命名空间；CI 静态分析质保其他业务命名空间不 `using Microsoft.Agents.AI.*`（[Roslyn analyzer / EditorConfig restrict_namespace](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/) 或 `BannedSymbols.txt`）。
  4. 监控 [microsoft/agent-framework releases](https://github.com/microsoft/agent-framework/releases) 与 ADR-003 配套的“已知 incompat 清单”在 H6 release-notes 中定期回写。
- **残余风险**（1）MAF 在 v1 周期内出现严重设计变更（如把 `IChatClient` 拆分），将不得不进行较大规模重构；（2）**软边界漂洗风险**：ADR-017 以 lint 代替 csproj 隔离后，若 `Inkwell.Core` 内部规模膨胀、代码评审纪律下降，lint 规则可能被“临时纵容”突破，导致 MAF 依赖渗透到业务命名空间。需在 [tech-debt-tracker](../../../.he/docs/tech-debt-gc.md) 登记定期扫描任务。

## RISK-002 IPersistenceProvider 切换抽象漏出

- **类别**：数据层
- **触发条件**：业务查询需要 Provider-specific 特性（如 PostgreSQL `JSONB` 操作符 / SQL Server `OUTPUT` 子句 / `FILTER WHERE`），跨 Provider 实现性能差距数量级。
- **影响范围**：[ADR-004 IPersistenceProvider](./adr/ADR-004-data-store-provider-switchable-ef-core.md) 涉及的所有 Repository（`Inkwell.Identity` / `Inkwell.Agents` / `Inkwell.Conversations` 等 6 个模块）。
- **缓解方案（可执行）**：
  1. CI 加 [matrix job](https://docs.github.com/actions/using-jobs/using-a-matrix-for-your-jobs)：每个 PR 跑两套 Provider（SqlServer / Postgres，Testcontainers 真实实例）集成测试。
  2. Repository 层引入"Provider-specific Strategy"模式：`IUserRepository` 接口 + `PostgresUserRepository` / `SqlServerUserRepository` 两实现，应用代码只引用接口。
  3. H3 详细设计阶段绘制"查询性能矩阵"：列出每个关键查询在两种 Provider 下的预期 P95，作为验收门禁。
  4. 在 [tech-debt-tracker](../../../.he/docs/tech-debt-gc.md) 登记本风险，每个 sprint review。
- **残余风险**：v1 范围内可能仍有未发现的 Provider-specific 边界，需要 H4 + H5 持续暴露。

## RISK-003 NFR-003 字面与 OQ-017 文字差异 W-003

- **类别**：文档一致性
- **触发条件**：[NFR-003 字面](../01-requirements/requirements.md) 仅说"客户端自动锁定"，未给"5 分钟"具体值；具体值在 [OQ-017](../01-requirements/open-questions.md) 决议。如果 H4 测试只看 NFR 不看 OQ，可能漏掉 5 min 这一具体阈值。
- **影响范围**：[ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md) 实现 + H4 用例。
- **缓解方案（可执行）**：
  1. H1↔H4 追溯矩阵中显式连线：`NFR-003 ← OQ-017 closed §A: 5 min` → `TC-XXX`。
  2. [requirements.md NFR-003](../01-requirements/requirements.md) 在下次 review 时把"5 分钟"作为具体值写进 NFR 主表（落实 W-003 闭环）。
  3. H3 详细设计的"客户端自动锁定"章节直接引用 OQ-017 而非 NFR-003 字面。
- **残余风险**：文档同步存在滞后；通过 [hx-doc-gardener](../../../.he/docs/tech-debt-gc.md) 周期性扫描发现。

## RISK-004 AKS 单 region 可用性

- **类别**：可用性
- **触发条件**：[ADR-005](./adr/ADR-005-deployment-docker-compose-aks.md) v1 仅单 region；region 级故障 / 维护窗口会导致全量服务中断。
- **影响范围**：所有生产服务（API + 数据 + 向量 + 可观测性）。
- **缓解方案（可执行）**：
  1. v1 把"单 region 可用性"显式写进 [架构文档可用性章节](./architecture.md)，给出 SLA = 99% 而非 99.9%。
  2. PostgreSQL Backup → [文件存储 Provider](./adr/ADR-015-object-storage-provider-switchable.md)（prod 常为 AzureBlob 或 MinIO）每日全量 + 30 min WAL；备份跨 region 存储。
  3. [Helm Chart values](./adr/ADR-005-deployment-docker-compose-aks.md) 预留 region 切换的快速恢复 runbook。
  4. v2 引入跨 region active-passive 列入 backlog。
- **残余风险**：v1 SLA 仅 99%；客户合规需求若涉及 99.9% 需协商 v2 排期。

## RISK-005 Azure Speech 依赖 / 成本

- **类别**：依赖成本
- **触发条件**：Azure Speech 服务故障 / 限流 / 月度调用量超出预算。
- **影响范围**：[REQ-016 多模态](../01-requirements/requirements.md) 语音输入；[EX-004 多模态降级](../01-requirements/requirements.md)。
- **缓解方案（可执行）**：
  1. 客户端 + 后端实现 [EX-004 §3 降级路径](../01-requirements/requirements.md)：Speech 失败 → toast + 文本输入兜底。
  2. 后端通过 [OpenTelemetry](./adr/ADR-013-observability-otel-self-hosted-grafana.md) 记录每次 Azure Speech 调用的"调用量 + 时长 + 错误码"指标。
  3. [Grafana](./adr/ADR-013-observability-otel-self-hosted-grafana.md) 配置月度调用量告警（达到 80% 阈值时邮件）。
  4. v2 评估 Whisper 自托管作为备路。
- **残余风险**：Azure Speech 单 region 故障下语音功能不可用；客户体验可降级但不能 100% 替代。

## RISK-006 自托管 Grafana 栈数据保留 / 运维

- **类别**：运维
- **触发条件**：Loki / Tempo / Prometheus 节点故障导致数据丢失；保留期内查询性能下降。
- **影响范围**：[REQ-014 trace 全链路](../01-requirements/requirements.md) + 业务指标观测。
- **缓解方案（可执行）**：
  1. [grafana/helm-charts](https://github.com/grafana/helm-charts) 默认 PVC + Azure Disk Premium + 多副本部署。
  2. Loki / Tempo 启用 [Object Storage backend](https://grafana.com/docs/loki/latest/operations/storage/)（写入 [文件存储 Provider](./adr/ADR-015-object-storage-provider-switchable.md)实例，prod 常为 AzureBlob 或 MinIO），不只依赖本地磁盘。
  3. Prometheus 启用 [remote_write](https://prometheus.io/docs/operating/integrations/#remote-endpoints-and-storage)（H3 详细设计选择目标，v1 可暂用本地 retention）。
  4. 运维 runbook（备份恢复 / 节点扩容 / 升级）作为 H3 任务交付。
- **残余风险**：v1 保留期 30 天 / 365 天 metric downsample 可能与某些客户合规要求冲突；可在 Helm values 调整。

## RISK-007 主进程长 SSE 跨锁屏可靠性

- **类别**：可靠性
- **触发条件**：[macOS App Nap](https://developer.apple.com/library/archive/documentation/Performance/Conceptual/power_efficiency_guidelines_osx/AppNap.html) / Windows 节能 / 企业代理 idle timeout / NAT 表老化 导致主进程长 SSE 静默失活；UI 进程解锁后误以为连接仍活。
- **影响范围**：[ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md) + [ADR-012](./adr/ADR-012-client-server-protocol-rest-agui.md)；UI-007 调试页 + [REQ-001](../01-requirements/requirements.md) 对话业务连续性。
- **缓解方案（可执行）**：
  1. Renderer 心跳定期 ping 主进程（[`ipcRenderer.invoke('runs:heartbeat')`](https://www.electronjs.org/docs/latest/api/ipc-renderer)，间隔 15s）；超过 60s 未响应才认为主进程失活。
  2. 主进程 SSE 超时 30s 主动重连；重连后使用 [`GET /api/runs/{id}/state`](./adr/ADR-012-client-server-protocol-rest-agui.md) 兑底，仅拉全量快照，不靠 cursor。
  3. [Microsoft.Agents.AI.DurableTask](../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/) 保证后端 Run 不丢，即使主进程被 OS suspend Run 仍走到终。
  4. 有 in-flight Run 时启用 [`powerSaveBlocker('prevent-app-suspension')`](https://www.electronjs.org/docs/latest/api/power-save-blocker) ，运行结束后释放；Run 列表为空时不阻止节能。
  5. H4 加性能 / 恢复性用例：macOS / Windows 锁屏 30 min × 多次 sleep/resume 后 Run 状态与后端一致。
  6. Renderer 重启（极端休眠后主进程被 kill）走 [`Inkwell.Conversations`](../01-requirements/repo-impact-map.md) 表重建快照作为最后防线。
- **残余风险**：极端节能场景下主进程仍可能被 OS suspend；仅能依赖 DurableTask + Conversation 表快照补上上下文，实时的 token 流丢失不可避免。

## RISK-008 v1 范围裁剪压力

- **类别**：进度 / 范围
- **触发条件**：[OQ-006 closed §A](../01-requirements/open-questions.md) 已签字接受 v1 范围风险；但实际开发中可能出现"看起来很小其实很大"的隐藏工作（如 Provider 切换抽象、Helm Chart values、运维 runbook）。
- **影响范围**：v1 整体交付节奏；[OQ-002](../01-requirements/open-questions.md) 性能软目标。
- **缓解方案（可执行）**：
  1. H3 详细设计阶段做一次"任务工作量估算"，与 [task-board.md](../../../.he/templates/task-board.md) 对齐。
  2. 每个 sprint 的 [phase-gate-checklist](../../../.he/templates/phase-gate-checklist.md) 复核范围。
  3. 触发"工作量预估超出 30%"时，立即与 Owner 同步是否再裁剪。
  4. v1 不允许新增 REQ；新需求一律进 v2 backlog。
- **残余风险**：v1 仍可能延期；通过透明的 task-board + phase gate 让"延期"可见而非掩盖。

## RISK-009 Skill 加载错误传播到对话

- **类别**：体验 / 错误
- **触发条件**：Skill `SKILL.md` 解析失败 / Activation 匹配错误 / Activation 后 system prompt 注入异常。
- **影响范围**：[REQ-008 Skills](../01-requirements/requirements.md) + [EX-008](../01-requirements/requirements.md)。
- **缓解方案（可执行）**：
  1. [ADR-010](./adr/ADR-010-skill-loading-static-only-v1.md) 已规定：Discovery 失败 → 不进 registry；Activation 失败 → 默认未命中。
  2. 失败事件记入 OTel span 状态（`Activity.SetStatus(Error)`），UI Skill 详情页显示错误状态。
  3. H4 用例覆盖：(a) frontmatter 缺字段；(b) markdown 语法错误；(c) Activation 匹配过载（多条 Skill 同时命中）。
- **残余风险**：v1 Skill 数量预期不大（< 50 条），错误概率与影响可控。

## RISK-010 v1 不引入 i18n 的 v2 重做成本

- **类别**：技术债
- **触发条件**：v2 引入英文 / 其他语言时，需要遍历全代码库抽 i18n key + 翻译。
- **影响范围**：[ADR-014](./adr/ADR-014-i18n-out-of-scope-v1.md) — 客户端 React 组件 + 后端错误消息 + 模型 prompt 模板。
- **缓解方案（可执行）**：
  1. 在 [tech-debt-tracker](../../../.he/docs/tech-debt-gc.md) 登记本风险，标记为"已知技术债，v2 必须处理"。
  2. v1 编码时 ESLint 规则禁止"魔术字符串"用于 UI 文案；强制走 [`tFn(label)`](./adr/ADR-014-i18n-out-of-scope-v1.md) wrapper（v1 wrapper 透传，v2 切到 i18n 框架时只改 wrapper 实现）— 这是"轻量预备"，与 ADR-014 §备选 B 区别在于不引入 i18n 框架，仅提供一个零成本 wrapper。
  3. 错误消息常量集中在 `Inkwell.Common.Errors.Resources`，v2 替换实现而不替换调用点。
- **残余风险**：第 2 条缓解需要团队纪律，纪律不到位时 v2 重构成本回升 — 但相比"完全不准备"已显著降低。

## RISK-011 文件存储三 Provider contract 漏出

- **类别**：数据层 / 测试覆盖
- **触发条件**：[ADR-015 IFileStorageProvider](./adr/ADR-015-object-storage-provider-switchable.md) 三 Provider（LocalFileSystem / AzureBlob / MinIO）在预签名 URL 有效期 / 大文件分片上传 / 列表分页与续传标记 / 元数据与 ContentType / 并发覆写 等语义上存在实现差异；三者 contract test 覆盖不全时，dev 在 LocalFileSystem 上运行正常但 prod 在 AzureBlob / MinIO 上出现运行时错误。
- **影响范围**：所有依赖 `IFileStorageProvider` 的模块：`Inkwell.Multimodal`（[ADR-009](./adr/ADR-009-multimodal-azure-speech.md) 多模态预签名 URL） / `Inkwell.KnowledgeBase`（知识库原始件 + 抽取后 Markdown）。
- **缓解方案（可执行）**：
  1. 建立公共 contract test 包 `Inkwell.FileStorage.Tests.Contract`，覆盖上述 5 个语义点，三 Provider 公用同一套用例。
  2. CI [matrix job](https://docs.github.com/actions/using-jobs/using-a-matrix-for-your-jobs) 跑三 Provider：LocalFileSystem（本地临时目录） / Azurite（本地 AzureBlob 模拟） / MinIO（本地容器）。
  3. `LocalFileSystem` 预签名 URL 为本机路由模拟，在接口文档中明确标注“仅 dev / 单测使用，不能作为 prod 预签名语义参考”。
  4. Helm post-install hook 跑 MinIO bucket 创建与探测测试，安装即验证。
  5. H3 详细设计阶段为预签名 URL TTL / 分片阈值 / 列表页大小 给出推荐默认值，三 Provider 取交集。
- **残余风险**：边缘场景（大文件分片 + 并发覆写 + 跨区复制）在 LocalFileSystem 上仅能模拟，仍可能漏出；实际项目需依赖预发布环境跑 prod Provider 全量回归。

## RISK-012 Redis 单点与缓存 invalidation 一致性

- **类别**：数据层 / 一致性
- **触发条件**：业务写 DB 后忘记 invalidate 缓存键 / Redis 节点故障导致 cache miss 击穿 / 多副本同时刷新缓存导致 [stampede](https://en.wikipedia.org/wiki/Cache_stampede) / 自建 Redis StatefulSet 单实例重启导致 rate limit 窗口丢失。
- **影响范围**：[ADR-016 ICacheProvider](./adr/ADR-016-cache-provider-redis.md) 涉及的 `Inkwell.PublicApi`（rate limit token bucket） / `Inkwell.Agents`（配置缓存） / `Inkwell.Skills`（registry 缓存） / `Inkwell.AgentRuntime`（AgentThread 短期状态）。
- **缓解方案（可执行）**：
  1. H3 详细设计为每个缓存键定义 invalidation 触发点（写表事件 → 调用 `_cache.RemoveAsync(key)`），以列表形式入 detailed-design 表格。
  2. `RedisCacheProvider` 加 stampede 防护：single-flight（同进程同键仅一个请求回源） + jitter（下游 TTL 加随机抽击）。
  3. prod Azure 场景选 [Azure Cache for Redis Standard 2-node replica 或 Premium cluster](https://learn.microsoft.com/azure/azure-cache-for-redis/cache-overview) 避免单点；自建场景 v1 明确"单节点 + 本地 PVC"，上限为内部评估 / POC 场景。
  4. 单元测试用 `InMemoryCacheProvider` + contract test 验证 invalidation 路径；集成测试用 `Testcontainers.Redis`。
  5. 关键路径必须 fall-through 到 DB（缓存 miss 不应报错而应重访 DB）。
- **残余风险**：v1 自建场景仅单 Redis 实例，重启期间 < 30s 内的 rate limit 窗口快照会丢；Azure Cache for Redis 已自带多 AZ；缓存与 DB 不可能 100% 强一致，需以 DB 为唯一事实源。

## RISK-013 v1 未引入 Key Vault 的凭据轮换与隔离弱化

- **类别**：安全 / 合规
- **触发条件**：[OQ-A006 closed §B](./open-questions-arch.md) 反转决议：v1 不引入 [Azure Key Vault](https://learn.microsoft.com/azure/key-vault/) + [CSI driver](https://learn.microsoft.com/azure/aks/csi-secrets-store-driver)，凭据走 [Kubernetes Secret](https://kubernetes.io/docs/concepts/configuration/secret/) + Compose `.env`；Secret 在 etcd 默认 base64（非加密），节点磁盘 / etcd 备份泄漏即明文暴露；凭据轮换需 Pod 重启。
- **影响范围**：Azure Speech 等外部 API 凭据、仅注入 LiteLLM 的模型厂商凭据、Redis 连接串、DB 连接串与 Public API Token 哈希盐。
- **缓解方案（可执行）**：
  1. AKS 启用 [etcd encryption-at-rest provider config](https://kubernetes.io/docs/tasks/administer-cluster/encrypt-data/)，避免 Secret 在持久化层明文。
  2. RBAC 收敛：仅 deployer / runtime ServiceAccount 可读 Secret；禁止 cluster-admin 外的人工访问。
  3. `.env` 进 [.gitignore](https://git-scm.com/docs/gitignore) + dev 文档明确 .env 不入仓；Aspire secret parameter / .NET User Secrets 从本机密码管理器（1Password / Bitwarden）获取。
  4. 审计日志记录 Secret 访问事件（[Kubernetes Audit Logging](https://kubernetes.io/docs/tasks/debug/debug-cluster/audit/)）。
  5. v2 升级路径：替换为 Azure Key Vault + CSI driver，业务代码 / `appsettings.json` 键名不变，仅部署层变更；提前在 Helm Chart 预留 `secretsProvider: kubernetes | keyvault` 开关。
  6. 商业合规场景（SOC2 / 客户安全审计）在合同条款中明确声明 v1 凭据姿态；外部审计要求 KV 的客户走 v2 选购路径。
- **残余风险**：v1 凭据泄漏概率高于 Key Vault 方案；多人安全审计（SOC2 / ISO 27001 的 Secret 必须加密存储 控制项）可能不通过；轮换价格高（需 Pod 重启不是热轮换）。

## RISK-014 RedisStreamQueueProvider 运维代价

- **类别**：运维 / 一致性
- **触发条件**：[OQ-A008 closed §B](./open-questions-arch.md) 决议 v1 同期交付 `RedisStreamQueueProvider` csproj（环境对称原则：dev = `ChannelsQueueProvider` / integration + prod = `RedisStreamQueueProvider`）。本风险在 [ADR-018](./adr/ADR-018-queue-abstraction-channels-default.md) accepted 同时激活。
- **影响范围**：[ADR-018 IQueueProvider](./adr/ADR-018-queue-abstraction-channels-default.md) 的全部消费者（KB ingest / 后台慢任务等 DurableTask 之外的 fire-and-forget 场景；触发器 / 多 Agent 编排等 v2 候选消费者已于 2026-07-09 推迟，详见 [requirements.md §13 第 28 条](../01-requirements/requirements.md)）；AKS HPA 多副本部署下的 worker pool fairness；Redis 实例负载（缓存 + 队列双职能）。
- **缓解方案（可执行）**：
  1. **DLQ + 可靠性全局默认**：[ADR-018](./adr/ADR-018-queue-abstraction-channels-default.md) 锁 N=3 24h DLQ + Redis Streams 内置语义（[`XREADGROUP`](https://redis.io/docs/latest/commands/xreadgroup/) consumer group + [`XCLAIM`](https://redis.io/docs/latest/commands/xclaim/) visibility timeout = 5 min + 指数退避 1s/max 60s）；H3 [Inkwell.Queue.Redis](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) HD 仅 thin wrapper 交付，不自建可靠性逻辑。
  2. **observability v1 必发**：`queue_depth`（[XLEN](https://redis.io/docs/latest/commands/xlen/)）；**prod 上线前补齐**五项：`queue_consume_latency_p95` / `queue_dlq_count` / `queue_consumer_lag`（[XPENDING](https://redis.io/docs/latest/commands/xpending/) summary） / `queue_redelivery_count` / `queue_consumer_active`——H6 release-notes 里指明未补齐者不得入 prod。
  3. **与 [Inkwell.Cache.Redis](./adr/ADR-016-cache-provider-redis.md) Redis 实例复用或独立**：H3 HD 决：(a) 复用同一 Redis 实例 + 不同 db number（运维简单，但 cache eviction 与 queue persistence 策略冲突）；(b) 独立部署 Redis 实例（AKS StatefulSet 多加 1 PVC，运维代价 vs 质量隔离）。建议项 (b)，prod ProdReady checklist 锁 (b)。
  4. **H4 补鱼类用例**：(a) crash recovery——worker SIGKILL 后 PEL 中未 ack 的 message 在 visibility timeout=5 min 后被重插；(b) fairness——多副本 worker 并发抢同一 stream，跨副本 ack 顺序不得出现永久偏斜；(c) DLQ——连续 3 次失败后 message 进入 `<queueName>:dlq` stream，TTL 24h 后被 Redis trim。
  5. **开发态零迁移代价**：dev 继续默认 `ChannelsQueueProvider`——零 Redis 依赖、零 docker compose 启动压力；只有 integration test + prod 才 `AddInkwell().UseRedisQueue(...)` 插入 `RedisStreamQueueProvider` 覆盖默认。
- **残余风险**：
  1. **observability 指标补齐跨期**：v1 交付仅 `queue_depth`，五项补齐项有漂洗进 prod 未补之可能。需在 [tech-debt-tracker](../../../.he/docs/tech-debt-gc.md) 登记并在 H6 release-notes Pre-prod 检查表未勾选则拒升 prod。
  2. **DLQ 人工处理接口 v1 不交付**：DLQ 被动退保、指标可报警，但 v1 不提供“DLQ 重发 / 检视”管理 UI——运维需 [`redis-cli XRANGE <queueName>:dlq`](https://redis.io/docs/latest/commands/xrange/) 手工检视。v2 backlog 补。
  3. **单 Redis 实例双职能**：若 H3 HD 选复用 Redis（(a)），cache eviction 与 queue 持久化冲突可能出现；建议项 (b) 独立部署。
  4. **`ChannelsQueueProvider` 伪造 ID + DeliveryCount 与 Redis 实现语义差**：H4 contract test 必须覆盖两 Provider 同样接口使用代码下的行为一致性（[Inkwell.Providers.Contract](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) 公共用例包补 Queue 部分）。

## RISK-015 WebApi / Worker / Migrator 三产物版本漂移与 OTel 双 source

- **类别**：部署 / 可观测性
- **触发条件**：[ADR-019](./adr/ADR-019-process-topology-webapi-worker-split.md) 锁定后端进程拓扑为 `Inkwell.WebApi` + `Inkwell.Worker` 双进程独立 Pod；[ADR-024](./adr/ADR-024-database-migration-seed-standalone-job.md) 新增 `Inkwell.Migrator` 一次性 Job，与前两者共用同一镜像 tag。运维若分次 Helm release（先滚 webapi 再滚 worker，或 Migrator Job 用了旧 tag）或镜像 tag 配置写错，三个产物可能跑不同 commit 的代码——enqueue 侧（webapi）写入新格式 message、consume 侧（worker）按旧 schema 解析会 silent data corruption / DLQ 堆积；Migrator 用旧 tag 执行迁移则可能漏掉最新的 schema 变更，导致 webapi/worker 启动后因 schema 不匹配报错。同时 OTel 由单 source 变双 source（`service.name` = `inkwell-webapi` / `inkwell-worker`；`Inkwell.Migrator` 是一次性 Job，不常驻，不计入 OTel 双 source 范畴，但仍应发 startup/exit 日志供 Helm hook 排障），dashboards 与 alert 规则需要双源覆盖，否则 worker 故障可能在仅看 webapi metric 时被忽略。
- **影响范围**：[ADR-005 prod 部署](./adr/ADR-005-deployment-docker-compose-aks.md) Helm Chart；[ADR-013 OTel pipeline](./adr/ADR-013-observability-otel-self-hosted-grafana.md)；[ADR-018 IQueueProvider](./adr/ADR-018-queue-abstraction-channels-default.md) 跨进程 enqueue / consume 语义；DurableTask actor placement（[ADR-006](./adr/ADR-006-orchestration-canvas-react-flow.md)）；[ADR-024 Migrator](./adr/ADR-024-database-migration-seed-standalone-job.md) 与 webapi/worker 的镜像 tag 一致性。
- **缓解方案（可执行）**：
  1. **Helm Chart 单 image tag**：[`charts/inkwell/values.yaml`](./adr/ADR-005-deployment-docker-compose-aks.md) 用 `image.tag` 单值控制，`webapi` / `worker` Deployment 与 `migrator` hook Job 均引用 `{{ .Values.image.tag }}`；CI 失败拒绝进 prod 时若 tag 不一致即报错。
  2. **单 release 同时滚**：`helm upgrade --atomic` 把 webapi + worker + migrator hook 视为同一 release 的子资源；Helm hook 原生保证 Migrator Job 跑完（成功）才继续滚动 webapi/worker，不允许绕过 hook 单独滚某一方——SOP 写进 [ADR-005 §部署](./adr/ADR-005-deployment-docker-compose-aks.md)。
  3. **OTel `service.name` 双 source**：[ADR-013](./adr/ADR-013-observability-otel-self-hosted-grafana.md) Resource Builder 必须设 `service.name = inkwell-webapi` / `inkwell-worker`；Grafana 默认 Dashboard 增加「Worker 健康」面板（Pod restart count / `BackgroundService` exception rate / `queue_consumer_active`）；alert 规则按 `service.name` 维度切分。`Inkwell.Migrator` 的执行结果（成功/失败 + 耗时）走 Helm hook Job 的标准日志，不接入 OTel 常驻 pipeline。
  4. **跨服务集成测试**：H4 必须有 enqueue (WebApi) → consume (Worker) → ack 全链路用例，覆盖 KB ingest [REQ-009](../01-requirements/requirements.md) / DurableTask 等典型异步场景（原触发器 REQ-011 场景已于 2026-07-09 推迟至 v2）。
  5. **schema 兼容性 SOP**：H3 [Inkwell.Queue.Redis](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) HD 锁定 message envelope schema 演进规则——新字段必须可选（向后兼容），废弃字段保留至少两个 release。
- **残余风险**：
  1. **OTel collector 单点**：[ADR-013](./adr/ADR-013-observability-otel-self-hosted-grafana.md) v1 OTel collector 单 Deployment——双 source 后 collector 故障会同时丢 webapi + worker 信号；prod HPA 多副本是 [ADR-013](./adr/ADR-013-observability-otel-self-hosted-grafana.md) 后果·中性段已声明项，本风险不重复计。
  2. **跨服务 trace correlation**：webapi enqueue → worker consume 的 trace span 需要靠 OTel context propagation 注入 message header；若 H3 [Inkwell.Queue.Redis](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) HD 漏掉这一步，[REQ-014 trace 全链路](../01-requirements/requirements.md) 在异步场景下断链。补偿手段：`MessageEnvelope` 必含 `traceparent` 字段，由 H4 集成测试覆盖。
  3. **dev / prod 一致性**：dev Compose 三容器（webapi/worker/migrator）与 prod Helm 双 Deployment + 1 hook Job 都遵 [ADR-019 双进程](./adr/ADR-019-process-topology-webapi-worker-split.md) + [ADR-024 Migrator](./adr/ADR-024-database-migration-seed-standalone-job.md)；但 dev 单机资源紧（Worker 占 1 容器额外 200MB）可能让开发者改 Compose override 成单容器——会重新引入「环境偏移 bug」。在 [`docker-compose.azurite.override.yml`](../../../docker-compose.azurite.override.yml) 同级补 `docker-compose.single-process.override.yml` 警告 banner 说明仅限性能受限场景使用。
  4. **Migrator hook Job 失败无自动重试**：Helm hook 失败不会自动重跑，需要人工排查后重新触发 `helm upgrade`；这是有意为之（避免带故障重复跑迁移），但需要写进运维 runbook，避免值班人员不知道要手动重跑。

## RISK-016 InMemoryVectorStore 与 Qdrant 语义偏移 / Microsoft.Extensions.VectorData 上游变化

- **类别**：环境差异 / 上游依赖
- **触发条件**：[ADR-020](./adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 锁定 v1 = Qdrant + InMemory 双 Provider。[Microsoft.Extensions.VectorData InMemory connector](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data) 不支持 Qdrant 的部分高级特性（hybrid search / geo filter / advanced HNSW 调优 / 名名字 vector 多向量 collection 等）。KB / Memory 开发者 unit test 在 InMemory 上跱试通过，prod 走 Qdrant 才发现依赖了 InMemory 不支持的高级特性 / 查询形状性能偏差。同时 [Microsoft.Extensions.VectorData.Abstractions](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data) NuGet 是上游独立发布周期，未来版本可能带 breaking change（接口 default impl / attribute 面谁 / 查询 builder API）传导到 Inkwell consumer。
- **影响范围**：[REQ-009 知识库](../01-requirements/requirements.md) / [REQ-010 长期记忆](../01-requirements/requirements.md)；Inkwell.Core.KnowledgeBase / Inkwell.Core.Memory Service 层查询代码；Inkwell.Abstractions Builder DSL（`UseQdrantVectorStore` / `UseInMemoryVectorStore`）。
- **缓解方案（可执行）**：
  1. **vector contract 用例包**：`tests/core/Inkwell.Providers.Contract/` （与 [RISK-011](#risk-011-文件存储三-provider-contract-漏出)同构）加 vector 章节——只覆盖 InMemory 与 Qdrant **均支持的子集语义**（基本 CRUD / 余弦相似 / metadata payload filter）；CI matrix 跑两 Provider 同一套用例，偏差即告警。
  2. **Qdrant only feature 标注**：H3 [Inkwell.Core.KnowledgeBase / .Memory 详细设计](../04-detailed-design/) 中使用 hybrid search / geo filter 等高级特性的查询必须加 `[QdrantOnly]` 注释 + `// TODO(InMemoryFallback)` 说明；unit test 如跳过该条路径的必须告警“该测试需 integration test 补上”。
  3. **NuGet 锁定 + 升级 SOP**：[Directory.Packages.props](../../../Directory.Packages.props) 锁定 `Microsoft.Extensions.VectorData.Abstractions` / `.InMemory` / `.Qdrant` 同 minor；升级上游前跑全量 vector contract 用例 + KB / Memory H4 用例，breaking change 走与 [ADR-003 MAF](./adr/ADR-003-agent-engine-microsoft-agent-framework.md) [RISK-001](#risk-001-microsoft-agent-framework-成熟度) 同一个 SOP。
  4. **向量维度 / metric 类型锁定**：H3 锁定 embedding 模型 = `text-embedding-3-large`（1536 维，cosine）；如未来换型号需同时以 ADR 记录并重建 collection。
  5. **embedding 生成点集中化**：KB / Memory Service 层不应重复调 [`Microsoft.Extensions.AI.IEmbeddingGenerator`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.ai.iembeddinggenerator-2)；H3 抽公共 `EmbeddingProducer` 服务封装 retry / timeout，避免成本估算不一致。
- **残余风险**：
  1. **InMemory 查询形状性能偏移仍可能逃逸**：unit test 跳过“高级特性”路径后仅走 happy path，prod 上 Qdrant 查询 latency 在某些 filter 组合下可能超预期。补偿手段：H4 integration test 油走完整 KB / Memory query 路径，并加 `qdrant_query_latency_p95` metric。
  2. **上游 NuGet breaking change 时间不可控**：[ADR-003 MAF](./adr/ADR-003-agent-engine-microsoft-agent-framework.md) 本身升级同期可能会拉高 M.E.VectorData 版本要求；v1 同一贴近发布期的两个升级窗口可能在一个 sprint 内叠加，赋资源压力。
  3. **`[QdrantOnly]` 标注是软约束**：开发者可能忘加，造成在 InMemory 上退化出错误结果但测试跱试通过。补偿手段：H4 [TestCaseAuthor](../../.he/agents/test-case-author/) 起草时加 Roslyn analyzer（与 [RISK-001](#risk-001-microsoft-agent-framework-成熟度) 软边界同 SOP）检查常见 hybrid search API 调用点是否有标注。

## RISK-017 EFCore family 幂等 DataSeed / schema 最小公倍数 / family 例外蔓延

- **类别**：数据层 / 依赖拓扑
- **触发条件**：[ADR-021](./adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 锁定 EFCore family = 3 csproj（1 共享 base + 2 final adapter）后，三个风险同时插足：（1）`InkwellSeeder.SeedAsync()` 默认 startup 运行，若幂等判定错误（多副本 WebApi 同时启动 / pod restart 圈 / dev 环境多次重启）会造成 seed 重复插入、唯一索引冲突、或升级版本后 seed 与 Migration 版本错位；（2）Entity 在共享 base 集中后，`OnModelCreating` 需同时兼容 SqlServer 2025 与 PostgreSQL 18 两套列型 / 索引 / 并发控制，最小公倍数 schema 会让特定引擎能力（`rowversion` / `jsonb` / `tsvector` / `xmin`）被险隐；（3）[ADR-017 §3.2](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) 依赖规则为 EFCore family 破例后，其他 family（FileStorage / Cache / Queue / VectorStore）可能提出同类诉求的 “shared base + final adapter” 软价 PR，如果没有明确准入机制会蔓延为“provider 互相引用的跳板”，最终压垮 [ADR-017](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) Ports & Adapters 边界。
- **影响范围**：[REQ-001](../01-requirements/requirements.md) ~ [REQ-017](../01-requirements/requirements.md) 全部依赖数据存储的模块（`Inkwell.Core.Auth` / `.Agents` / `.Conversations` / `.KnowledgeBase` / `.Memory` / `.Versioning` / `.Traces` 等）；`providers/Inkwell.Persistence.EFCore/` 共享 base 代码以及 SqlServer / Postgres final adapter Migration 文件；H3 [Inkwell.Abstractions HD](../04-detailed-design/) 中 `IPersistenceProvider` 接口容量化程度。
- **缓解方案（可执行）**：
  1. **幂等详细设计位**：H3 `Inkwell.Core.Persistence.Bootstrap` HD 中为 `InkwellSeeder` 明列以下三点：（i）每条 seed 记录必须按**业务唯一键**（如 `User.Email` / `Agent.Name + Owner` / `Tool.PublicName + Version`）调用 [`UPSERT` 语义](https://learn.microsoft.com/ef/core/saving/transactions)，不重建 Id；（ii）seed 与 Migration 版本一体化，所有 `Migrations/` 下含初始数据的 Migration 只面向一个版本区间，跨版本增量走 [`HasData()`](https://learn.microsoft.com/ef/core/modeling/data-seeding) + Migration；（iii）[`MigrationRunner`](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying) 启动时使用 Postgres `pg_advisory_lock` / SqlServer `sp_getapplock` 互斥锁避免多副本同时 seed。
  2. **EFCore-Conditional schema 设计公约**：H3 `InkwellDbContext.OnModelCreating` HD 中明列公约——默认只用两引擎都支持的能力（[`ConcurrencyToken`](https://learn.microsoft.com/ef/core/modeling/concurrency) / [`HasIndex().IsUnique()`](https://learn.microsoft.com/ef/core/modeling/indexes) / `nvarchar`）；如需引擎特定能力（`jsonb` / `tsvector` / `rowversion`）必须走 [`Database.IsSqlServer()` / `IsNpgsql()` 分支](https://learn.microsoft.com/ef/core/providers/) + Provider-specific extension method；该公约在 H3 [Inkwell.Core.Persistence HD](../04-detailed-design/) 中锁定，PR 走 [h3-detailed-design-reviewer](../../.he/agents/detailed-design-reviewer/) 机械化核查。
  3. **EFCore family 例外锁定 + 其他 family 准入机制**：[ADR-017 §3.2](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) 增补 EFCore family 例外后，同时错错明列“其他 family 不享受此例外”；如未来某 family（如 FileStorage 多云统一 metadata）要走同样拓扑，必须以独立 ADR 为入口（备选项打分 + 后果 + 迁移路径），判定不能仅凭 PR 试调。[h2-architect-advisor](../../.he/agents/architect-advisor/) 作为准入 gate。
  4. **contract test 改造**：`tests/core/Inkwell.Providers.Contract/` Persistence 章节加 SqlServer × Postgres 双 Provider matrix，覆盖：seed 幂等（连跑三次 SeedAsync） / Migration 重入（同一 Migration 起点 vs 升级后 Migration） / `OnModelCreating` Provider-specific 分支调起 / DbContextPooling 与多线程 SeedAsync；CI matrix 跳过文件存储那三套、独立运行。
  5. **Migration drift detection**：CI 在 SqlServer × Postgres 两套上跑 [`dotnet ef migrations has-pending-model-changes`](https://learn.microsoft.com/ef/core/cli/dotnet)，防止 Entity 变更后仅为其中一套生成 Migration；[Directory.Build.props](../../../Directory.Build.props) 加 `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` 让 Provider-specific tooling 警告不被吞。
- **残余风险**：
  1. **dev / 测试环境启动成本上升**：2026-07-08 移除 InMemory 关系型 Provider 后，dev / unit test 改用 Testcontainers 起真实 SqlServer / Postgres 实例，相比原 InMemory 快速反馈失去了零依赖 / 秒级启动的优势，本地开发与 CI 均需容器运行时。补偿手段：Testcontainers 镜像预拉取 + 复用容器（`Testcontainers.Xunit` / `ryuk` 生命周期管理）降低单次启动开销。
  2. **幂等未覆盖的跨版本路径**：seed 跨版本修改（如从 v1.0 升到 v1.1 后某条 seed 被修改）仍可能存在“老记录保留 vs 新记录覆盖”的决策质量问题。补偿手段：H6 [release-note-writer](../../.he/agents/release-note-writer/) 在发布说明中锅漆“seed 变更」独立章节、指向迁移脚本。
  3. **其他 family PR 试加例外**：即使明文锁定，能不能压制依赖蔓延仍取决于 PR 评审人。补偿手段：[h3-detailed-design-reviewer](../../.he/agents/detailed-design-reviewer/) 与 [h5-commit-auditor](../../.he/agents/commit-auditor/) 机械化打击`providers/<X>` 不在 EFCore family 例外名单上、但出现跨 provider `using` 的 commit。

> **2026-07-06 errata（同步 [ADR-021 2026-07-06 errata](./adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md)）**：上方触发条件 (1) 与缓解方案 1 中「`MigrationRunner` 启动时使用 Postgres `pg_advisory_lock` / SqlServer `sp_getapplock` 互斥锁避免多副本同时 seed」里隐含的「Migration 也在 `Inkwell.WebApi` 启动时跑」前提已变：Migration 改由 CI/CD pipeline 独立步骤在部署前执行，`Inkwell.WebApi` 启动时**不再**触发 Migration，多副本同时执行 Migration 的场景被结构性消除。该互斥锁机制**保留但缩小范围**——仅需防止多副本 `InkwellSeeder.SeedAsync()` 并发写入 seed 数据本身，不再需要协调 Migration 与 Seed 的先后顺序（Migration 完成时间点已由 CI/CD 保证早于应用启动）。触发条件 (1) 中「seed 与 Migration 版本错位」的残余风险相应降低——CI/CD 部署前置检查（[`dotnet ef migrations has-pending-model-changes`](https://learn.microsoft.com/ef/core/cli/dotnet)，见缓解方案 5）可在应用启动前拦截 schema 未就绪的情况。触发原因：H3 HD-011 起草期发现的生产安全考量，Owner 拍板；详见 [ADR-021 §「Migration / DataSeed 启动行为」](./adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) errata。

## RISK-018 mixin 体系演进扫漏（手写 mapper 模式下）

- **类别**：技术债 / 静态检查
- **触发条件**：[ADR-022](./adr/ADR-022-entity-domain-mapper-selection.md) 锁定 Entity ↔ Model 手写 Extensions 映射后，mixin 与 mapping 文件间存在"实现某 mixin 但 mapping 未提及对应字段"的可能手误。v1 不强制落 Roslyn analyzer——未来若增加新 mixin（如 `IHasTenantId` 多租户）时，~18 个 mapper 文件需逐一 +字段 +测试——PR review 容易漏，造成运行期“某些 Entity 能存但 mapper 丢字段”隐藏错误。
- **影响范围**：`providers/Inkwell.Persistence.EFCore/Mapping/` 下全部 `<TypeName>MappingExtensions.cs`；上游 18 个业务命名空间的 Model / `IXxxRepository` 接口。
- **缓解方案（可执行）**：
  1. **HD-009 锁定纯手动公约**：§3.9 mapping 模板明确要求每个 `<TypeName>MappingExtensions` 同时覆盖 `IHasTimestamps` / `IHasRowVersion` / `IHasOwner` 三 mixin 的全部字段；§10 C1/C2 grep 趋动化扫描 `ToModel` / `ToEntity` / `SelectAsModel` 三方法齐备。
  2. **占位 Roslyn analyzer**：`MissingMixinFieldAnalyzer`——扫描某 Entity 实现某 mixin 但 `<TypeName>MappingExtensions.ToModel` / `.ToEntity` 未提及对应字段时编译报警；analyzer 源码起 H5 任务落实，v1 不阻塞。
  3. **PR 模板**：双端 HD（业务块 + HD-009） 提供 PR template checklist 重心提醒“如动了 mixin 请同时检查全部 mapping 文件”。
  4. **跨版本扫漏**：首次获得 H6 release-notes 时加入 `mixin 体系演进` 独立章节，迁移脚本引入必要时。
- **残余风险**：
  1. **手动依赖**：v1 未启用 analyzer，仅靠评审 + grep 手动检查是有漏面的；进入 [tech-debt-tracker](../../../.he/docs/tech-debt-gc.md) 跳转任务。
  2. **未来 mixin 增量未锁 ADR**：若 v2 加新 mixin（如多租户）需同期起 ADR 锁增量路径。

## RISK-019 LiteLLM 关键依赖与模型目录路由漂移

- **类别**：可用性 / 配置一致性
- **触发条件**：（1）LiteLLM 不可用、过载或升级后出现协议回归，全部 Agent 模型调用被阻断；（2）Inkwell Model Registry 中 `RuntimeId=litellm` 的 `RemoteModelId` 在 LiteLLM 中不存在；（3）运维修改同名路由后，已发布 Agent 的后续调用落到不同上游模型。
- **影响范围**：所有经过 MAF Agent Factory 发起的文本、多模态、tool calling 和 structured output 调用；REQ-005 / REQ-006 / REQ-014。
- **缓解方案（可执行）**：
  1. 部署前检查每个可用 LiteLLM 模型的 `RemoteModelId` 均存在于 LiteLLM 发现结果，并拒绝重复或空模型标识。
  2. H4 增加 streaming、tool calling、structured output、图片、取消、429/5xx/fallback、token usage 和模型标识的端到端契约矩阵。
  3. LiteLLM 配置进入版本控制；生产路由变更必须关联发布记录并支持回滚，禁止控制台无审计热改。
  4. 配置 readiness / liveness、请求超时、并发上限和脱敏日志；压测后决定生产副本数与共享预算存储。
  5. trace 同时记录 Inkwell `ModelId`、`SourceId`、`RuntimeId`、`RemoteModelId`、实际上游模型标识和网关请求标识（可获得时），定位 fallback 与路由漂移。
- **残余风险**：v1 不复制完整 LiteLLM 路由到 Agent 快照，同名路由变化后历史 Agent 不能保证逐次完全重放；网关本身仍是新增供应链和运行时依赖。

## 1. 自检

- 风险数量：19 条 ≥ 10（[architect-advisor/AGENT.md §6](../../../.he/agents/architect-advisor/AGENT.md) 阈值通过）。
- 字段完整性：每条都填了 类别 / 触发条件 / 影响范围 / 缓解方案 / 残余风险。
- 缓解方案均为可执行（不是"加强测试""提高质量"等空话）。
- W-003 已通过 RISK-003 显式记录，闭环。
- RISK-007 重写（从 RunEventStore 吞吐 → 主进程长 SSE 跨锁屏可靠性）与 [OQ-A002 closed §A](./open-questions-arch.md) 同步；RISK-012 / RISK-013 与 [OQ-A004 closed §B](./open-questions-arch.md) / [OQ-A006 closed §B](./open-questions-arch.md) 同步。
- RISK-001 缓解方案 2026-05-10 重写：`Inkwell.AgentRuntime` 独立 csproj 硬边界 → [ADR-017](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) `Inkwell.Core.AgentRuntime` 命名空间 + Roslyn analyzer / `BannedSymbols.txt` 软边界；新增残余风险"软边界漂洗"。
- RISK-014 从占位激活：[OQ-A008 closed §B](./open-questions-arch.md) 决议 v1 同期出 `RedisStreamQueueProvider` csproj；DLQ N=3 24h + Redis Streams 内置语义锁定；observability v1 仅 `queue_depth`，其余五项进 prod ProdReady checklist。
- RISK-015 新增：[ADR-019](./adr/ADR-019-process-topology-webapi-worker-split.md) 双进程拓扑落地后，Helm 单 image tag + 单 release 同时滚 + OTel `service.name` 双 source + 跨服务集成测试 + schema 兼容性 SOP 是 H3 / H4 / H6 的硬约束；2026-07-09 随 [ADR-024](./adr/ADR-024-database-migration-seed-standalone-job.md) 新增 `Inkwell.Migrator` 一次性 Job 后扩展为三产物同镜像 tag 同步。
- RISK-016 新增：[ADR-020](./adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 双 Provider 拓扑落地后，vector contract 用例包仅覆盖 InMemory × Qdrant 子集语义 + Qdrant-only feature 标注 + NuGet 锁定 + embedding 点集中化 + 向量维度锁定是 H3 / H4 的硬约束。
- RISK-017 新增：[ADR-021](./adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) EFCore family 4-csproj 布局落地后，`InkwellSeeder` 幂等详细设计位 + `OnModelCreating` Provider-specific 分支公约 + 其他 family 不享受 `providers/* 依赖` 例外 + Migration drift CI + SqlServer × Postgres 双 Provider contract test matrix 是 H3 / H4 / H5 的硬约束。
- RISK-018 新增：[ADR-022](./adr/ADR-022-entity-domain-mapper-selection.md) 手写 Extensions 映射锁定后，`MissingMixinFieldAnalyzer` 未激活为 v1 可接受占位（靠 HD-009 §3.9 mapping 公约 + §10 grep C1/C2 + PR template + tech-debt-tracker 跨 sprint review）；v2 增量 mixin 时需同期起 ADR + 激活 analyzer。
- RISK-019 新增：[ADR-026](./adr/ADR-026-model-gateway-litellm.md) 引入 LiteLLM 后，以部署前目录/路由一致性检查 + OpenAI-compatible 协议契约矩阵 + 配置版本控制 + trace 路由字段缓解网关单点与双配置漂移。
