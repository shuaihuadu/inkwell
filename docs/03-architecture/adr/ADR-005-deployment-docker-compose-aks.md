---
id: ADR-005-deployment-docker-compose-aks
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
  - ADR-002
  - ADR-004
downstream:
  - ADR-009
  - ADR-013
  - ADR-015
  - ADR-016
---

# ADR-005 部署形态：dev = Docker Compose / prod = AKS

## 上下文

[Q-A5](../open-questions-arch.md) 用户答"开发环境 Docker Compose，生产环境 Azure Kubernetes Service (AKS)"。后端模块拓扑在 [§3.1 repo-impact-map](../../01-requirements/repo-impact-map.md) 已展开，需要支持：

- 后端 ASP.NET Core API（[ADR-002](./ADR-002-backend-runtime-dotnet10-aspnetcore.md)）
- 关系数据库（PostgreSQL / SQL Server，[ADR-004 IPersistenceProvider](./ADR-004-data-store-provider-switchable-ef-core.md)）
- 向量库（Qdrant）
- 缓存层（[ADR-016 ICacheProvider](./ADR-016-cache-provider-redis.md)）：dev = 本机 Redis 8 容器 / prod = [Azure Cache for Redis](https://learn.microsoft.com/azure/azure-cache-for-redis/) 或自建 Redis StatefulSet
- 可观测性栈（[ADR-013](./ADR-013-observability-otel-self-hosted-grafana.md)：Grafana / Loki / Tempo / Prometheus）
- Azure Speech 凭据（[ADR-009](./ADR-009-multimodal-azure-speech.md)）
- File Storage（[OQ-A005 closed §D](../open-questions-arch.md) + [ADR-015 IFileStorageProvider](./ADR-015-object-storage-provider-switchable.md)）：三 Provider 切换（`LocalFileSystem` / `AzureBlob` / `MinIO`）——dev 默认 MinIO，prod 由客户选 AzureBlob 或 MinIO

## 决策

**开发环境通过 Docker Compose 一键启动整个依赖栈；生产环境部署在 Azure Kubernetes Service (AKS)，通过 Helm Chart 管理。**

- `docker-compose.yml`（dev）：包含 `api` / `postgres` / `qdrant` / `redis` / `minio` / `grafana` / `loki` / `tempo` / `prometheus` 九个 service，单 `docker compose up -d` 启动。[ADR-015](./ADR-015-object-storage-provider-switchable.md) 文件存储 dev 默认 MinIO；需对接 Azure Blob 的开发者可通过 [`docker-compose.azurite.override.yml`](../../../docker-compose.azurite.override.yml) 切换。[ADR-016](./ADR-016-cache-provider-redis.md) 缓存 dev 走本机 Redis 8 容器（[redis:8](https://hub.docker.com/_/redis)）。
- Helm Chart `charts/inkwell/`（prod）：
  - API：Deployment + HPA（CPU 70% 触发）
  - PostgreSQL：StatefulSet + PVC（[Azure Disk Premium](https://learn.microsoft.com/azure/aks/azure-csi-disk-storage-provision)）
  - Qdrant：StatefulSet + PVC
  - Redis：默认 [Azure Cache for Redis](https://learn.microsoft.com/azure/azure-cache-for-redis/cache-overview) 该项外部服务 + Private Endpoint 接入；自建场景可切换为 `redis` StatefulSet + PVC
  - Grafana 栈：通过 [grafana-stack umbrella chart](https://github.com/grafana/helm-charts) 引用
  - Ingress：[NGINX Ingress Controller](https://learn.microsoft.com/azure/aks/app-routing) + cert-manager（Let's Encrypt）
- Region：v1 仅单 region（east-asia 或 china-east2，按客户需求选）；多 region 不在 v1 范围（[OQ-006 closed §A](../../01-requirements/open-questions.md)）。
- 镜像仓库：[Azure Container Registry (ACR)](https://learn.microsoft.com/azure/container-registry/)。

## 备选项

### 备选 A：dev = AKS / prod = AKS（开发与生产同构）

- **放弃理由**：(1) AKS 启动一个 dev 集群成本 ≥ ¥1000/月，远高于本地 Compose；(2) 本地 Compose 启动 < 30 s，AKS 启动 + 镜像拉取 5 ~ 10 min，开发循环慢；(3) [REQ-016 麦克风](../../01-requirements/requirements.md) 调试需要客户端直连后端，跨网络 AKS 链路调试不便。

### 备选 B：dev = Compose / prod = Azure Container Apps (ACA)

- **放弃理由**：(1) ACA 不支持 [Microsoft.Agents.AI.DurableTask](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/) 跨实例的 sticky session 需求 — DurableTask 需要稳定的 actor placement，ACA 的弹性伸缩与无状态假设冲突。(2) ACA 对 GPU / 大内存实例的支持弱于 AKS。(3) Helm Chart 在 ACA 上不可用，部署模板要重写。

### 备选 C：dev = Compose / prod = 自建 K8s 集群

- **放弃理由**：(1) 自建 K8s 控制面需要专职运维，与 [OQ-006](../../01-requirements/open-questions.md) v1 范围冲突；(2) 客户大多在 Azure 上，AKS 可直接绑定 Entra ID / KeyVault / Monitor；(3) 升级 / 安全补丁 / 备份恢复需要团队自维护。

### 备选 D：单容器 + Docker Swarm

- **放弃理由**：Docker Swarm 已进入维护模式，社区生态萎缩；不是负责任的长期选择。

## 后果

### 正面

- dev 体验：单 `docker compose up -d` + 自动等待健康检查通过即可进入开发；对 [OQ-A007](../open-questions-arch.md) CI 也友好（GitHub Actions 直接复用 Compose 文件跑集成测试）。
- prod 与 dev 拓扑同构：8 个 service ≈ 8 个 Deployment / StatefulSet，环境切换主要在配置层（appsettings + Helm values）。
- AKS 与 [Azure Speech](./ADR-009-multimodal-azure-speech.md) / [Azure Cache for Redis](./ADR-016-cache-provider-redis.md) / [Azure Blob](./ADR-015-object-storage-provider-switchable.md) 同 region 通信，延迟与权限链路稳定。
- Helm Chart 是行业惯例，新人接入成本低。
- 凭据隔离（[OQ-A006 closed §B](../open-questions-arch.md)）：dev 走 [Docker Compose `.env`](https://docs.docker.com/compose/environment-variables/set-environment-variables/)（不进仓）；prod 走 [Kubernetes Secret](https://kubernetes.io/docs/concepts/configuration/secret/) + [静态加密 at rest](https://kubernetes.io/docs/tasks/administer-cluster/encrypt-data/) + RBAC 收敛访问者；**未引入 Azure Key Vault / Key Vault CSI**，换取该能力的代价与残余风险走 [RISK-013](../risk-analysis.md)。
- Public API rate limit（[ADR-007](./ADR-007-public-api-token-auth.md)）与模型响应 cache 都走 [`ICacheProvider`](./ADR-016-cache-provider-redis.md)，在多副本部署下语义一致。

### 负面

- AKS 单 region 不具备跨区高可用，[NFR-006 安全](../../01-requirements/requirements.md) 边界 + [NFR-001 可用性](../../01-requirements/requirements.md) 在 v1 仅给软目标 — 详见 [RISK-004](../risk-analysis.md)。
- Azure 锁定（厂商绑定）：客户若需在非 Azure 部署，需要额外 Helm values 模板（PostgreSQL 自管理 / 其他 File Storage / 其他 Redis / 其他 Vault）；H3 详细设计需要把 Azure-specific 依赖封装在可替换的接口后。
- AKS 升级窗口需要业务侧协调（dev / prod Kubernetes 版本对齐）。
- 缓存层仍是新运维项：调优 / 补丁 / 重启对齐 Pod scale 有学习曲线（[ADR-016 §后果](./ADR-016-cache-provider-redis.md)）。

### 中性

- Compose dev 栈本地资源占用 ≥ 4 GB RAM，开发机需要 ≥ 16 GB 内存。
- Helm 模板的 values 设计是 H3 详细设计的一项独立工作。

## 状态

- **状态**：accepted
- **首次发布**：2026-05-08
- **关联**：supersedes 无；上游 [ADR-002](./ADR-002-backend-runtime-dotnet10-aspnetcore.md) / [ADR-004](./ADR-004-data-store-provider-switchable-ef-core.md)；下游 [ADR-009](./ADR-009-multimodal-azure-speech.md) / [ADR-013](./ADR-013-observability-otel-self-hosted-grafana.md)
- **置信度**：high（与 Q-A5 答案直接对齐，AKS + Compose 是业界主流双栈）
