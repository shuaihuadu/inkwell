---
id: tech-selection-custom-agent
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers:
  - name: self-review
    decision: approved
    date: 2026-05-07
created: 2026-05-07
updated: 2026-05-07
upstream:
  - architecture-custom-agent
  - requirements-custom-agent
  - repo-impact-map-custom-agent
downstream: []
---

# 自定义 Agent 功能 · H2 技术选型

> 每条选型按 stages.md 第 5.5 节六字段：**选择 / 为什么选择 / 替代方案 / 放弃替代方案的原因 / 对团队维护能力的影响 / 对成本性能安全交付周期的影响**。每条标 **置信度**：`high` / `medium` / `low`。

## 0. 置信度分布

| 等级 | 数量 | 占比 |
| --- | --- | --- |
| high | 5 | 36% |
| medium | 8 | 57% |
| low | 1 | 7% |
| **合计** | 14 | 100% |

`low` 条目（1 条）：**T-1 联网搜索服务商**（待 [OQ-A-002](./open-questions-arch.md)）；显式标"待澄清后定"，不视作选型缺陷。

> v0.2 更新：原 "MAF + AG-UI 具体 NuGet 包" 因 [OQ-A-003 关闭](./open-questions-arch.md) 由 `low` 升至 `medium`（包已固化为 `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`，但 [Microsoft Learn 标 Preview](https://learn.microsoft.com/en-us/agent-framework/integrations/) 不是 Released，所以不到 high）。

## 1. 前端框架 + 语言 + UI 库

- **选择**：React 18 + TypeScript 5.x（strict）+ Ant Design 5（含 Pro Components）
- **为什么选择**：
  1. 用户偏好 1 硬约束
  2. [ui-spec.md 第 0 节](../01-requirements/ui-spec.md) 已对齐 AntD Pro 模板
  3. AntD 5 提供 Form / Drawer / Modal / Upload / Skeleton / ConfigProvider 全覆盖 ui-spec 的控件清单，无需自研基础组件
- **替代方案**：
  - Vue 3 + Element Plus / Naive UI
  - Svelte / SolidJS（更轻量）
- **放弃替代方案的原因**：
  - Vue：与 AntD 锐定生态错位（Ant Design Vue 维护节奏滞后于 AntD 5）；团队若已会 React 切 Vue 收益小
  - Svelte / Solid：无成熟企业级 UI 库可直接覆盖 ui-spec.md 全控件；自研基础组件交付周期成本不可接受
- **对团队维护能力的影响**：React + TS 是业内招聘最广泛栈之一，新人接手成本低；AntD 文档完善，常见问题 Stack Overflow 高覆盖
- **对成本/性能/安全/交付周期的影响**：成本中（无 license）；性能 medium（AntD 体积 ~600KB gzip，可接受）；安全 high（无已知供应链风险）；交付 fast（主流栈无学习成本）
- **置信度**：high

## 2. 后端框架

- **选择**：ASP.NET Core 9（Minimal API + Controller 混合）
- **为什么选择**：
  1. 用户偏好 2 硬约束
  2. [.github/copilot-instructions.md](../../.github/copilot-instructions.md) `dotnet test / dotnet format` 已锁定 .NET 工具链
  3. .NET 9 LTS 候选；性能优于 .NET 8（HTTP/3、AOT 准备）
- **替代方案**：
  - Node.js + Express / NestJS
  - Python FastAPI
  - Go + Gin / Echo
- **放弃替代方案的原因**：
  - Node / Python / Go：与 [AGENTS.md](../../AGENTS.md) MAF dogfooding 项目身份冲突；MAF 仅 Python / .NET 双语言原生支持，但 .NET 是 Inkwell 已签的栈
- **对团队维护能力的影响**：高；.NET 生态成熟
- **对成本/性能/安全/交付周期的影响**：成本中（无 runtime license）；性能 high（Kestrel ≥ Node 同等场景）；安全 high；交付 fast
- **置信度**：high

## 3. ORM + 数据库（multi-provider M1）

- **选择**：EF Core 9 + 三 provider 同时支持（InMemory / SQL Server / PostgreSQL），运行时按 `appsettings.Persistence.Provider` 切换
- **为什么选择**：
  1. 用户偏好 2 + 用户 Q3 = M1
  2. EF Core 是 .NET 官方 ORM，与 ASP.NET Core 无缝集成
  3. 三 provider 通过 NuGet 包切换：`Microsoft.EntityFrameworkCore.InMemory` / `.SqlServer` / `Npgsql.EntityFrameworkCore.PostgreSQL`
- **替代方案**：
  - Dapper（micro-ORM，手写 SQL，无迁移）
  - 仅锁定一个 provider（M2 / M3）
- **放弃替代方案的原因**：
  - Dapper：与 EF Core 提供的 query filter（DB-004 软删除）+ Migrations + 多 provider 兼容性不匹配；自实现等价能力交付周期不可接受
  - 单 provider：与用户 Q3 = M1 选择冲突
- **对团队维护能力的影响**：medium-high（EF Core 是主流；多 provider 引入"行为差异调试"成本，由 [RISK-003](./risk-analysis.md) 缓解）
- **对成本/性能/安全/交付周期的影响**：
  - 成本：抽象层 + 测试矩阵成本约 H5 阶段额外 15-20 人天（[RISK-003](./risk-analysis.md)）
  - 性能：medium，EF Core 通常比 Dapper 慢 10~20%，本期数据量低不构成瓶颈
  - 安全：high
  - 交付：medium，多迁移目录管理需要 H3 详细设计明确
- **置信度**：medium

## 4. 队列（multi-provider M1）

- **选择**：自研 `IQueue<T>` 抽象 + 两 provider（InMemory `Channel<T>`、Redis Streams via `StackExchange.Redis`）
- **为什么选择**：
  1. 用户 Q3 = M1（同时支持运行时切换）
  2. .NET `Channel<T>` 是单进程内最高性能队列；Redis Streams 提供持久化 + 多消费者
  3. 不引入独立 MQ（RabbitMQ / Kafka）规避运维复杂度
- **替代方案**：
  - RabbitMQ（AMQP）
  - Azure Service Bus / SQS
  - 数据库 Outbox 表轮询
- **放弃替代方案的原因**：
  - RabbitMQ：MVP 规模 ≤ 100 同时在线，引入独立 MQ 是 over-engineering
  - 云 MQ：与"私有部署 K8s"双形态不兼容
  - DB Outbox 轮询：无法满足"7 天软删除 → 硬删除"的低延迟扫描；Redis Streams 已能覆盖
- **对团队维护能力的影响**：medium，Redis Streams 概念简单但 consumer group offset / claim / pending 需要文档
- **对成本/性能/安全/交付周期的影响**：成本 low；性能 high；安全 medium（Redis 需 AUTH + TLS）；交付 medium
- **置信度**：medium

## 5. 缓存（multi-provider M1）

- **选择**：自研 `ICache` + 两 provider（InMemory via `IMemoryCache`、Redis via `Microsoft.Extensions.Caching.StackExchangeRedis`）
- **为什么选择**：
  1. 用户 Q3 = M1
  2. .NET 内置 `IMemoryCache` 与 Redis Cache 接口（`IDistributedCache`）已存在，自研抽象只为统一 `GetOrSetAsync` 与 tag 失效语义
  3. 与队列 provider 复用同一 Redis 实例，降低运维成本
- **替代方案**：
  - 仅 InMemory（生产单副本）
  - Memcached
- **放弃替代方案的原因**：
  - 仅 InMemory：与生产 ≥ 2 副本部署冲突（缓存不一致）
  - Memcached：无 pub/sub、无持久化、与 Redis 比无独占优势
- **对团队维护能力的影响**：high（StackExchange.Redis 是 .NET 主流客户端）
- **对成本/性能/安全/交付周期的影响**：成本 low；性能 high（Redis ≤ 1ms）；安全 medium；交付 fast
- **置信度**：high

## 6. 对象存储（仅 Azure Blob + Local，不引入 S3 / OSS）

- **选择**：自研 `IObjectStorage` + 两 provider（Local 文件系统、Azure Blob via `Azure.Storage.Blobs`）
- **为什么选择**：
  1. 用户 Q4 = A（仅 Azure Blob）
  2. Local provider 满足开发 / 单机演示
  3. 不引入 S3 兼容层（FluentStorage / MinIO 客户端），降低依赖深度
- **替代方案**：
  - 引入 S3 兼容抽象（FluentStorage / Minio.AspNetCore）
  - 仅 Local（单机部署）
- **放弃替代方案的原因**：
  - S3 抽象：未在用户偏好范围内；引入即需 H4 测试矩阵新增一套；vNext 需要时再加
  - 仅 Local：与 K8s 多副本部署不兼容（不同 Pod 的本地文件系统不共享）
- **对团队维护能力的影响**：high（Azure SDK 主流）
- **对成本/性能/安全/交付周期的影响**：成本 low（头像 < 100GB）；性能 medium（Azure Blob hot tier）；安全 high（SAS token + 私有容器）；交付 fast
- **置信度**：high

## 7. Agent 框架

- **选择**：Microsoft Agent Framework `Microsoft.Agents.AI` 1.4.0+（GA）
- **为什么选择**：
  1. 用户偏好 2 + [AGENTS.md L14](../../AGENTS.md) "MAF dogfooding" 项目身份签字
  2. GA 状态稳定（[GitHub Releases](https://github.com/microsoft/agent-framework/releases) 最新 dotnet-1.4.0，2 天前发布）
  3. 内置 OpenTelemetry / 中间件 / 工具 / Workflow 编排 / Agent skills，与 [REQ-010 SKILL.md](../01-requirements/requirements.md) + REQ-011 + REQ-012（vNext MCP）契合
  4. 与 AG-UI 官方 1st-party 集成（[docs.ag-ui.com](https://docs.ag-ui.com/)：Agent Framework - 1st Party · Microsoft Agent Framework Supported）
- **替代方案**：
  - Semantic Kernel（MAF 前身）
  - AutoGen（MAF 前身）
  - 自研编排
  - LangChain.NET（社区版）
- **放弃替代方案的原因**：
  - Semantic Kernel / AutoGen：[learn.microsoft.com](https://learn.microsoft.com/en-us/agent-framework/overview/) 官方说明 MAF 是两者的"直接继任者"，新项目应直接选 MAF
  - 自研：dogfooding 项目目的与之冲突
  - LangChain.NET：社区维护，与 .NET 主流生态偏离，依赖更新不可控
- **对团队维护能力的影响**：medium（MAF 较新，团队学习成本中等；但文档与样例齐备）
- **对成本/性能/安全/交付周期的影响**：成本 low（开源 MIT）；性能 high；安全 high（Microsoft 维护）；交付 medium（GA 但生态仍在演进，[RISK-002](./risk-analysis.md)）
- **置信度**：medium

## 8. 模型网关

- **选择**：Azure OpenAI Service（gpt-4.1 部署，region 待 OQ-A-006）
- **为什么选择**：
  1. 用户 Q6 = A
  2. 与 MAF + AGENTS.md "Microsoft 生态 dogfooding" 一致
  3. 数据驻留 / 不用于训练默认承诺（[NFR-004](../01-requirements/requirements.md)）
  4. 内置内容过滤可关闭 / 按 region 调整（与 [E4 不做内容审核](../01-requirements/requirements.md) 边界协调）
- **替代方案**：
  - OpenAI 官方 API
  - 自建 LLM 网关（OneAPI / LiteLLM）
  - 多供应商抽象（vNext REQ-005）
- **放弃替代方案的原因**：
  - OpenAI 官方：合规与数据驻留对企业用户不友好
  - 自建网关：增加运维复杂度；MVP 规模不需要
  - 多供应商：[REQ-005 vNext](../01-requirements/requirements.md) 锁定，本期 MVP 固定单供应商
- **对团队维护能力的影响**：high（Azure SDK 主流）
- **对成本/性能/安全/交付周期的影响**：成本 medium（按 token 计费，[architecture.md 第 16 节](./architecture.md#16-成本估算含付费云资源)）；性能 high；安全 high；交付 fast
- **置信度**：high

## 9. AG-UI 协议（数据面 vs 对话面）

- **选择**：REST(JSON) 数据面 + AG-UI(SSE) 对话面双协议；前端用 `@ag-ui/client`，后端用 MAF 1st-party AG-UI integration（具体 NuGet 包名待 OQ-A-003）
- **为什么选择**：
  1. 用户偏好 7 硬约束
  2. 数据面 REST 调试简单 / 缓存友好；对话面 AG-UI 16 种 EventType 标准化覆盖 lifecycle / text-message / tool-call / state，降低自研事件 schema 成本
  3. 后端走 MAF 1st-party 集成（[docs.ag-ui.com](https://docs.ag-ui.com/) Agent Framework - 1st Party 列表 Supported），规避 [.NET SDK 仅社区 PR](https://docs.ag-ui.com/) 的风险
- **替代方案**：
  - 自研 SSE / WebSocket（与 OpenAI Responses API 一致的 delta 事件）
  - SignalR
  - gRPC streaming
- **放弃替代方案的原因**：
  - 自研：放弃 AG-UI 生态（CopilotKit / dojo 等）；事件 schema 自维护成本高
  - SignalR：协议绑定 .NET 客户端首选，但前端 React 客户端要走 `@microsoft/signalr`，与 AntD + AG-UI 生态不顺
  - gRPC streaming：浏览器需 grpc-web 转换，引入额外复杂度
- **对团队维护能力的影响**：medium-low（AG-UI 是较新协议，需读 16 种 EventType 文档；但官方与 MAF 集成已铺路）
- **对成本/性能/安全/交付周期的影响**：成本 low；性能 medium；安全 medium（SSE 需 K8s ingress 调优）；交付 medium（[RISK-001](./risk-analysis.md) [.NET SDK 状态] + [RISK-009](./risk-analysis.md) MAF 集成包待确认）
- **置信度**：medium

## 10. 平台登录方案

- **选择**：双模式（dev mock + 生产 OIDC，IdP 待 GAP-003 确定）
- **为什么选择**：
  1. 用户 Q5 = "先模拟登录，后续实现"
  2. [requirements.md ND-009 + R7](../01-requirements/requirements.md) 已锁定"不内置登录"；本架构以 `IPlatformAuthenticator` 接口预留 + dev/prod 双 handler 实现该约束
  3. ASP.NET Core 鉴权管道天然支持 multiple scheme，dev 与 prod 切换零代码改动
- **替代方案**：
  - 本期就接入某个 IdP
  - 本期完全不做登录（裸 API）
  - 本期自建 ASP.NET Core Identity（违反 ND-009）
- **放弃替代方案的原因**：
  - 接入 IdP：平台 IdP 未定（GAP-003），强行落地会被 H3 / H5 反复推翻
  - 不做登录：与 [PB-001 私有 + 跨用户隔离](../01-requirements/requirements.md) 测试无法验证
  - 自建 Identity：与 ND-009 冲突
- **对团队维护能力的影响**：high（ASP.NET Core 鉴权官方文档完善）
- **对成本/性能/安全/交付周期的影响**：成本 low；性能 high；安全 medium（dev mock 必须 build flag 隔离，[RISK-004](./risk-analysis.md)）；交付 medium（生产期切换需 OIDC 适配工作）
- **置信度**：medium

## 11. 联网搜索（T-1）实现

- **选择**：本期 Mock + `IWebSearchTool` 接口预留；生产期实现待 GAP-005 / OQ-A-002
- **为什么选择**：
  1. 用户 Q7 = "可以先 Mock 实现"
  2. [REQ-011 / AC-011-1](../01-requirements/requirements.md) 锁定首批 Tool = T-1 + T-3；T-1 服务商未定不阻塞 MVP UI 与协议
  3. Mock 实现 = 静态 JSON 返回（含 query echo + 3~5 条预制摘要），可覆盖 [E6 首次启用须知](../01-requirements/requirements.md) 的全部交互测试
- **替代方案**：
  - 本期就接入 Bing Web Search API
  - 本期接入 Tavily / Brave / Google CSE
  - 本期不上线 T-1（仅 T-3）
- **放弃替代方案的原因**：
  - 本期接入：服务商未确定，强行落地后切换成本高
  - 不上线 T-1：与 [ND-013](../01-requirements/requirements.md) 锁定首批 = T-1 + T-3 冲突
- **对团队维护能力的影响**：high（Mock 实现 < 100 行）
- **对成本/性能/安全/交付周期的影响**：成本 low；性能 high（Mock 同步返回）；安全 medium（生产切换时 [R8](../01-requirements/requirements.md) 缓解需重新评估，[RISK-005](./risk-analysis.md)）；交付 fast
- **置信度**：low（生产切换成本未量化）

## 12. 部署形态

- **选择**：开发用 docker-compose（postgres + redis + minio + 应用），生产用 Helm chart 部署到 K8s（≥ 2 副本 + Redis Sentinel + Azure 托管 DB / Blob + Ingress 启用 SSE 反代）
- **为什么选择**：
  1. 用户 Q8 双形态（开发 docker-compose / 生产 K8s + Sentinel + SSE 代理）
  2. 开发期 docker-compose 启动 < 60s，反馈循环快
  3. 生产期 K8s 是云原生标杆，AKS / EKS / 自建 K3s 通用
- **替代方案**：
  - 单一形态（开发与生产都用 K8s）
  - 仅 docker-compose（不上 K8s）
  - 直接部署 IIS / systemd
- **放弃替代方案的原因**：
  - 单一 K8s：开发期启动慢、本地资源占用高
  - 仅 docker-compose：生产高可用 / 滚动更新缺失
  - IIS / systemd：无云原生编排能力
- **对团队维护能力的影响**：medium（K8s 学习曲线中等；Helm chart 模板化）
- **对成本/性能/安全/交付周期的影响**：成本 medium（K8s Node $30~$60/月起，[architecture.md 第 16 节](./architecture.md#16-成本估算含付费云资源)）；性能 high；安全 high（Secret + RBAC）；交付 medium（首次 Helm chart 编写约 5~7 人天）
- **置信度**：medium

## 13. 单元测试框架

- **选择**：MSTest（用户加项）+ Moq + WebApplicationFactory + Testcontainers
- **为什么选择**：
  1. 用户加项 = MSTest
  2. WebApplicationFactory 是 ASP.NET Core 集成测试官方方案
  3. Testcontainers 用于 PG / SQL Server / Redis 真实容器集成测（M1 多 provider 行为差异验证）
- **替代方案**：
  - xUnit + Moq
  - NUnit + NSubstitute
- **放弃替代方案的原因**：
  - xUnit / NUnit：在用户硬约束下放弃；技术上等价
- **对团队维护能力的影响**：high（MSTest 是 .NET 内置测试框架，VS / VSCode 直接支持）
- **对成本/性能/安全/交付周期的影响**：成本 low；性能 high；安全 N/A；交付 fast
- **置信度**：high

## 14. 前端构建工具（H3 决，本节仅占位）

> 本节列在选型表里仅是为了"覆盖度提示" — H2 不替 H3 决前端构建工具。Vite 5 / Next.js / Remix 等候选由 H3 阶段在 `docs/04-detailed-design/` 中决定。本期 H2 默认 Vite 5（与 React 18 + TS 主流组合一致），如 H3 否决可推翻。

- **选择**：Vite 5（H2 默认；H3 可改）
- **为什么选择**：HMR 速度 / 主流 / AntD 5 兼容性已验证
- **替代方案**：Next.js（SSR）/ Remix / Rsbuild
- **放弃替代方案的原因**：本期纯 SPA 无 SSR 需求；Next.js 引入 Node 运行时 + 后端框架冲突
- **对团队维护能力的影响**：high
- **对成本/性能/安全/交付周期的影响**：成本 low；性能 high；安全 high；交付 fast
- **置信度**：medium（H2 默认；H3 可推翻）

---

## 总结

| # | 维度 | 选择 | 置信度 |
| --- | --- | --- | --- |
| 1 | 前端 | React 18 + TS + AntD 5 | high |
| 2 | 后端 | ASP.NET Core 9 | high |
| 3 | DB | EF Core + InMemory/SqlServer/PG (M1) | medium |
| 4 | 队列 | InMemory Channel + Redis Streams (M1) | medium |
| 5 | 缓存 | InMemory + Redis (M1) | high |
| 6 | 对象存储 | Local + Azure Blob | high |
| 7 | Agent 框架 | MAF GA 1.4.0 | medium |
| 8 | 模型网关 | Azure OpenAI gpt-4.1 | high |
| 9 | 对话协议 | REST + AG-UI(SSE) | medium |
| 10 | 登录 | dev mock + 生产 OIDC（IdP 待定） | medium |
| 11 | 联网搜索 | 本期 Mock + 接口预留 | low |
| 12 | 部署 | docker-compose（dev）+ K8s+Helm（prod） | medium |
| 13 | 测试 | MSTest + Moq + WAF + Testcontainers | high |
| 14 | 前端构建 | Vite 5（H3 可推翻） | medium |
