---
id: ADR-025-local-orchestration-aspire
stage: H2
status: draft
authors:
  - name: GitHub Copilot
    role: agent
reviewers: []
created: 2026-07-13
updated: 2026-07-13
upstream:
  - ADR-002
  - ADR-005
  - ADR-019
  - ADR-024
downstream: []
---

# ADR-025 本地开发编排：Aspire AppHost

## 上下文

[ADR-005](./ADR-005-deployment-docker-compose-aks.md) 将开发环境锁定为 Docker Compose，生产环境锁定为 AKS + Helm；[ADR-024](./ADR-024-database-migration-seed-standalone-job.md) 又要求 `Inkwell.Migrator` 在 `Inkwell.WebApi` 与 `Inkwell.Worker` 启动前成功执行。当前仓库尚未落地 Compose 文件，但三个 .NET 入口项目已经存在。

Inkwell 的本地开发需要统一处理资源生命周期、连接字符串注入、项目启动顺序和可观测性入口。继续手写 Compose 可以满足容器编排，却需要额外维护项目构建、环境变量和一次性 Migrator 的依赖关系。Aspire AppHost 能以强类型 .NET 应用模型描述这些关系，并在本地提供统一 Dashboard。

## 决策

**本地开发使用 Aspire AppHost；生产部署继续使用 ADR-005 已确定的 AKS + Helm。**

本 ADR 仅取代 ADR-005 与 ADR-024 中的开发环境 Docker Compose 编排，不改变以下决策：

- `Inkwell.Migrator` 仍是 Migration + Seed 的唯一执行者。
- `Inkwell.WebApi` 与 `Inkwell.Worker` 必须等待 Migrator 成功退出后再启动。
- 生产环境仍由 Helm `pre-install,pre-upgrade` hook Job 保证 Migrator 顺序。
- WebApi、Worker 与 Migrator 的生产制品仍须使用同一 image tag。

### AppHost 边界

新增 `src/core/Inkwell.AppHost/`，使用 `Aspire.AppHost.Sdk` 和 `Aspire.Hosting.PostgreSQL`，首批只编排当前代码真实依赖的资源：

- PostgreSQL 17，默认使用临时开发数据库；每次 AppHost 重建资源时由 Migrator 恢复 schema 与 Seed。
- `Inkwell.Migrator` 一次性项目资源。
- `Inkwell.WebApi` 常驻项目资源。
- `Inkwell.Worker` 常驻项目资源。

PostgreSQL 数据库资源命名为 `Inkwell`，由 `WithReference` 注入 `ConnectionStrings:Inkwell`。Migrator 额外接收 `Inkwell:Persistence:Provider=Postgres`。WebApi 与 Worker 使用 `WaitForCompletion(migrator)`，只有 Migrator 成功退出后才启动；Migrator 使用 `WaitFor(database)` 等待数据库就绪。

Redis、Qdrant、MinIO 不在首批 AppHost 中提前声明。对应入口项目切换到实际 Provider 后，再按真实消费关系加入 AppHost，避免出现资源已启动但应用仍使用内存实现的假集成。

可观测性不依赖业务 Provider，按 [ADR-013](./ADR-013-observability-otel-self-hosted-grafana.md) 由 AppHost 编排 OTel Collector、Tempo、Loki、Prometheus 与 Grafana。WebApi / Worker 统一通过 OTLP gRPC 上报 trace、log、metric；Grafana 启动时预置三个后端数据源。当前基线只覆盖通用 ASP.NET Core、HttpClient 和 .NET Runtime 遥测，业务自定义指标、Dashboard 与 SMTP 告警规则仍按后续 H5 任务增量落地。

### 版本约束

- Aspire SDK 与 Hosting integration 锁定同一稳定版本。
- PostgreSQL 显式锁定 17，避免 Aspire 13.4 默认 PostgreSQL 18 与既有架构版本不一致。
- 首批 AppHost 不挂载数据卷。Aspire 默认生成的 PostgreSQL 密码与容器同生命周期，避免复用旧卷时因新密码无法通过健康检查；如需跨 AppHost 启动保留数据，必须同时设计稳定的本地 Secret 与卷生命周期。
- AppHost 只承担本地开发编排，不作为生产运行时，也不生成或替代 Helm 部署资产。

## 备选项

### 备选 A：继续使用 Docker Compose

放弃。Compose 能表达容器依赖，但本仓库当前入口均为 .NET 项目，Aspire 的强类型项目引用、配置注入、启动等待和 Dashboard 更贴合本地开发循环。

### 备选 B：Aspire 同时负责本地与生产部署

放弃。生产 AKS + Helm 已由 ADR-005 锁定，并承载 HPA、Helm hook、Secret、Ingress 与单 release 升级约束。AppHost 不替代这些生产运维契约。

### 备选 C：首批一次性编排全部基础设施

放弃。当前 WebApi 与 Worker 仍使用 InMemory Cache、Channels Queue 和 InMemory Vector Store；提前启动 Redis、Qdrant、MinIO 与完整 Grafana 栈不会验证真实业务路径，只会增加本地资源消耗与维护成本。

## 后果

### 正面

- 单一 AppHost 同时表达资源、连接信息与启动顺序。
- `WaitForCompletion` 保留 ADR-024 的 Migrator 成功门禁，失败时不会启动 WebApi/Worker。
- Aspire Dashboard 集中展示本地资源状态、日志与端点。
- 后续 Provider 接入可以按实际依赖增量扩展，不需要重写整体编排。

### 负面

- 开发者需要 Docker 兼容容器运行时，并需要通过 .NET SDK 或 Aspire CLI 启动 AppHost。
- 本地开发拓扑与生产 Helm 拓扑不再由同一种声明格式表达，需要通过集成测试与生产部署检查保持行为一致。
- Aspire 与 PostgreSQL integration 成为新的开发期依赖，需要按兼容性成组升级。

### 中性

- Compose 不再作为 Inkwell 的本地启动入口，但 Testcontainers 仍可继续服务于 Provider 集成测试。
- 生产 AKS、Helm、ACR、HPA 和 Kubernetes Secret 决策不受影响。

## 迁移路径

1. 将 `Inkwell.AppHost` 加入解决方案并编排 PostgreSQL、Migrator、WebApi、Worker。
2. 验证 Migrator 成功完成后 WebApi/Worker 才启动，Migrator 失败时二者保持等待状态。
3. 通过 AppHost 编排 OTel Collector、Tempo、Loki、Prometheus 与 Grafana，验证 trace、log、metric 三条链路。
4. 将本地开发文档从 `docker compose up` 更新为运行 AppHost。
5. 后续每启用一个外部 Provider，同步添加对应 Aspire hosting integration 和端到端验证。

## 状态

`draft`。待 Owner 审阅后人工翻转状态；在此之前不得修改 ADR-005 的既有签字字段。

## 置信度

`high`。Aspire 13.4.6 已通过当前 .NET 10 解决方案的实际编译验证，`AddPostgres`、`WithReference`、`WaitFor` 与 `WaitForCompletion` 均可用。

