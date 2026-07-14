---
id: open-questions-arch-inkwell-agent-platform
stage: H2
status: closed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [ Inkwell Owner ]
created: 2026-05-08
updated: 2026-05-10
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
downstream: []
---

<!-- 2026-05-10：随 ADR-017 / ADR-018 补录后追加 OQ-A008（RedisStreamQueueProvider v1 是否实现）；Owner 选 B（v1 同期实现）并提供三前置条件（环境对称论据 + DLQ 默认 + queue_depth），RISK-014 同步激活。OQ-A001 ~ OQ-A008 均 closed。 -->

# Inkwell Agent 平台 · 架构待澄清清单（H2）

> **本文件谁动手 / 在哪填**：
>
> - **Agent 起草** OQ-A001 起的题干 + 候选答 + 影响范围 + 建议默认值（status: `pending`）。
> - **人工填答**：每条 OQ 末尾的 `回答 / 决策日期 / 决策人` 三行就是输入位——把 `> **[ 待填 ]**：...` 整行替换成实际答案。允许写"接受默认值"。
> - **Agent 回写**：人工答完后由 `H2-ArchitectAdvisor` 在评审中把 `卡点等级` 改为 `closed YYYY-MM-DD`，并把"回写"行指向具体落点。
>
> **本文件存在的用途**：本次会话中，Owner 在 [对话](../../) 里授权"按建议默认值推进 Q-A4-followup 与 Q-A6-followup"。按 [agents/architect-advisor/prompt.md §第三步](../../.he/agents/architect-advisor/prompt.md)：所有"由 Agent 默认 + 待评审接受"的项必须显式写到本文件。`status: draft → reviewed` 的人工评审环节里，每条 OQ 都要被 Owner 接受或推翻。

---

## OQ-A001 数据库切换的"可切换"边界（Q-A4-followup）

- **问题**：[Q-A4](../01-requirements/repo-impact-map.md) 已锁"InMemory / SQL Server / PostgreSQL 三库可切换"。但具体边界未定——向量检索是否跟随 Provider 切换？
- **为什么需要答**：决定 [ADR-004](./adr/ADR-004-data-store-provider-switchable-ef-core.md) 与 [Inkwell.KnowledgeBase / Inkwell.Memory / Inkwell.Traces 模块](../01-requirements/repo-impact-map.md) 的存储拓扑。
- **影响范围**：REQ-009 / REQ-010 / REQ-014 / NFR-005；ADR-004；[Inkwell.KnowledgeBase / Inkwell.Memory / Inkwell.Traces](../01-requirements/repo-impact-map.md)。
- **候选答**：
  - **A**（Agent 默认推进）。仅 EF Core Provider 切换关系数据；向量库另选独立服务（推荐 [Qdrant](https://qdrant.tech/) 跨 dev / prod 一致部署）。后果：实现最干净；运维多一个服务；向量库与三种关系库正交。
  - **B**. 三种引擎都支持向量检索（SQL Server 2025 vector / pgvector / InMemory 内存索引）。后果：三套向量实现 + 切换抽象漏出，与 [OQ-006 closed §A](../01-requirements/open-questions.md) 范围风险冲突。
  - **C**. v1 仅 PostgreSQL + pgvector 真做，SQL Server / InMemory 仅占位。后果：等于锁 PostgreSQL，与 Q-A4"三库切换"承诺不符。
  - **D**. 其他（请显式列出）。
- **回答**：

  > 2026-05-09。接受 A（Agent 默认）。仅 EF Core Provider 切换关系数据（[IPersistenceProvider 抽象](./adr/ADR-004-data-store-provider-switchable-ef-core.md) + InMemory / SQL Server 2025 / PostgreSQL 17）；向量库由 Qdrant 1.x 独立服务承担，与关系层正交。

- **决策日期**：

  > 2026-05-09

- **决策人**：

  > Inkwell Owner

- **卡点等级**：closed
- **回写**：→ [ADR-004](./adr/ADR-004-data-store-provider-switchable-ef-core.md) §决策；[tech-selection.md §4](./tech-selection.md)；[architecture.md §4](./architecture.md)。

> **2026-07-08 决策更新（本条历史回答部分作废）**：Owner 决定移除 EF Core **InMemory 关系型数据库** Provider（不支持外键约束，本地开发 / 测试价值有限），持久化收敛为两 Provider（SQL Server 2025 / PostgreSQL 17），dev / 测试改用 [Testcontainers](https://testcontainers.com/) 起真实实例。上方"三库可切换" / "InMemory / SQL Server 2025 / PostgreSQL 17" 表述已不再成立；`InMemoryVectorStore`（向量存储子系统）不受影响。完整决策记录见 [ADR-004](./adr/ADR-004-data-store-provider-switchable-ef-core.md) / [ADR-021](./adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 2026-07-08 更新。本条历史记录保留不删，仅作废其中的三库表述。
>
> **2026-07-14 决策更新**：Owner 将 PostgreSQL 支持基线从 17 升级为 **18**，当前开发与测试固定到最新稳定补丁 `18.4`。上方历史回答及 2026-07-08 更新中的 PostgreSQL 17 仅保留为决策演进记录，当前有效组合为 SQL Server 2025 / PostgreSQL 18。

---

## OQ-A002 在途任务跨锁屏存活机制（Q-A6-followup）

- **问题**：[Q-A6](../01-requirements/repo-impact-map.md) 已锁"REST + AG-UI Protocol"；但 [NFR-003 + OQ-017 closed](../01-requirements/open-questions.md) 要求"锁屏期间录音 / 上传 / 流式继续，解锁后用户能看到结果"。这要求协议层支持跨锁屏的会话续接。
- **为什么需要答**：决定 [ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md) 与 [ADR-012](./adr/ADR-012-client-server-protocol-rest-agui.md) 的协同方式。
- **影响范围**：NFR-003 / REQ-016 / EX-001 ~ EX-008；ADR-011；ADR-012；[Inkwell.Traces / Inkwell.Conversations](../01-requirements/repo-impact-map.md)。
- **候选答**：
  - **A**. 仅 AG-UI 一通到底：Electron 主进程持 SSE，UI 进程切锁屏页，主进程在背后维持订阅。后果：实现最简单；要求主进程在用户合上盖子 / 系统休眠时仍持连接，长链可靠性依赖电源与网络调优 — 已表述为 [RISK-007](./risk-analysis.md)。
  - **B**. AG-UI（对话流）+ SignalR（锁屏推送 / 多端协同）。后果：双通道职责清晰；运维多一根线；与 .NET 生态原生支持的 AG-UI 已形成功能重叠。
  - **C**（Agent 默认推进）。AG-UI + Run resume：锁屏前断 SSE，解锁后用 `run_id + cursor` 重连续传；后端把事件落 store。后果：符合 AG-UI 协议本身的事件 store + replay 模式；后端必须实现事件持久化（已有 [Inkwell.Traces](../01-requirements/repo-impact-map.md)）；最贴 NFR-003 / OQ-017 的预期。
  - **D**. 其他（请显式列出）。
- **回答**：

  > 2026-05-09。接受 A。Owner 考虑后认为 v1 在 “单用户 + 主进程可保活” 场景下“AG-UI 一通到底＋主进程背后维持 SSE”实现成本最低；不引入 Run resume cursor、不引入 RunEventStore；可靠性问题走 [RISK-007](./risk-analysis.md) 表述 + DurableTask 兑底。与默认值 C 的反转说明：Run resume 所需的事件 store + cursor 语义与 v1 范围代价不成比例。

- **决策日期**：

  > 2026-05-09

- **决策人**：

  > Inkwell Owner

- **卡点等级**：closed
- **回写**：→ [ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md) 重写；[ADR-012](./adr/ADR-012-client-server-protocol-rest-agui.md) 删除 Run resume 端点 + cursor 语义；[risk-analysis.md RISK-007](./risk-analysis.md) 重写为“主进程长 SSE 跨锁屏可靠性”；[architecture.md §6 / §7 / §14](./architecture.md)。

---

## OQ-A003 W-003 NFR-003 字面缺 OQ-017 特例的处理

- **问题**：[requirements.md §6 NFR-003 / §11 NFR-003 验收口径](../01-requirements/requirements.md) 字面仍是"硬锁定"措辞，未补 [OQ-017 closed](../01-requirements/open-questions.md) 的"在途任务保留"特例；下游 [ui-spec.md / user-flow.md / acceptance-criteria.md](../01-requirements/) 已写入特例。本 H2 是把这条"文字漂移"显式记入 [risk-analysis.md RISK-003](./risk-analysis.md)，还是回 H1 由 [`H1-RequirementsInterviewer`](../../.he/agents/requirements-interviewer/) 在 §9 上游决策追加一条特例说明？
- **为什么需要答**：影响 H4 测试用例如何引用 NFR-003：若文字不补，AC-076 ~ AC-079 与 §11 验收口径互相打架；若回 H1 补，H1 status 需要重新走一遍 review。
- **影响范围**：NFR-003；[acceptance-criteria.md AC-076 ~ AC-079](../01-requirements/acceptance-criteria.md)；ADR-011；RISK-003。
- **候选答**：
  - **A**（Agent 默认推进）。本 H2 在 [risk-analysis.md RISK-003](./risk-analysis.md) 显式记入文字漂移风险，残余风险由 Owner 接受；下一次 H1 修订自然把字面带过去。后果：H1 status 不动；H2 / H3 / H4 不阻塞；H4 引用 NFR-003 时同步引用 ADR-011 + AC-076 ~ AC-079 的特例口径。
  - **B**. 回炉 H1 由 `H1-RequirementsInterviewer` 在 §6 NFR-003 表行 + §11 NFR-003 验收口径补"特例见 OQ-017 / ADR-011"，重走一遍 H1 review。后果：H1 多一轮迭代；H2 必须等 H1 重新 reviewed 后才能 lock ADR-011。
  - **C**. 其他（请显式列出）。
- **回答**：

  > 2026-05-09。接受 A（Agent 默认）。本 H2 在 [risk-analysis.md RISK-003](./risk-analysis.md) 显式记入文字漂移；H4 引用 NFR-003 时同步引用 [ADR-011](./adr/ADR-011-auto-lock-with-inflight-task-survival.md) + [AC-076 ~ AC-079](../01-requirements/acceptance-criteria.md) 的特例口径；下一次 H1 修订自然补上字面。

- **决策日期**：

  > 2026-05-09

- **决策人**：

  > Inkwell Owner

- **卡点等级**：closed
- **回写**：→ [risk-analysis.md RISK-003](./risk-analysis.md)。

---

## OQ-A004 v1 是否引入独立缓存层（Redis）

- **问题**：架构中是否引入独立的 Redis 缓存层（用于 session / model response cache / rate limit）？
- **为什么需要答**：决定 [Inkwell.Health / Inkwell.PublicApi](../01-requirements/repo-impact-map.md) 与 [架构图](./architecture.md)；影响 dev Compose 与 AKS Helm chart 的依赖项数量。
- **影响范围**：REQ-013（Public API rate limit）/ NFR-001 / [部署形态](./architecture.md#部署方式)；ADR-005。
- **候选答**：
  - **A**（Agent 默认推进）。v1 不引入独立 Redis；用 ASP.NET Core `IMemoryCache` + Microsoft Agent Framework 内置 thread state；Public API rate limit 用 `Microsoft.AspNetCore.RateLimiting`（基于内存 token bucket）。后果：依赖更少；多副本部署时缓存语义不强一致（用户量级 ~100 可忍）；后续真有性能瓶颈再升级 Redis。
  - **B**. v1 即引入 Redis（dev Azurite + AKS Azure Cache for Redis），所有 cache + rate limit 统一走分布式语义。后果：依赖多一个；多副本部署强一致；运维成本多一项。
  - **C**. 其他（请显式列出）。
- **回答**：

  > 2026-05-09。接受 B。Owner 反转默认值，v1 即引入 Redis：dev = 本机 Redis 8 容器 / prod = [Azure Cache for Redis](https://learn.microsoft.com/azure/azure-cache-for-redis/)；抽象为 `ICacheProvider`（与 [IPersistenceProvider](./adr/ADR-004-data-store-provider-switchable-ef-core.md) / [IFileStorageProvider](./adr/ADR-015-object-storage-provider-switchable.md) 同构）；session / model response cache / Public API rate limit 全部走分布式语义。

- **决策日期**：

  > 2026-05-09

- **决策人**：

  > Inkwell Owner

- **卡点等级**：closed
- **回写**：→ [ADR-016 缓存层：ICacheProvider + Redis](./adr/ADR-016-cache-provider-redis.md) 新建；[tech-selection.md §16 缓存层](./tech-selection.md)；[architecture.md §5 缓存策略](./architecture.md)；[ADR-005](./adr/ADR-005-deployment-docker-compose-aks.md) Compose / AKS 中加 Redis service。

---

## OQ-A005 v1 文件存储方案

- **问题**：用户上传的图片 / 语音 / 文档（REQ-016 + REQ-009）如何存储？候选：(a) Azure Blob Storage（prod） + [Azurite emulator](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (dev)，(b) MinIO 跨环境一致，(c) 数据库 BLOB 字段，(d) 三 Provider 切换（本地 / Azure Blob / MinIO）。
- **为什么需要答**：决定 [Inkwell.Multimodal / Inkwell.KnowledgeBase](../01-requirements/repo-impact-map.md) 的依赖项；影响 OQ-001 数据出境合规（DPA 范围）。
- **影响范围**：REQ-009 / REQ-016；NFR-006；ADR-005；ADR-009；ADR-015。
- **候选答**：
  - **A**. Azure Blob Storage（prod） + Azurite emulator（dev）。后果：与 Q-A5 AKS 部署一致；dev 用 emulator 避免账号配置；NFR-006 已签 DPA 厂商范围内。
  - **B**. MinIO 跨环境一致部署。后果：脱离 Azure 锁定；运维多一个 MinIO 实例；与 Q-A5 AKS 部署形态不对齐。
  - **C**. 直接存数据库 BLOB 字段。后果：实现最简单；数据库膨胀；不适合 ≥ MB 级文件。
  - **D**（由 Owner 在 H2 阶段提出，已接受）。三 Provider 切换：`LocalFileSystem` / `AzureBlob` / `MinIO`，同一 `IFileStorageProvider` 抽象，启动时按配置选择。与 [ADR-004 IPersistenceProvider](./adr/ADR-004-data-store-provider-switchable-ef-core.md) 同构；dev 默认 MinIO，单元测试默认 LocalFileSystem，prod 由客户选。成本：三 Provider contract test matrix；Helm Chart 三套 values 模板。
- **回答**：

  > 2026-05-08（初始决议） / 2026-05-09（复议保留）。接受 D。Owner 在 H2 阶段提出“本地 + Azure Blob + MinIO”三 Provider 要求，与 [ADR-004 IPersistenceProvider](./adr/ADR-004-data-store-provider-switchable-ef-core.md) 同构；详见 [ADR-015 文件存储：Provider 可切换](./adr/ADR-015-object-storage-provider-switchable.md)。抽象接口名 `IFileStorageProvider`（与 `IPersistenceProvider` / `ICacheProvider` 保持 `*Provider` 后缀一致）。

- **决策日期**：

  > 2026-05-08

- **决策人**：

  > Inkwell Owner

- **卡点等级**：closed
- **回写**：ADR-015 新增；tech-selection.md §15 新增文件存储选型（抽象名变为 IFileStorageProvider）；architecture.md §8 文件存储方案重写；[ADR-005](./adr/ADR-005-deployment-docker-compose-aks.md) Compose 默认 minio + override azurite；[ADR-009](./adr/ADR-009-multimodal-azure-speech.md) 多模态文件路径改引用 IFileStorageProvider。

---

## OQ-A006 模型与外部 API 凭据存储

- **问题**：Azure OpenAI / Azure Speech / 其他模型厂商 API key 如何存储？
- **为什么需要答**：决定 [Inkwell.Models / Inkwell.Multimodal](../01-requirements/repo-impact-map.md) 的配置加载方式；NFR-006 合规相关。
- **影响范围**：REQ-005 / REQ-016；NFR-006；ADR-005。
- **候选答**：
  - **A**（Agent 默认推进）。Azure Key Vault（prod） + ASP.NET Core User Secrets（dev）；通过 [Microsoft.Extensions.Configuration.AzureKeyVault](https://learn.microsoft.com/azure/key-vault/secrets/quick-create-net) 注入。后果：与 AKS Managed Identity 配套；dev 不写明文配置文件；NFR-006 凭据不出 Azure 边界。
  - **B**. 环境变量 + Docker Compose `.env` / Kubernetes Secret。后果：实现最简单；机密以 Kubernetes Secret base64 存储，不如 KeyVault 防泄漏；轮换需要 Pod 重启。
  - **C**. 其他（请显式列出）。
- **回答**：

  > 2026-05-09。接受 B。Owner 反转默认值，v1 凭据走环境变量 + Docker Compose `.env` / Kubernetes Secret，不引入 Azure Key Vault。限制与补偿措施：env 文件不进仓库（`.gitignore`）；K8s Secret 启用 [静态加密](https://kubernetes.io/docs/tasks/administer-cluster/encrypt-data/)；RBAC 限制 Pod 启动事件不出现 Secret 原文；轮换以 Pod 重启代价接受。[risk-analysis.md RISK-013](./risk-analysis.md) 显式记录“未使用 KeyVault”的残余风险；v2 评估升级为 Key Vault + CSI driver。

- **决策日期**：

  > 2026-05-09

- **决策人**：

  > Inkwell Owner

- **卡点等级**：closed
- **回写**：→ [tech-selection.md §18 配置与凭据](./tech-selection.md)；[architecture.md §13 安全设计](./architecture.md)；[ADR-005 §后果](./adr/ADR-005-deployment-docker-compose-aks.md)；[risk-analysis.md RISK-013](./risk-analysis.md) 新增。

---

## OQ-A007 测试与 CI 工具链

- **问题**：测试与 CI 平台选型：(a) xUnit（后端）+ Vitest（前端）+ Playwright（E2E）+ GitHub Actions，(b) NUnit + Jest + Cypress + Azure DevOps，(c) 其他组合。
- **为什么需要答**：决定 H4 测试用例编写时的命名 / 断言风格；影响 [.github/copilot-instructions.md](../../.github/copilot-instructions.md) 中 `dotnet test` 的实际执行链路。
- **影响范围**：H4 全部测试用例；H5 编码任务的 Verify 命令格式；CI 流水线。
- **候选答**：
  - **A**（Agent 默认推进，Owner 修正后部分采纳）。后端 [MSTest 微软系](https://github.com/microsoft/testfx)（[`MSTest.Sdk`](https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-sdk) 4.x 最新稳定 + [`Microsoft.Testing.Platform`](https://learn.microsoft.com/dotnet/core/testing/unit-testing-platform-intro) / MTP runner） + Vitest + Playwright + GitHub Actions。后果：与 .NET 10 + C# 14 默认生态一致；`MSTest.Sdk` 提供零配置项目模板；Microsoft.Testing.Platform 带来冷启动提速；与 GitHub Actions matrix 原生兼容。
  - **B**. NUnit + Jest + Cypress + Azure DevOps。后果：脱离 GitHub 平台；Jest 与 Vite 集成需要 babel-jest；Cypress 不支持多 tab E2E。
  - **C**. 其他（请显式列出）。
- **回答**：

  > 2026-05-09。接受 A（修正后）。Owner 明确：后端从 xUnit 换为 [MSTest v3](https://github.com/microsoft/testfx) 最新稳定版（`MSTest.Sdk` + `Microsoft.Testing.Platform`），代表微软官方当前推荐的实践；前端保留 Vitest；E2E 保留 Playwright；CI 平台保留 GitHub Actions。

- **决策日期**：

  > 2026-05-09

- **决策人**：

  > Inkwell Owner

- **卡点等级**：closed
- **回写**：→ [tech-selection.md §19 测试与 CI](./tech-selection.md)；[architecture.md §10 可观测性方案](./architecture.md)。

> **2026-05-10 措辞勘误**：上文 Agent 作为背景考调及「MSTest v3」是测试框架本身的版本号（[`MSTest.TestFramework` 3.x](https://github.com/microsoft/testfx)），与本项目实际引入的 [MSBuild SDK 版本](https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-sdk) `MSTest.Sdk` 4.x（最新稳定 4.2.2，默认使用 [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/unit-testing-platform-intro) / MTP runner）是上游项目两个错位的发布通道。该 OQ 决议指向 MSTest 微软系本身仍锁定，措辞仅由“MSTest v3”精化为“[MSTest.Sdk 4.x](https://github.com/microsoft/testfx)”；Owner 回答字段保留原文。同步→ [tech-selection.md §18](./tech-selection.md) / [AGENTS.md §2.4](../../AGENTS.md) / [2026-05-10-h2-architecture-review §9](../07-reviews/2026-05-10-h2-architecture-review.md)。

---

## OQ-A008 RedisStreamQueueProvider v1 是否实现

- **问题**：[ADR-018](./adr/ADR-018-queue-abstraction-channels-default.md) 已决议在 [Inkwell.Abstractions](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) 预留 `IQueueProvider` 接口 + [Inkwell.Core](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) 提供 `ChannelsQueueProvider` 默认实现。是否在 v1 同期实现独立的 `RedisStreamQueueProvider`（依赖 [Redis Streams](https://redis.io/docs/latest/develop/data-types/streams/)）还未决。
- **为什么需要答**：Redis 可靠队列与 Channels in-process 队列在可靠性语义上差异明显（crash 后任务是否丢失 / 多副本 worker 能否各自拍任务）。是否 v1 出 csproj 决定了 [Inkwell.Queue.Redis](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) 是否需创建、H4 测试是否需覆盖 crash recovery / fairness 用例、微服务多副本部署能否启用。Owner 在 ADR-018 取舍过程中未提供具体 v1 触发场景，由本 OQ 显式入账。
- **影响范围**：[ADR-017](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) `src/core/providers/` 拓扑（是否多一个 `Inkwell.Queue.Redis/` csproj）；[ADR-018](./adr/ADR-018-queue-abstraction-channels-default.md) 「接受的代价」边界；[RISK-014](./risk-analysis.md) 是否激活；H4 需要补鱼类用例的范围；[Inkwell.Triggers](../01-requirements/repo-impact-map.md) / [Inkwell.Orchestrations](../01-requirements/repo-impact-map.md) 是否能依赖「多副本 不 抢 同一任务」语义。
- **候选答**：
  - **A**（Agent 默认推进）。**v1 不实现** `RedisStreamQueueProvider`，仅保留接口 + `ChannelsQueueProvider`。后果：ADR-018 不需重写；v1 运行时代价与当前等价；v1 范围不膊胀（[OQ-006 closed §A](../01-requirements/open-questions.md) 范围风险不加重）；HPA 开多副本时，需明确警告「同一任务可能被多副本重复拍」——该条限制进 [Inkwell.Triggers](../01-requirements/repo-impact-map.md) / [Inkwell.Orchestrations](../01-requirements/repo-impact-map.md) 的 H3 详细设计。
  - **B**。**v1 同期实现** `RedisStreamQueueProvider`（新增 `src/core/providers/Inkwell.Queue.Redis/` csproj）。**前置条件（Owner 必须提供）**：(a) 明确 v1 触发场景（哪个 REQ / 哪个任务路径 / 哪项 SLA 在 in-process 队列下满足不了）；(b) dead-letter / retry / fairness 策略；(c) observability 指标清单。后果：RISK-014 激活；H4 补鱼 crash recovery / fairness / DLQ 用例；v1 范围膊胀。
  - **C**。**现在不实现，但预起始化脚手架**（在 [Inkwell.Abstractions](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) 内增补可靠性能力接口预留，如 `IDurableQueueProvider : IQueueProvider`）。后果：ADR-018 需微调 「接受的代价」；与选项 A 相比只多一个 marker 接口，运行时代价 ≈ 0。
  - **D**。其他（请显式列出）。
- **回答**：

  > 2026-05-10。接受 **B**：v1 同期实现 `RedisStreamQueueProvider`（新增 `src/core/providers/Inkwell.Queue.Redis/` csproj）。三前置条件：
  >
  > - **(a) v1 触发场景**：环境对称论据——`ChannelsQueueProvider` = 开发态默认（InMemory / 单进程 / 零依赖）；`RedisStreamQueueProvider` = 集成测试 + prod 默认（与 [`IPersistenceProvider`](./adr/ADR-004-data-store-provider-switchable-ef-core.md) / [`ICacheProvider`](./adr/ADR-016-cache-provider-redis.md) / [`IFileStorageProvider`](./adr/ADR-015-object-storage-provider-switchable.md) 的「InMemory dev · 真 Provider prod」拓扑对齐）。以此避免「开发期靠 Channels 跳过可靠性设计、上线才发现多副本抢同一任务」这类环境偏移 bug。
  > - **(b) dead-letter / retry / fairness 策略**：DLQ 默认 N=3 + 24h 保留期。其余依赖 [Redis Streams 内置语义](https://redis.io/docs/latest/develop/data-types/streams/)：fairness 由 [`XREADGROUP`](https://redis.io/docs/latest/commands/xreadgroup/) consumer group 多副本公平分发；crash recovery 由 PEL（pending entries list）+ [`XCLAIM`](https://redis.io/docs/latest/commands/xclaim/) visibility timeout = 5 min 重插；retry 策略 = 指数退避 + jitter（1s 起 / max 60s）。详细实现进 [Inkwell.Queue.Redis](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) 的 H3 详细设计。
  > - **(c) observability 指标清单**：v1 必发 `queue_depth`（活动 stream 长度）。其余 `queue_consume_latency_p95` / `queue_dlq_count` / `queue_consumer_lag` / `queue_redelivery_count` / `queue_consumer_active` 均进 [RISK-014](./risk-analysis.md) “prod 上线前补齐”残余风险，H4 验收门禁 = `queue_depth` + `queue_dlq_count` 两项（DLQ 计数 H4 必须能报警，否则无法验收选项 (b) DLQ 默认 N=3）。

- **决策日期**：

  > 2026-05-10

- **决策人**：

  > Inkwell Owner

- **卡点等级**：closed
- **回写**：→ [ADR-017 §决策 · csproj 表](./adr/ADR-017-backend-module-topology-ports-and-adapters.md) 新增 `Inkwell.Queue.Redis` 行；[ADR-018 §决议](./adr/ADR-018-queue-abstraction-channels-default.md) 「接受的代价」重写为「开发态 / 集成测试 + prod 双 Provider」；[RISK-014](./risk-analysis.md) 从「占位」激活。

---

> **完成后下一步**：
>
> 1. OQ-A001 ~ OQ-A008 均已 closed（2026-05-08 初谈 + 2026-05-09 复议 + 2026-05-10 随 ADR-017 / ADR-018 补录）。
> 2. 评审纪要走 `/log-review`（见 [.github/copilot-instructions.md §3](../../.github/copilot-instructions.md)），同时人工确认 [risk-analysis.md](./risk-analysis.md) / [tech-selection.md](./tech-selection.md) / [architecture.md](./architecture.md) `updated: 2026-05-10`（status 保持 reviewed）；ADR-017 / ADR-018 本轮已随 Owner 选 B + AGENTS.md 一次性授权一同翻转为 `accepted`。
> 3. H3 详细设计需接重点足迹：四 Provider 抽象接口 contract（[`IPersistenceProvider`](./adr/ADR-004-data-store-provider-switchable-ef-core.md) / [`IFileStorageProvider`](./adr/ADR-015-object-storage-provider-switchable.md) / [`ICacheProvider`](./adr/ADR-016-cache-provider-redis.md) / [`IQueueProvider`](./adr/ADR-018-queue-abstraction-channels-default.md)）、主进程长 SSE 在 macOS App Nap / Windows 节能模式下的保活策略、凭据轮换与 Pod 重启联动、[`Inkwell.Core.AgentRuntime` 命名空间 的 MAF 隔离 lint 规则](./adr/ADR-017-backend-module-topology-ports-and-adapters.md)、[`RedisStreamQueueProvider` H3 详设](./adr/ADR-018-queue-abstraction-channels-default.md)（DLQ N=3 24h + Redis Streams 内置语义 + queue_depth metric）。
