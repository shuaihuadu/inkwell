---
id: ADR-004-data-store-provider-switchable-ef-core
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
  - OQ-A001
downstream:
  - ADR-005
  - ADR-020
  - ADR-021
---

# ADR-004 数据存储：EF Core Provider 可切换 + Qdrant 向量库

> **2026-05-10 增量更新**：本 ADR §决策 line 43 “`Inkwell.VectorStore` 模块封装” 表述已被 [ADR-020 向量存储抽象](./ADR-020-vector-store-microsoft-extensions-vectordata.md) 精化（refinement，不 supersede）为：复用 [Microsoft.Extensions.VectorData](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data)；Qdrant 实现抽到 `providers/VectorStore/Inkwell.VectorStore.Qdrant/`；`Inkwell.Core/` 加 `InMemoryVectorStore` 默认实现（与 [ADR-018](./ADR-018-queue-abstraction-channels-default.md) 环境对称原则一致）。csproj 11 → 12。
>
> **2026-05-10 增量更新·第二轮**：本 ADR §决策 line 39 “三 Provider 实现” 表述已被 [ADR-021 EFCore Persistence 共享层](./ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 精化（refinement，不 supersede）为：EFCore family = 4 csproj（`Inkwell.Persistence.EFCore` base + InMemory / SqlServer / Postgres 三 final adapter）。Entity / `OnModelCreating` / `EfCorePersistenceProvider` / DataSeed 集中在 base；Migration SQL 文本为 Provider-specific，在 SqlServer / Postgres final adapter 各自 `Migrations/`；InMemory 不支持 Migration 仅走 `EnsureCreated`。csproj 12 → 13。
>
> **2026-07-08 增量更新·第三轮（取代上述两条中的 InMemory 部分）**：Owner 拍板：[`Microsoft.EntityFrameworkCore.InMemory`](https://learn.microsoft.com/ef/core/providers/in-memory/) 不支持外键约束等关系完整性行为，对本地开发 / 单测价值有限，**不再作为 v1 Provider**。关系数据 Provider 从三个收敛为**两个**（SQL Server 2025 / PostgreSQL 17）；本地开发与单元 / 集成测试改用 [Testcontainers](https://testcontainers.com/) 起真实 SqlServer / Postgres 实例，不再依赖 InMemory 进行快速无容器测试。csproj 总数相应减少（详见 [ADR-021](./ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 同日更新）。本条下方正文已直接删除 InMemory 相关描述，不再保留历史遗迹文字。
>
> **2026-07-14 增量更新·第四轮**：Owner 决定将 PostgreSQL 支持基线从 17 升级为 **18**，当前 dev / Testcontainers 固定到最新稳定补丁 `18.4`。Npgsql EF Core Provider 10 已支持 PostgreSQL 18；AppHost 不挂载持久卷，测试容器均使用临时实例，因此当前开发阶段不涉及已有数据目录升级。生产部署若已有 PostgreSQL 17 数据，必须通过 `pg_upgrade` 或 dump/restore 完成 major upgrade，不能直接复用 17 的数据目录。

## 上下文

[Q-A4](../open-questions-arch.md) 用户答"v1 支持 SQL Server / PostgreSQL 切换"——意图是让 Inkwell 在不同部署环境下都能起得来（企业内部 IT 锁 SQL Server 的客户用 SQL Server，开源 / 云原生客户用 PostgreSQL）。本地开发与单元 / 集成测试统一走 [Testcontainers](https://testcontainers.com/) 起真实实例，不引入 InMemory Provider（[`Microsoft.EntityFrameworkCore.InMemory`](https://learn.microsoft.com/ef/core/providers/in-memory/) 不支持外键约束，对本地开发价值有限，2026-07-08 Owner 拍板删除）。"可切换"边界在 [Q-A4-followup / OQ-A001](../open-questions-arch.md) 中给出三条：

- A. 仅关系数据 EF Core Provider 切换；向量库另选独立服务
- B. 两种引擎都支持向量检索（SQL Server 2025 vector / pgvector）
- C. v1 仅 PostgreSQL + pgvector，SQL Server 仅占位

授权"按建议默认值推进"，本 ADR 落 A。

[REQ-009 知识库](../../01-requirements/requirements.md) 与 [REQ-010 长期记忆](../../01-requirements/requirements.md) 都需要向量检索；[REQ-014 trace](../../01-requirements/requirements.md) 是高写入但不需向量检索；[NFR-005 对话历史](../../01-requirements/requirements.md) 是关系存储。

## 决策

**关系数据：EF Core 10（与 .NET 10 同步发布）+ `IPersistenceProvider` 抽象 + 两 Provider 实现（SQL Server 2025 / PostgreSQL 18）；向量数据：[Qdrant 1.x](https://qdrant.tech/) 独立服务。**

> 上述“两 Provider 实现”的 csproj 物理布局由 [ADR-021](./ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) 锁定：EFCore family = 3 csproj（`Inkwell.Persistence.EFCore` base + SqlServer / Postgres 两 final adapter）。

- **抽象名与命名**：后端代码仅依赖 [`IPersistenceProvider`](../../01-requirements/repo-impact-map.md)（同 [`IFileStorageProvider`](./ADR-015-object-storage-provider-switchable.md) / [`ICacheProvider`](./ADR-016-cache-provider-redis.md) 保持 `*Provider` 后缀一致），其下封装 `InkwellDbContext`（继承 `DbContext`）与 Migration 运行时。业务代码绝对不出现 Provider 特定 API。
- **建模路径**：采用 [Code First](https://learn.microsoft.com/ef/core/managing-schemas/migrations/) + [EF Migration](https://learn.microsoft.com/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli)；表结构以 C# 实体为唯一源，不手写 SQL DDL。
- **启动注入**：通过 `appsettings.json` 的 `Inkwell:Persistence:Provider` 字段（值域 `SqlServer` / `PostgreSQL`）选择 Provider。
- **Migration**：每种 Provider 一份 `Migrations/<Provider>/` 子目录；CI 跑两套迁移测试。
- **向量库**：通过 `Inkwell.VectorStore` 模块封装 Qdrant gRPC SDK，与 EF 解耦；[Inkwell.KnowledgeBase / Inkwell.Memory](../../01-requirements/repo-impact-map.md) 的存储路径分两段（关系字段 → EF Core；embedding → Qdrant）。具体抽象来源 / csproj 布局 / 多 Provider 矩阵由 [ADR-020](./ADR-020-vector-store-microsoft-extensions-vectordata.md) 锁定。
- **向量 ID 与关系 ID** 在应用层通过 `Guid` 关联，不在 DB 层做 FK。

## 备选项

### 备选 A（Q-A4-followup B）：两引擎都支持向量

- **放弃理由**：(1) 实现成本与 [OQ-006 closed §A](../../01-requirements/open-questions.md) 范围风险冲突——SQL Server 2025 vector / pgvector 两套实现 + 切换抽象需要做"最小公倍数"语义集；(2) SQL Server 2025 vector 当前 GA 不久，[向量索引能力与 pgvector 不完全对等](https://learn.microsoft.com/sql/relational-databases/json/vector-data-type)；(3) 切换抽象漏出风险——查询性能在不同引擎下差距数量级，UI 层无法屏蔽。

### 备选 B（Q-A4-followup C）：v1 仅 PostgreSQL + pgvector

- **放弃理由**：(1) 与 [Q-A4](../open-questions-arch.md) 答案"SQL Server / PostgreSQL 切换"不一致；(2) 把"v1 占位"包装为"切换"是话术游戏，不诚实；(3) 后续切到 SQL Server 时会暴露大量没测过的 SQL/EF 边界问题。

### 备选 C：直接锁 PostgreSQL + pgvector（不做切换）

- **放弃理由**：与 Q-A4 用户决策直接冲突——用户明确不希望 v1 锁单一引擎。

### 备选 D：用 Azure AI Search 替代 Qdrant

- **放弃理由**：(1) Azure AI Search 与 [Q-A8](../open-questions-arch.md) "不锁 Azure telemetry"的方向不一致——dev 环境（Compose）跑不起 Azure AI Search emulator；(2) 月租成本远高于自托管 Qdrant；(3) Qdrant 的 cross-environment 一致部署（dev Compose + prod AKS）路径更短。

## 后果

### 正面

- 与 [Q-A4](../open-questions-arch.md) 决策完全对齐。
- Qdrant 与 EF 解耦让向量索引可以独立扩缩容，不会因为关系库切换而崩溃。
- 两种 Provider 的 Migrations 独立维护，dev / 单测通过 Testcontainers 起真实实例，与 prod 行为一致，避免"InMemory 能跑但真实 Provider 下失败"的环境漂移风险。
- 应用代码层引用 `InkwellDbContext` 与 `IVectorStore` 两个抽象，H3 详细设计的边界清晰。

### 负面

- Qdrant 引入额外运维项（Compose 一个 service / AKS 一个 StatefulSet）；通过 [ADR-005](./ADR-005-deployment-docker-compose-aks.md) 统一部署模板缓解。
- 两种 Provider 切换抽象的"最小公倍数"约束 → EF Core 中的 Provider-specific 特性（如 PostgreSQL `JSONB` 操作符 / SQL Server `OUTPUT` 子句）不能用；通过把这类特性用到的查询封装在 Provider-specific Repository 中（每种 Provider 一份）缓解。**这是关键 trade-off**，详见 [RISK-002](../risk-analysis.md)。
- Migration 两套维护成本：建议 H5 第一个 TASK 把"两 Provider 迁移测试"加进 CI 而不是手动同步。

### 中性

- pgvector 在 PostgreSQL Provider 下不可用（因为关系层抽象不允许 Provider-specific 类型），即使部署在 PostgreSQL 上 pgvector 扩展也只是冗余安装。

## 状态

- **状态**：accepted（前提：[OQ-A001](../open-questions-arch.md) 接受默认值 A）
- **首次发布**：2026-05-08
- **关联**：supersedes 无；上游 [ADR-002](./ADR-002-backend-runtime-dotnet10-aspnetcore.md) / [OQ-A001](../open-questions-arch.md)；下游 [ADR-005](./ADR-005-deployment-docker-compose-aks.md)
- **置信度**：medium（依赖用户接受 OQ-A001 默认值；切换抽象漏出风险需 H3 / H5 验证 → RISK-002）
