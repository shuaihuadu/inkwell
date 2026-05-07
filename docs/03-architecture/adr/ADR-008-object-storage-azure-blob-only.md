---
id: ADR-008
title: 对象存储仅 Azure Blob + Local，不引入 S3 / OSS
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

# ADR-008：对象存储仅 Azure Blob + Local，不引入 S3 / OSS

> ADR 编号跳过 007 是为了让"测试框架 = MSTest"留 ADR-007 占位（如未来需要重新评估测试框架时启用）；本期 MSTest 由 [tech-selection.md 第 13 节](../tech-selection.md) 直接落定，不需要独立 ADR。

## 上下文

[REQ-002 头像上传](../../01-requirements/requirements.md) 是本特性唯一的对象存储用例。
H2 反问 Q4 用户答：
> A 仅 Azure Blob

## 决策

- 抽象：`IObjectStorage` 接口（`UploadAsync` / `GetReadUrlAsync` / `DeleteAsync`）
- Provider：
  - **Local**：开发 / 单机演示
  - **Azure Blob**：生产（`Azure.Storage.Blobs` SDK）
- 不引入 S3 兼容抽象（FluentStorage / Minio 客户端）
- 不引入阿里云 OSS / 腾讯云 COS / Google Cloud Storage SDK

## 备选项

| 备选 | 放弃理由 |
| --- | --- |
| 引入 S3 兼容抽象（FluentStorage） | 未在用户偏好范围内；引入即需 H4 测试矩阵新增一套；vNext 需要时再加，符合 [Implementation Discipline](../../../.github/copilot-instructions.md) "只做被请求或明确必要的"原则 |
| 同时支持 Azure Blob + S3 + OSS（M1 模式扩展） | 工作量翻倍；单实例需求未确认 |
| 仅 Local 单 provider | 与 K8s 多副本部署不兼容（不同 Pod 本地文件系统不共享） |
| 仅 Azure Blob 单 provider | 单机 / 演示 / 离线开发不可用 |

## 后果

### 正面

- 接口已抽象，vNext 加 S3 / OSS 仅新增实现类，不改应用层
- 私有部署 = Local 单副本 或 NFS 挂载 + Local（[RISK-007](../risk-analysis.md) 缓解）

### 负面

- 私有部署多副本场景（无 Azure 公网访问 + 多副本要求）暂不友好（[RISK-007](../risk-analysis.md)）→ release notes "运维须知"提示
- vNext S3 provider 排期未定

### 中性

- 开发期用 minio（S3 兼容）启动只是 docker-compose 里多一个容器，Local provider 仍 cover 本机文件系统；不阻塞

## 状态

`accepted` · 2026-05-07
