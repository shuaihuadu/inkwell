---
id: risk-analysis-inkwell-agent-platform
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [ Inkwell ]
created: 2026-05-08
updated: 2026-05-09
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
downstream:
  - architecture-inkwell-agent-platform
---

# Inkwell Agent 平台 · 风险分析

> 本文档对应 [stages.md §5.4](../../../.he/docs/stages.md) "主要技术风险" 与 [agents/architect-advisor/AGENT.md §4.3](../../../.he/agents/architect-advisor/AGENT.md) 的字段要求：风险编号 / 类别 / 触发条件 / 影响范围 / 缓解方案 / 残余风险。每条 RISK-NNN 至少要有一条可执行的缓解动作。

## 0. 风险摘要

| 编号                                                           | 类别            | 主题                                  | 严重度 | 关联                                                                                                                             |
| -------------------------------------------------------------- | --------------- | ------------------------------------- | ------ | -------------------------------------------------------------------------------------------------------------------------------- |
| [RISK-001](#risk-001-microsoft-agent-framework-成熟度)         | 依赖成熟度      | MAF 仍在演进                          | 中     | [ADR-003](./adr/ADR-003-agent-engine-microsoft-agent-framework.md)                                                               |
| [RISK-002](#risk-002-ipersistenceprovider-切换抽象漏出)        | 数据层          | IPersistenceProvider 抽象漏出         | 中     | [ADR-004](./adr/ADR-004-data-store-provider-switchable-ef-core.md)                                                               |
| [RISK-003](#risk-003-nfr-003-字面与-oq-017-文字差异-w-003)     | 文档一致性      | NFR-003 字面与 OQ-017 差异 W-003      | 低     | [NFR-003](../01-requirements/requirements.md) / [OQ-017](../01-requirements/open-questions.md)                                   |
| [RISK-004](#risk-004-aks-单-region-可用性)                     | 可用性          | AKS 单 region 不具备跨区高可用        | 中     | [ADR-005](./adr/ADR-005-deployment-docker-compose-aks.md)                                                                        |
| [RISK-005](#risk-005-azure-speech-依赖--成本)                  | 依赖成本        | Azure Speech 调用量与可用性           | 低     | [ADR-009](./adr/ADR-009-multimodal-azure-speech.md)                                                                              |
| [RISK-006](#risk-006-自托管-grafana-栈数据保留--运维)          | 运维            | Grafana 栈数据保留 + 备份             | 中     | [ADR-013](./adr/ADR-013-observability-otel-self-hosted-grafana.md)                                                               |
| [RISK-007](#risk-007-主进程长-sse-跨锁屏可靠性)                | 可靠性          | 主进程长 SSE 跨锁屏休眠重连           | 中     | [ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md) / [ADR-012](./adr/ADR-012-client-server-protocol-rest-agui.md) |
| [RISK-008](#risk-008-v1-范围裁剪压力)                          | 进度 / 范围     | OQ-006 范围裁剪是否兜得住             | 中     | [OQ-006](../01-requirements/open-questions.md)                                                                                   |
| [RISK-009](#risk-009-skill-加载错误传播到对话)                 | 体验 / 错误     | Skill 加载失败影响对话                | 低     | [ADR-010](./adr/ADR-010-skill-loading-static-only-v1.md) / [EX-008](../01-requirements/requirements.md)                          |
| [RISK-010](#risk-010-v1-不引入-i18n-的-v2-重做成本)            | 技术债          | v2 引入 i18n 重构成本                 | 低     | [ADR-014](./adr/ADR-014-i18n-out-of-scope-v1.md)                                                                                 |
| [RISK-011](#risk-011-文件存储三-provider-contract-漏出)        | 数据层 / 测试   | 文件存储三 Provider contract 测试漏出 | 中     | [ADR-015](./adr/ADR-015-object-storage-provider-switchable.md)                                                                   |
| [RISK-012](#risk-012-redis-单点与缓存-invalidation-一致性)     | 数据层 / 一致性 | Redis 单点 + 多副本 invalidation      | 中     | [ADR-016](./adr/ADR-016-cache-provider-redis.md)                                                                                 |
| [RISK-013](#risk-013-v1-未引入-key-vault-的凭据轮换与隔离弱化) | 安全 / 合规     | K8s Secret + .env 弱于 Key Vault      | 中     | [ADR-005](./adr/ADR-005-deployment-docker-compose-aks.md) / [OQ-A006 closed §B](./open-questions-arch.md)                        |

## RISK-001 Microsoft Agent Framework 成熟度

- **类别**：依赖成熟度
- **触发条件**：MAF 在 v1 开发周期内发布 breaking change（接口签名变更 / NuGet 包重组）；或 MAF 中关键能力（Workflows / DurableTask / AGUI）在生产场景出现稳定性问题。
- **影响范围**：[ADR-003](./adr/ADR-003-agent-engine-microsoft-agent-framework.md) 引入的全部 17 个 `Inkwell.*` 模块（[§3.1 repo-impact-map](../01-requirements/repo-impact-map.md)）。
- **缓解方案（可执行）**：
  1. 锁定具体 NuGet 版本（pin 到 patch level）在 [Directory.Packages.props](../../) — 不使用 `*` 通配符。
  2. 升级 MAF 之前必须跑全量 H4 用例 + 集成测试，通过后再合并。
  3. 在 [Inkwell.AgentRuntime](../01-requirements/repo-impact-map.md) 模块对 MAF 关键 API 做"门面"封装，减少跨模块的直接依赖。
  4. 监控 [microsoft/agent-framework releases](https://github.com/microsoft/agent-framework/releases) 与 ADR-003 配套的"已知 incompat 清单"在 H6 release-notes 中定期回写。
- **残余风险**：MAF 在 v1 周期内出现严重设计变更（如把 `IChatClient` 拆分），将不得不进行较大规模重构。

## RISK-002 IPersistenceProvider 切换抽象漏出

- **类别**：数据层
- **触发条件**：业务查询需要 Provider-specific 特性（如 PostgreSQL `JSONB` 操作符 / SQL Server `OUTPUT` 子句 / `FILTER WHERE`），跨 Provider 实现性能差距数量级。
- **影响范围**：[ADR-004 IPersistenceProvider](./adr/ADR-004-data-store-provider-switchable-ef-core.md) 涉及的所有 Repository（`Inkwell.Identity` / `Inkwell.Agents` / `Inkwell.Conversations` / `Inkwell.AuditLogs` 等 7 个模块）。
- **缓解方案（可执行）**：
  1. CI 加 [matrix job](https://docs.github.com/actions/using-jobs/using-a-matrix-for-your-jobs)：每个 PR 跑三套 Provider 集成测试。
  2. Repository 层引入"Provider-specific Strategy"模式：`IUserRepository` 接口 + `PostgresUserRepository` / `SqlServerUserRepository` / `InMemoryUserRepository` 三实现，应用代码只引用接口。
  3. H3 详细设计阶段绘制"查询性能矩阵"：列出每个关键查询在三种 Provider 下的预期 P95，作为验收门禁。
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
  2. 后端记录每次 Azure Speech 调用的"调用量 + 时长 + 错误码"到 [审计日志](./adr/ADR-008-audit-log-store-and-query.md)。
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
  2. 失败事件写 [审计日志](./adr/ADR-008-audit-log-store-and-query.md)，UI Skill 详情页显示错误状态。
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
- **影响范围**：所有依赖 `IFileStorageProvider` 的模块：`Inkwell.Multimodal`（[ADR-009](./adr/ADR-009-multimodal-azure-speech.md) 多模态预签名 URL） / `Inkwell.KnowledgeBase`（知识库原始件 + 抽取后 Markdown） / `Inkwell.AuditLogs` v2 导出。
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
- **影响范围**：Azure Speech / Azure OpenAI 等所有外部 API 凭据 / Redis 连接串 / DB 连接串 / Public API Token 哈希盐。
- **缓解方案（可执行）**：
  1. AKS 启用 [etcd encryption-at-rest provider config](https://kubernetes.io/docs/tasks/administer-cluster/encrypt-data/)，避免 Secret 在持久化层明文。
  2. RBAC 收敛：仅 deployer / runtime ServiceAccount 可读 Secret；禁止 cluster-admin 外的人工访问。
  3. `.env` 进 [.gitignore](https://git-scm.com/docs/gitignore) + dev 文档明确 .env 不入仓；[`docker compose`](https://docs.docker.com/compose/) 启动脚本从本机密码管理器 (1Password / Bitwarden) 拉取。
  4. 审计日志记录 Secret 访问事件（[Kubernetes Audit Logging](https://kubernetes.io/docs/tasks/debug/debug-cluster/audit/)）。
  5. v2 升级路径：替换为 Azure Key Vault + CSI driver，业务代码 / `appsettings.json` 键名不变，仅部署层变更；提前在 Helm Chart 预留 `secretsProvider: kubernetes | keyvault` 开关。
  6. 商业合规场景（SOC2 / 客户安全审计）在合同条款中明确声明 v1 凭据姿态；外部审计要求 KV 的客户走 v2 选购路径。
- **残余风险**：v1 凭据泄漏概率高于 Key Vault 方案；多人安全审计（SOC2 / ISO 27001 的 Secret 必须加密存储 控制项）可能不通过；轮换价格高（需 Pod 重启不是热轮换）。

## 1. 自检

- 风险数量：13 条 ≥ 10（[architect-advisor/AGENT.md §6](../../../.he/agents/architect-advisor/AGENT.md) 阈值通过）。
- 字段完整性：每条都填了 类别 / 触发条件 / 影响范围 / 缓解方案 / 残余风险。
- 缓解方案均为可执行（不是"加强测试""提高质量"等空话）。
- W-003 已通过 RISK-003 显式记录，闭环。
- RISK-007 重写（从 RunEventStore 吞吐 → 主进程长 SSE 跨锁屏可靠性）与 [OQ-A002 closed §A](./open-questions-arch.md) 同步；RISK-012 / RISK-013 与 [OQ-A004 closed §B](./open-questions-arch.md) / [OQ-A006 closed §B](./open-questions-arch.md) 同步。
