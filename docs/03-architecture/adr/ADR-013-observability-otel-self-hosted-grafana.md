---
id: ADR-013-observability-otel-self-hosted-grafana
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
  - ADR-005
downstream: []
---

<!-- 2026-05-10 [ADR-019 进程拓扑](./ADR-019-process-topology-webapi-worker-split.md) 引入后：OTel `service.name` resource attribute 区分 `inkwell-webapi` / `inkwell-worker`；Prometheus scrape 双 source；Grafana Dashboard 加「队列吞吐 / Worker 健康」面板。 -->

本地调试需要集成Aspire

# ADR-013 可观测性：OpenTelemetry + 自托管 Grafana 栈

## 上下文

[REQ-014 调试 / 评测](../../01-requirements/requirements.md) 要求 trace 全链路（LLM 调用 / 工具调用 / Skill 命中 / 编排节点 / 错误）。[Q-A8](../open-questions-arch.md) 用户答 "A 自托管 Grafana / Loki / Tempo / Prometheus"。[ADR-005](./ADR-005-deployment-docker-compose-aks.md) 已锁定 Compose（dev） / Helm（prod）部署模板。

[ADR-003 Microsoft Agent Framework](./ADR-003-agent-engine-microsoft-agent-framework.md) 内置 OpenTelemetry instrumentation；[ADR-002 ASP.NET Core](./ADR-002-backend-runtime-dotnet10-aspnetcore.md) 通过 `Microsoft.Extensions.Diagnostics` + [OpenTelemetry .NET SDK](https://opentelemetry.io/docs/languages/dotnet/) 输出 metric / trace / log。

## 决策

**可观测性栈：OpenTelemetry .NET SDK 输出 → OTel Collector → 三个后端：[Tempo](https://grafana.com/oss/tempo/)（trace）/ [Loki](https://grafana.com/oss/loki/)（log）/ [Prometheus](https://prometheus.io/)（metric）；统一 UI 走 [Grafana](https://grafana.com/)。dev 用 Docker Compose 跑全栈；prod 用 Helm Chart 部署到 AKS。**

- SDK 配置：`AddOpenTelemetry()` + `WithMetrics()` + `WithTracing()` + `WithLogs()`，全部走 OTLP gRPC 到 Collector。`service.name` resource attribute 区分 `inkwell-webapi` / `inkwell-worker`（[ADR-019](./ADR-019-process-topology-webapi-worker-split.md)），dashboards 按 service 维度切分。
- Collector：单 Deployment，配置 receiver = OTLP（双 source：webapi + worker），exporter = Tempo / Loki / Prometheus。
- Grafana：默认 Dashboard 包含：(1) Run 数 / 平均延迟 / 错误率 / 工具调用数 / Skill 命中数；(2) PostgreSQL / Qdrant 健康；(3) AKS Pod 健康（节点 CPU / 内存 / 重启次数）；(4) **队列吞吐 / Worker 健康**（`queue_depth` / `inkwell-worker` Pod restart count / `BackgroundService` 自定义 metric）。
- 数据保留：Loki / Tempo 保留 30 天（v1 默认）；Prometheus 保留 15 天 metric raw + 365 天 5 min downsample。
- 业务 Dashboard：[REQ-014 trace 面板](../../01-requirements/requirements.md) 在 [UI-007 调试页](../../01-requirements/ui-spec.md) 是独立 UI（直接查 Tempo），不嵌入 Grafana iframe。
- 告警：v1 仅 Grafana Alerting → SMTP（邮件）一种通道。

## 备选项

### 备选 A（Q-A8 §B）：[Azure Application Insights / Azure Monitor](https://learn.microsoft.com/azure/azure-monitor/)

- **放弃理由**：(1) 与"客户可能在非 Azure 环境部署"的方向冲突 — 自托管 Grafana 栈在 AKS / 自建 K8s / 公有云通用；(2) Azure Monitor 在 dev 环境（[ADR-005 Compose](./ADR-005-deployment-docker-compose-aks.md)）无法本地启动 emulator；(3) Azure Monitor 月费随数据量线性增长，自托管栈成本可控。

### 备选 B（Q-A8 §C）：自建 ELK（Elasticsearch + Logstash + Kibana）

- **放弃理由**：(1) ELK 运维成本高（mapping / index lifecycle / cluster sizing）；(2) Trace 能力不如 Tempo；(3) 团队 Grafana 栈经验大于 ELK。

### 备选 C：[Datadog](https://www.datadoghq.com/) / [New Relic](https://newrelic.com/) SaaS

- **放弃理由**：(1) SaaS 月费高 + 数据出境合规问题；(2) 与"v1 自托管 / 内网部署"客户场景冲突。

### 备选 D：仅写应用日志文件（不做 OTel）

- **放弃理ով**：(1) [REQ-014 trace 全链路](../../01-requirements/requirements.md) 需要 trace span 关联，文件日志做不到；(2) MAF 内置 OTel instrumentation 不用就浪费。

## 后果

### 正面

- OpenTelemetry 是 CNCF 标准，与 Microsoft Agent Framework 内置 instrumentation 直接对齐。
- Grafana 栈（Tempo / Loki / Prometheus）是 [grafana/helm-charts](https://github.com/grafana/helm-charts) 一键部署，[ADR-005](./ADR-005-deployment-docker-compose-aks.md) Helm 模板友好。
- dev 环境 Compose 跑全栈，开发人员本地能看 Run trace + log + metric，调试效率高。
- v2 客户切到非 Azure 环境时，可观测性栈零修改可迁移。

### 负面

- 自托管栈带来运维负担（备份 / 节点扩容 / 版本升级）；通过 Helm Chart + AKS managed disk + 默认告警规则缓解 — 详见 [RISK-006](../risk-analysis.md)。
- 数据保留策略需要按客户合规要求微调（v1 默认 30 天）。
- Grafana Alerting 只支持 SMTP 是 v1 限制，企业通常需要钉钉 / 企业微信 / Webhook — 列入 v2 增强项。

### 中性

- [UI-007 调试页](../../01-requirements/ui-spec.md) 直接查 Tempo（自建 UI）而不是嵌 Grafana iframe — 业务 trace UI 与 ops Dashboard 分离。
- v1 OTel Collector 单实例，prod 可水平扩展（[ADR-005 AKS](./ADR-005-deployment-docker-compose-aks.md) HPA）。

## 状态

- **状态**：accepted
- **首次发布**：2026-05-08
- **关联**：supersedes 无；上游 [ADR-002](./ADR-002-backend-runtime-dotnet10-aspnetcore.md) / [ADR-005](./ADR-005-deployment-docker-compose-aks.md) / [Q-A8](../open-questions-arch.md)
- **置信度**：high（Q-A8 已答；Grafana 栈是业界主流自托管观察方案）
