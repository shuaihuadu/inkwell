---
id: ADR-006
title: 部署形态分裂（dev docker-compose / prod K8s + Helm）
stage: H2
status: accepted
authors:
  - name: H2-ArchitectAdvisor
    role: agent
date: 2026-05-07
upstream:
  - architecture-custom-agent
  - tech-selection-custom-agent
supersedes: []
superseded-by: []
---

# ADR-006：部署形态分裂（dev docker-compose / prod K8s + Helm）

## 上下文

H2 反问 Q8 用户答：
> 本地开发容器（Docker）+ 单机 docker-compose，生产容器 + Kubernetes（含 ingress / SSE 代理 / Redis Sentinel）

NFR：≤ 100 同时在线（[NFR-002](../../01-requirements/requirements.md)）；生产期需要高可用 + 滚动更新。

## 决策

### 开发形态（dev）

- `docker-compose.yml` 启动栈：postgres + redis + minio（local 替代）+ 后端镜像 + 前端镜像
- `Authentication:Mode=DevMock`
- 启动 < 60s 反馈循环优先

### 生产形态（prod）

- Helm chart 部署到 K8s
- 后端 Deployment ≥ 2 副本，rolling update（`maxSurge=25%, maxUnavailable=0`）
- 前端 Deployment（也可走 Static Web App / CDN，H3 决）
- Ingress（默认 NGINX Ingress Controller）：
  - `proxy_buffering off`
  - `proxy_read_timeout 3600`
  - `proxy_send_timeout 3600`
  （[RISK-006](../risk-analysis.md)）
- Redis Sentinel（或 Azure Cache for Redis）
- DB 走外部托管（Azure Database for PG 或 SQL Server，[NFR-004 同 region](../../01-requirements/requirements.md)）
- Azure Blob 走外部托管
- `Authentication:Mode=Oidc`
- Secret 走 K8s Secret + Azure Key Vault
- 健康探针：`/healthz` liveness + `/readyz` readiness（含 DB / Redis ping）
- OTel 出口走 `OTEL_EXPORTER_OTLP_ENDPOINT`，目的地由平台运维决定

## 备选项

| 备选 | 放弃理由 |
| --- | --- |
| 单一形态（开发 + 生产都用 K8s） | 开发期启动慢、本地资源占用高；反馈循环糟糕 |
| 仅 docker-compose（不上 K8s） | 生产高可用 / 滚动更新 / 多副本能力缺失 |
| IIS / systemd 直接部署 | 无云原生编排能力；Linux 容器与 Windows IIS 主机隔阂 |
| Azure Container Apps / Service Fabric | 用户偏好已锁 K8s；vNext 私有部署需通用方案 |
| dev 用 Aspire（.NET Aspire Dashboard） | 锁定 .NET 生态；与"前后端两个语言栈"演练价值低；Aspire 仍演进中 |

## 后果

### 正面

- 开发反馈循环快，新人 1 条命令上手
- 生产高可用 + 多副本 + 滚动更新满足 [NFR-002 ≤ 100 同时在线](../../01-requirements/requirements.md) 与未来 vNext 扩展
- AKS / EKS / 自建 K3s 通用，私有部署友好

### 负面

- 双形态双 Helm / compose 文件维护成本（首次约 5~7 人天）
- Helm chart 模板初版易出 SSE 配置遗漏（[RISK-006](../risk-analysis.md)）→ H6 阶段端到端 SSE 延迟回归
- dev / prod 配置漂移风险 → values.yaml 与 docker-compose.yml 共享 .env 文件可缓解

### 中性

- 开发期用 minio（S3 兼容）替代 Azure Blob；接口由 `IObjectStorage` 抽象兜底，不影响 [ADR-008](./ADR-008-object-storage-azure-blob-only.md)

## 状态

`accepted` · 2026-05-07

## 关联待澄清

- [OQ-A-005](../open-questions-arch.md) K8s 集群基线（K8s 版本 / Ingress 控制器 / Redis 形态）
