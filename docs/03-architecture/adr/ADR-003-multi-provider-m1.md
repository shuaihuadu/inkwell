---
id: ADR-003
title: 多 provider 抽象采用 M1 模式（运行时按配置切换）
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

# ADR-003：多 provider 抽象采用 M1 模式（运行时按配置切换）

## 上下文

用户偏好 3~6：
> 数据库：可以支持 InMemory、SQL Server、Postgres
> 队列：可以支持 InMemory、Redis
> 缓存：可以支持 InMemory、Redis
> 对象存储：可以支持本地、常见的云存储（譬如 Azure Storage Account）

H2 反问 Q3 答案：
> 多 provider「支持」具体是同时支持，运行时单实例按配置切换

定义 3 种 multi-provider 模式：
- **M1 同时支持运行时切换**：单实例按 `appsettings:*:Provider` 配置切换，所有 provider 都需实现并测
- **M2 编译期开关**：编译时选一种 provider，其他不打包
- **M3 仅一种 provider 锁定**：完全不抽象

用户选 M1（最高抽象成本，最高灵活性）。

## 决策

- **DB**：EF Core 9 + 三 provider（InMemory / SqlServer / PostgreSQL）；DbContext 单一，provider 仅在 `Program.cs` 切换；迁移按 provider 分目录
- **队列**：自研 `IQueue<T>` + 两 provider（InMemory `Channel<T>` / Redis Streams）
- **缓存**：自研 `ICache` + 两 provider（`IMemoryCache` / Redis via StackExchange.Redis）
- **对象存储**：自研 `IObjectStorage` + 两 provider（Local 文件系统 / Azure Blob via `Azure.Storage.Blobs`）
- **InMemory 限定测试 / 单机演示**：多副本 K8s 部署强制非 InMemory（Helm chart 校验，[RISK-008](../risk-analysis.md)）

## 备选项

| 备选 | 放弃理由 |
| --- | --- |
| M2 编译期开关（条件编译 / SourceGen） | 测试矩阵仍要全 provider 跑（每个 build flavor 一遍）；运行时灵活性不如 M1；用户已选 M1 |
| M3 锁单一 provider（仅 PG / 仅 Redis / 仅 Azure Blob） | 与用户 Q3 答案冲突；放弃私有 / 单机部署灵活性 |
| 抽象更宽（再加 SQLite / MySQL / S3 / OSS） | 超出本期需求；M1 抽象层不限 vNext 加 provider，本期只锁定用户列出的 |

## 后果

### 正面

- 私有部署 / 公有云 / 自托管 / 单机演示等多场景共享同一份后端镜像
- 抽象接口本身是一个有用产物：`IRepository` / `IQueue` / `ICache` / `IObjectStorage` 在 vNext 接入新 provider（如 SQLite / S3）零成本

### 负面

- **抽象层 + 测试矩阵成本约 H5 阶段额外 15~20 人天**（[RISK-003](../risk-analysis.md)）
- M1 抽象漏抽风险：某个 provider 的原生特性（如 PG `JSONB` / SQL Server `MERGE`）被绕过 → 性能低于直用
- 行为差异调试成本：同一段代码在不同 provider 下行为微妙不同（大小写敏感 / 隔离级别 / 索引语义），首次切 provider 必出问题

### 中性

- CI 矩阵成本可通过"按 PR 标签触发集成测"（PR 加 `provider:postgres` 标签才跑 PG 集成测）控制总耗时
- Testcontainers 让本地开发 + CI 共享 provider 启动方式

## 状态

`accepted` · 2026-05-07

## 监督指标

H6 阶段每个 provider 至少跑一次完整 E2E + 关键路径压测；任一 provider 缺一个关键路径覆盖，本 ADR 触发 review。
