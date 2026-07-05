---
id: ADR-020-vector-store-microsoft-extensions-vectordata
stage: H2
status: accepted
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [Inkwell Owner]
created: 2026-05-10
updated: 2026-05-10
upstream:
  - REQ-009
  - REQ-010
  - ADR-004
  - ADR-017
downstream: []
---

# ADR-020 向量存储抽象：复用 Microsoft.Extensions.VectorData + 双 Provider（Qdrant / InMemory）

## 上下文

[ADR-004 §决策](./ADR-004-data-store-provider-switchable-ef-core.md) 锁定「关系数据走 EF Core 三 Provider；向量数据走 Qdrant 独立服务，通过 `Inkwell.VectorStore` 模块封装」，但**未锁定**：

1. **抽象来源**：Inkwell 是否要重新发明 `IVectorStore<T>`，还是复用 [Microsoft.Extensions.VectorData.Abstractions](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data) 已有的 `VectorStore` / `VectorStoreCollection<TKey, TRecord>`。
2. **csproj 物理布局**：Qdrant 实现位置——是放 `Inkwell.Core/`（与 [ADR-015 IFileStorageProvider](./ADR-015-object-storage-provider-switchable.md) / [ADR-016 ICacheProvider](./ADR-016-cache-provider-redis.md) / [ADR-018 IQueueProvider](./ADR-018-queue-abstraction-channels-default.md) 的 SDK-bound 实现都在 `providers/*` 的拓扑不对称），还是抽到 `providers/`。
3. **多 Provider 矩阵**：v1 是否给 InMemory / Azure AI Search / Pinecone 留位。
4. **接口粒度**：[REQ-009 知识库](../../01-requirements/requirements.md) 与 [REQ-010 长期记忆](../../01-requirements/requirements.md) 都需向量检索，是否共用同一个抽象。

[ADR-017 Ports & Adapters](./ADR-017-backend-module-topology-ports-and-adapters.md) 锁定的环境对称原则要求：dev / unit test 必须有 in-process 替代品，否则 CI 必须起容器才能跑（[ADR-018 §决策 环境对称论据](./ADR-018-queue-abstraction-channels-default.md) 已为队列家族确立此原则）。本 ADR 把同一原则延伸到向量库家族。

Owner 在本 ADR 起草会话中通过 picker 拍板：D1 = A（复用 M.E.VectorData）；D2 = C（providers/* + Inkwell.Core/ 加 InMemoryVectorStore）；D3 = B（v1 = Qdrant + InMemory）；D4 = A（单一接口 + KB / Memory 各自 Service 层包业务语义）。

## 决策

**Inkwell 不重新发明向量抽象**——`IVectorStore` / `VectorStoreCollection<TKey, TRecord>` 直接复用 [`Microsoft.Extensions.VectorData.Abstractions`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data)；`Inkwell.Abstractions` 仅提供 Builder DSL 扩展方法（`UseQdrantVectorStore(...)` / `UseInMemoryVectorStore(...)`）+ Inkwell 特定的 `VectorStoreOptions`。

物理布局符合环境对称原则：

```text
src/core/
├── Inkwell.Abstractions/
│   └── (type-alias re-export Microsoft.Extensions.VectorData 主要类型；Builder DSL 扩展方法)
├── Inkwell.Core/
│   └── VectorStore/
│       └── InMemoryVectorStore   (基于 Microsoft.Extensions.VectorData.InMemory connector)
└── providers/
    └── Inkwell.VectorStore.Qdrant/   (基于 Microsoft.Extensions.VectorData.Qdrant connector + 配置 Builder)
```

后端总 csproj 数 [ADR-019](./ADR-019-process-topology-webapi-worker-split.md) 锁定的 11 → 12（`providers/` 7 → 8）。

### 接口粒度（D4 = A）

- **底层抽象**：`Microsoft.Extensions.VectorData.VectorStore` + `VectorStoreCollection<TKey, TRecord>`，由 KB / Memory 业务层共用。
- **业务语义**：Chunking / RetentionPolicy / Eviction / TTL 等业务约束**不进**向量抽象，而由 `Inkwell.Core.KnowledgeBase` 与 `Inkwell.Core.Memory` 各自 Service 层包封装（`IKnowledgeBaseService` / `IMemoryService` 出现在业务命名空间，**不**进 `Inkwell.Abstractions`）。
- **Collection 命名**：H3 详细设计落地，本 ADR 不锁；建议默认 KB 一个 collection per knowledge-base、Memory 一个 collection per agent，均带 `tenant_id` payload 字段隔离（v1 单租户但保接口形状）。

### Schema 生命周期（D6 由 D1 强约束）

复用 M.E.VectorData 的 attribute model：[`[VectorStoreKey]`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.vectordata.vectorstorekeyattribute) / [`[VectorStoreData]`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.vectordata.vectorstoredataattribute) / [`[VectorStoreVector]`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.vectordata.vectorstorevectorattribute)。schema 是 code-first，与 EF Core Migration 风格一致。**不引入** appsettings.json 中的 schema 字段。

### Embedding 生成（D5 由 D1 强约束）

[`Microsoft.Extensions.VectorData`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data) 设计上**不内嵌 embedding 生成**，调用方通过 [`Microsoft.Extensions.AI.IEmbeddingGenerator<TInput, TEmbedding>`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.ai.iembeddinggenerator-2) 完成。Inkwell 在 `Inkwell.Abstractions` 暴露 Builder DSL `UseAzureOpenAIEmbeddings(...)` 注册 `IEmbeddingGenerator<string, Embedding<float>>`；KB / Memory Service 层注入该接口生成向量后写入 `VectorStoreCollection<TKey, TRecord>`。

### Builder DSL 形状（H3 锁定具体签名）

```csharp
builder.Services.AddInkwell()
    .UseSqlServer(...)
    .UseAzureBlob(...)
    .UseRedis(...)
    .UseRedisQueue(...)
    .UseAzureOpenAIEmbeddings(opts => { /* deployment / endpoint / model = text-embedding-3-large */ })
    .UseQdrantVectorStore(opts => { /* host / port / api-key / use-https */ })   // prod
    // 或在 dev / unit test：.UseInMemoryVectorStore()
    .Build();
```

H3 [HD-001 Inkwell.Abstractions] 必须锁定：(1) `VectorStoreOptions` 字段；(2) `UseQdrantVectorStore` / `UseInMemoryVectorStore` 扩展方法签名；(3) 与 `IEmbeddingGenerator` 注册的衔接顺序。

### 多 Provider 矩阵（D3 = B）

- **v1**：Qdrant（prod / integration test）+ InMemory（dev / unit test）
- **未来**：Azure AI Search / Pinecone / Weaviate 等走未来 ADR；本 ADR**不预留占位 csproj**

## 备选项

### 备选 A（D1 = B）：Inkwell 自定义 `IVectorStore<T>`，内部 Adapter 包装 M.E.VectorData

- **理由**：与 `IPersistenceProvider` 包装 EF Core 的做法对称；可在 Inkwell 边界完全屏蔽上游变化
- **放弃理由**：(1) M.E.VectorData 已经是上游标准抽象（Semantic Kernel 与 Microsoft.Agents.AI 全部 connector 已遵此抽象），再包一层 = 平移类型签名 + 维护映射代码无业务价值；(2) 上游引入 hybrid search / filter 新语义时，Inkwell 自定义接口会跟不上；(3) [Inkwell.Core.AgentRuntime 命名空间 ADR-017](./ADR-017-backend-module-topology-ports-and-adapters.md) 已经有"复用 MAF 抽象不重复发明"的先例

### 备选 B（D1 = C）：完全自定义 `IVectorStore<T>` + 直接调 Qdrant gRPC SDK

- **放弃理由**：(1) 与 [ADR-003 Microsoft Agent Framework](./ADR-003-agent-engine-microsoft-agent-framework.md) 生态严重不对齐——MAF Skill / RAG 模板都基于 M.E.VectorData，Inkwell 自走一套会让 H3 detail 必须为每个 RAG 集成点写适配代码；(2) Qdrant gRPC SDK 是底层细节，业务代码必须永远绕路；(3) v2 想接 Azure AI Search 时需要重写而非加 connector

### 备选 C（D2 = B）：维持 Qdrant 实现在 `Inkwell.Core/`（ADR-004 现状）

- **放弃理由**：(1) 与其他四 Provider 家族（Persistence / FileStorage / Cache / Queue）拓扑不对称——H3 author 反复需要为「为什么 vector 不在 providers/」解释；(2) Qdrant SDK 是 SDK-bound 重依赖（gRPC + protobuf），在 `Inkwell.Core` 会让客户端只想跑 unit test 也被迫拉 Qdrant 依赖；(3) 阻碍未来 Azure AI Search / Pinecone connector 进入 providers/ 的对称添加路径

### 备选 D（D3 = A）：v1 仅 Qdrant，不交付 InMemoryVectorStore

- **放弃理由**：(1) 与 [ADR-018 §决策 环境对称论据](./ADR-018-queue-abstraction-channels-default.md) 不一致——同样的论据（dev 靠 in-process 跳过设计 / 上线才发现可用性 bug）适用于向量；(2) unit test 必须起 Qdrant 容器才能跑 KB / Memory 测试，CI 时间膨胀；(3) M.E.VectorData InMemory connector 是 NuGet 现成包，纳入零开发成本

### 备选 E（D3 = C）：v1 一次开 Qdrant + InMemory + Azure AI Search

- **放弃理由**：(1) 与 [ADR-004 备选 D 放弃理由](./ADR-004-data-store-provider-switchable-ef-core.md) 一致——dev 跑不起 Azure AI Search emulator；(2) v1 [NFR-001](../../01-requirements/requirements.md) 用户量级（≤ 30 同时在线）不需要多向量库 Provider；(3) 占位 csproj 是技术债务来源（无 caller 但占维护成本）

### 备选 F（D4 = B）：上层 `IKnowledgeBaseStore` / `IMemoryStore` + 底层 `IVectorStore`

- **放弃理由**：(1) 业务语义（chunking / retention / eviction）属于 Service 层而非 Repository 层，强行加业务接口在 `Inkwell.Abstractions` 会污染端口层纯度（[ADR-017 §依赖规则](./ADR-017-backend-module-topology-ports-and-adapters.md)）；(2) 增加一层抽象但不增加表达能力；(3) KB / Memory 详细 schema 在 H3 设计前过早抽象

### 备选 G（D4 = C）：完全独立的两个抽象 `IKnowledgeBaseStore` / `IMemoryStore`，各自可接同 / 不同 Provider

- **放弃理由**：(1) 违反 [ADR-017 §决策 三 Provider 抽象家族](./ADR-017-backend-module-topology-ports-and-adapters.md) DRY 原则——KB 与 Memory 在向量层面是同一回事（向量 + payload）；(2) 让运维需要分别配置两个 Qdrant 集群；(3) v1 [NFR-001](../../01-requirements/requirements.md) 量级下零业务价值

## 后果

### 正面

- 与 MAF / Semantic Kernel 生态完全对齐——RAG / Skill 模板代码可直接落地
- 复用 [`[VectorStoreKey]` / `[VectorStoreData]` / `[VectorStoreVector]`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data) attribute model，schema 是 code-first 与 EF Migration 风格一致
- 与四 Provider 家族（Persistence / FileStorage / Cache / Queue）物理布局对称——`providers/Inkwell.VectorStore.Qdrant/` + `Inkwell.Core/` 默认 InMemory 实现
- unit test 走 InMemory 不依赖容器；integration test / prod 走 Qdrant
- `IEmbeddingGenerator` 与 `VectorStoreCollection` 解耦——embedding 模型升级（如 `text-embedding-3-large` → 下一代）不会触发向量库 schema 变更

### 负面

- M.E.VectorData 类型签名（`VectorStoreCollection<TKey, TRecord>`）会出现在 `Inkwell.Abstractions` 公共 API 中——上游 NuGet 包升级时 Inkwell consumer 会感知 breaking change（[RISK-016](../risk-analysis.md)）
- InMemoryVectorStore 与 Qdrant 语义子集——hybrid search / geo filter / advanced indexing 等高级特性可能 InMemory 不支持，开发态测过 InMemory 在 prod 跑 Qdrant 才出 surprise（[RISK-016](../risk-analysis.md)）
- `tests/core/Inkwell.Providers.Contract` 包必须扩展加 vector matrix（与 [RISK-011 三 FileStorage Provider contract 漏出](../risk-analysis.md) 同构）
- csproj 数 11 → 12（开发体感与 ADR-018 / ADR-019 累积）

### 中性

- v1 单租户但 Collection 命名约定中保 `tenant_id` payload，未来开多租户零迁移
- KB / Memory Service 层的业务语义（chunking / retention）在 H3 仍然要写——本 ADR 只解放底层抽象选择，不减少 Service 层工作量

## 迁移路径

**breaking change 标记**：是（以 ADR-004 §决策 line 43 「`Inkwell.VectorStore` 模块封装」表述为基线计算 diff，本 ADR 把该模糊表述细化为具体 csproj + Provider + 抽象来源）。

| 步骤 | 文件 | 改动 | 是否需翻 status |
| ---- | ---- | ---- | ---------------- |
| 1 | [`ADR-004` §决策 line 43-44 + 状态段](./ADR-004-data-store-provider-switchable-ef-core.md) | refinement note 引用 ADR-020；状态接力 | 内部增量，仍 accepted |
| 2 | [`adr/README.md`](./README.md) | 索引表 +ADR-020 行；依赖树 ADR-004 子节点 +ADR-020 | draft（编辑者签字位） |
| 3 | [`architecture.md` §1 + §3.1 + §3.2 + §3.3](../architecture.md) | csproj 树 providers/ 加 `Inkwell.VectorStore.Qdrant/`；`Inkwell.Core/VectorStore/InMemoryVectorStore`；§3.3 IVectorStore 默认实现位置改 providers/ | reviewed（incremental update） |
| 4 | [`tech-selection.md` §0 + §4 + §22](../tech-selection.md) | §0 摘要表「关系 + 向量数据」行的关联决策 +ADR-020；§4 段加 refinement note；§22 自检 ADR 数 19 → 20 | reviewed（incremental update） |
| 5 | [`risk-analysis.md`](../risk-analysis.md) | §0 摘要表 +RISK-016 行；新增 §RISK-016 全文；§1 自检 15 → 16 风险 | reviewed（incremental update） |
| 6 | [`AGENTS.md` §3.1 + §4](../../../AGENTS.md) | §3.1 providers/ 加 `Inkwell.VectorStore.Qdrant/`；`Inkwell.Core/` 加 InMemoryVectorStore；§4 ADR 数 19 → 20 | 签字位 ⚠️（需人工授权） |
| 7 | [HD-001 Inkwell.Abstractions](../../04-detailed-design/) | 扩展 Builder DSL 范围：`UseQdrantVectorStore` / `UseInMemoryVectorStore` / `UseAzureOpenAIEmbeddings` 三个签名 | H3 起草阶段一并写入 |
| 8 | `tests/core/Inkwell.Providers.Contract` | 扩展加 vector contract 用例（与 KB / Memory H3 详细设计联动）| H4 [TestCaseAuthor] 起草时一并落地 |
| 9 | OTel `service.name = inkwell-webapi` / `inkwell-worker` 双 source（[ADR-019](./ADR-019-process-topology-webapi-worker-split.md)）覆盖 vector 调用埋点；instrumentation 由 M.E.VectorData 内置 ActivitySource 提供 | 无需配置改动，仅在 Grafana Dashboard 加面板 | dashboard JSON 在 H5 工作 |

**自动化检查命令**（落地后用以确认旧表述已清理）：

```bash
# 检查 Inkwell.VectorStore.Qdrant 是否在 providers/ 出现
grep -rn "Inkwell\.VectorStore\.Qdrant" docs/ AGENTS.md

# 检查 ADR-020 是否被 ADR-004 引用
grep -rn "ADR-020" docs/03-architecture/

# 检查残留的"Inkwell.VectorStore 模块封装"模糊表述（若仍存在应改为指向具体 csproj）
grep -rn "Inkwell.VectorStore 模块" docs/
```

## 状态

`accepted` — 2026-05-10。Owner 在本 ADR 起草会话中通过 4 个 picker 拍板：D1 = A（M.E.VectorData 复用）；D2 = C（providers/* + Inkwell.Core/ InMemory）；D3 = B（Qdrant + InMemory 双 Provider）；D4 = A（单一抽象 + KB/Memory Service 层）。本 ADR 是 [ADR-004 §决策 line 43](./ADR-004-data-store-provider-switchable-ef-core.md) "`Inkwell.VectorStore` 模块封装" 表述的精化（refinement），**不 supersede** ADR-004。

## 置信度

`high`。

依据：(1) M.E.VectorData 是 [Microsoft 官方 GA 抽象](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data)，与 [ADR-003 MAF](./ADR-003-agent-engine-microsoft-agent-framework.md) 同生态，零调研风险；(2) Qdrant 与 InMemory 双 connector 都是 NuGet 现成包，集成成本明确；(3) 与 [ADR-017](./ADR-017-backend-module-topology-ports-and-adapters.md) / [ADR-018](./ADR-018-queue-abstraction-channels-default.md) 环境对称原则严格对齐，无新增架构方法论。
