---
id: HD-001
title: Inkwell.Abstractions 详细设计 — Foundation（Builder DSL + Common DTO + 错误与日志约定）
stage: H3
status: reviewed
reviewers: [Inkwell]
upstream:
  - REQ-001
  - REQ-014
  - REQ-016
  - ADR-002
  - ADR-017
  - ADR-018
  - ADR-019
  - ADR-020
  - ADR-021
  - ADR-023
---

> **错误处理约定**（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) accepted by Inkwell 2026-05-11，含 [errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) 废错误码、[errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 删 `Result<T>` / `Error` 抽象）：端口层与业务层**统一**采用裸 `Task<T>` + 异常，失败语义仅靠 [.NET BCL 异常类型](https://learn.microsoft.com/dotnet/standard/exceptions/)表达；Inkwell **不自建 `Result<T>` / `Error` 抽象** / 不自建错误码机制 / 不自建端口层异常基类，仅保留 `InkwellConfigurationException` / `InkwellBuilderException` 两个程序错误子类用于 DI 装配期校验。详 §3 / §4 / §5。
>
> **2026-05-11 errata·第四轮**（Owner 主动指令 + [ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) accepted by Inkwell）：删除 `Common/Result.cs` / `Common/Error.cs` 两个文件及全部引用。业务命名空间错误处理统一 BCL 异常，不再保留 “业务可选” 工具。受影响章节：§1.2 / §1.3 Q1 / §2.2 / §3.1 / §3.2 / §4.1 / §5.3 / §7 / §10 Q1 / §11 / §14.1。详 §13 errata 记录。
>
> **范围切片**：本 HD 仅覆盖 `Inkwell.Abstractions` 的"地基"——`InkwellException` 两子类、`IInkwellBuilder + AddInkwell()`、`InkwellOptions` 根、共享 DTO、5 大端口的命名 / 签名 / 错误 / 日志公共约定。具体的 5 个端口接口（`IPersistenceProvider` / `IFileStorageProvider` / `ICacheProvider` / `IQueueProvider` / `IAgentRuntime`）与向量存储接入 拆到 HD-002 ~ HD-008，每个端口独立一张 HD。
>
> **拓扑依据**：[ADR-017 §依赖规则](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) — `Inkwell.Abstractions` **零外部包依赖**（除 `Microsoft.Extensions.*.Abstractions`、`Microsoft.Extensions.VectorData.Abstractions` 与 `Microsoft.Extensions.AI.Abstractions`[2026-07-06 errata·第六轮，详 §13]）。
>
> **2026-07-05 errata·第五轮**（[design-review-report §3.2 N2/C4](../design-review-report.md#n2auditcontextactoruserid-与-ihasownerowneruserid-类型分歧c4) + HD-007 起草期 Owner picker 拍板）：`Common/AuditContext.cs` 的 `ActorUserId` 字段类型由 `string` 改为 `Guid`，与 [HD-002 §3.9](HD-002-Inkwell.Abstractions-persistence-port.md) `IHasOwner.OwnerUserId: Guid` 强一致（均指向 `users.id`）；系统 actor（定时任务 / Trigger 触发）用 `Guid.Empty` 表示，构造期校验不再要求 `ActorUserId` 非空（`Guid.Empty` 是合法值）。受影响章节：§3.7。详见 [HD-007 §13 决策记录](HD-007-Inkwell.Abstractions-audit-logger-port.md#13-决策记录)。
>
> **2026-07-09 errata·第七轮**（Owner 决定 v1 不做审计日志功能，详见 [requirements.md §13 第 14/23 条 2026-07-09 决策更新](../../01-requirements/requirements.md)）：上方 2026-07-05 第五轮 errata 描述的 `Common/AuditContext.cs` 已**整个删除**（仅服务于已删除的 `IAuditLogger`/HD-007）；§3.7 小节同步删除，本 errata 保留作为历史记录。
>
> **2026-07-06 errata·第六轮**（[design-review-report.md §18.3 B15](../design-review-report.md#b15q5比照-vectordata-先例缺物理落地机制iembeddinggenerator-依赖白名单例外未实际生效c91) + Owner picker 拍板选项 1）：`Microsoft.Extensions.AI.Abstractions` 对称纳入 `Inkwell.Abstractions.csproj` 依赖白名单 + `GlobalUsings.cs` 追加 `global using Microsoft.Extensions.AI;`，使 [HD-008 §6](HD-008-Inkwell.Abstractions-vector-store-type-alias.md#6-与-iembeddinggeneratortinput-tembedding-的衔接) 锁定的 `IEmbeddingGenerator<string, Embedding<float>>` 通过依赖 `Inkwell.Abstractions` 间接可见，与 `Microsoft.Extensions.VectorData.Abstractions`（C86 既有落地）完全同构；不修改 [AGENTS.md §3.2](../../../AGENTS.md)。受影响章节：§2 / §14.3。详 §13 errata 记录。

## 1. 模块概述

### 1.1 职责

`Inkwell.Abstractions` 是 Ports & Adapters 拓扑（[ADR-017](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）中的**端口层**——它是整个后端的"接口总账本"：

- 5 大基础设施端口接口（`IPersistenceProvider` / `IFileStorageProvider` / `ICacheProvider` / `IQueueProvider` / `IAgentRuntime`）
- 业务模块对外接口（如 `IAgentService` / `IConversationService`，逐 module 定义）
- 共享 Result / Error 模型与异常基类
- 共享 DTO（`Pagination` / `SortOrder` / `TimeRange`）
- Options 根 `InkwellOptions` + 子 Options 命名约定
- DI 装配入口 `IInkwellBuilder` + `AddInkwell()` 静态扩展（与 [Microsoft Agent Framework `AgentApplicationBuilder`](../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI/) 风格一致）

> 向量存储抽象**复用** [`Microsoft.Extensions.VectorData.VectorStore`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data) / `VectorStoreCollection<TKey, TRecord>`（[ADR-020](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md)），不在 `Inkwell.Abstractions` 重新发明 `IVectorStore`；`Inkwell.Abstractions` 仅暴露 Builder DSL 钩子（`UseQdrantVectorStore` / `UseInMemoryVectorStore` / `UseAzureOpenAIEmbeddings`，由 HD-008 起草）。

### 1.2 范围（HD-001 = Foundation Only）

本 HD 覆盖：

| 类别             | 文件清单（位于 `src/core/Inkwell.Abstractions/`）                                                            |
| ---------------- | ------------------------------------------------------------------------------------------------------------ |
| Common Exception | `Common/InkwellException.cs`（仅 `InkwellConfigurationException` / `InkwellBuilderException` 两子类）        |
| Common DTO       | `Common/Pagination.cs` / `Common/SortOrder.cs` / `Common/TimeRange.cs`                                       |
| Builder DSL      | `Builder/IInkwellBuilder.cs` / `Builder/InkwellBuilder.cs` / `Builder/InkwellServiceCollectionExtensions.cs` |
| Options          | `Options/InkwellOptions.cs` / `Options/InkwellOptionsValidator.cs`                                           |
| 端口接口公共约定 | 见 §5（命名 / 签名 / 错误处理 / 日志），不在本 HD 写具体接口                                                 |

不在本 HD 范围（拆到后续 HD）：

- HD-002 `IPersistenceProvider` 接口与 Entity 定义大轮廓（同步建立 `database-design.md` Inkwell.Abstractions 章节）
- HD-003 `IFileStorageProvider` 接口
- HD-004 `ICacheProvider` 接口
- HD-005 `IQueueProvider` 接口 + `MessageEnvelope` DTO（[RISK-015 traceparent 跨服务字段](../../03-architecture/risk-analysis.md)）
- HD-006 `IAgentRuntime` 接口
- HD-008 向量存储 type-alias 复用 + Builder DSL `UseQdrantVectorStore` / `UseInMemoryVectorStore`

### 1.3 关键决策摘要

| ID     | 决策                                                                                                                                               | 来源                                                                                                                                                                                                                                                                    |
| ------ | -------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ~~Q1~~ | ~~`Result<T>` / `Error` 自研轻量 readonly struct + record，**零三方包**~~                                                                          | [ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 后废止——业务命名空间不再需要 `Result<T>` 抽象，错误语义统一靠 BCL 异常表达 |
| ~~Q2~~ | ~~错误码 = `string ID` 格式 `INK-<MODULE>-<NNN>`~~                                                                                                 | [ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) 后废止——错误语义改走 BCL 异常类型表达，详 §4.1 / §5.3                                                                                                                |
| Q3     | Builder DSL = `IServiceCollection` extension（**唯一**）                                                                                           | picker 2026-05-10；[architecture.md §3](../../03-architecture/architecture.md) 与 5 条 ADR 现有示例                                                                                                                                                                     |
| Q4     | Options 全量走 [`IOptions<T>` 标准](https://learn.microsoft.com/aspnet/core/fundamentals/configuration/options) + `IValidateOptions<T>` 启动期校验 | picker 2026-05-10；[ADR-002 §DI](../../03-architecture/adr/ADR-002-backend-runtime-dotnet10-aspnetcore.md)                                                                                                                                                              |
| Q5     | DTO = `record class` 为主（`init` 不可变 + value equality）                                                                                        | picker 2026-05-10；.NET 6+ DTO 通用最佳实践                                                                                                                                                                                                                             |
| Q6     | 异步方法**必须**带 `CancellationToken ct = default`                                                                                                | picker 2026-05-10；[ADR-013 trace cancellation 联动](../../03-architecture/adr/ADR-013-observability-otel-self-hosted-grafana.md)                                                                                                                                       |

> 决策**复议**：6 条决策若需重开，发起新 ADR（不在本 HD 内翻盘），同步发起 HD-001 修订——本 HD `status: draft` 由人工评审签字后翻 `reviewed`，签字之前任意一条决策都可在 reviewer 反馈中调回。

## 2. 文件结构

```text
src/core/Inkwell.Abstractions/
  Inkwell.Abstractions.csproj          # 仅引用 Microsoft.Extensions.{DependencyInjection,Configuration,Options,Logging}.Abstractions
                                       #     + Microsoft.Extensions.VectorData.Abstractions (HD-008 起用)
                                       #     + Microsoft.Extensions.AI.Abstractions (HD-008 起用，2026-07-06 errata·第六轮 B15)
  GlobalUsings.cs                      # 项目级 global using——详 §14.3 / [docs-style](../../../.github/instructions/coding-discipline.instructions.md)
  Common/
    InkwellException.cs                # 仅 InkwellConfigurationException + InkwellBuilderException 两个程序错误子类，直继 System.Exception
    Pagination.cs                      # record class Pagination(int Page, int PageSize)
    SortOrder.cs                       # record class SortOrder(string Field, SortDirection Direction)
    TimeRange.cs                       # record class TimeRange(DateTimeOffset Start, DateTimeOffset End)
  Builder/
    IInkwellBuilder.cs                 # public interface IInkwellBuilder
    InkwellBuilder.cs                  # internal sealed class，AddInkwell() 内部实例
    InkwellServiceCollectionExtensions.cs   # public static class，AddInkwell() / Build() 入口
  Options/
    InkwellOptions.cs                  # 根 Options，含 ServiceName / Environment / 子 Options 引用
    InkwellOptionsValidator.cs         # IValidateOptions<InkwellOptions>
```

> `Inkwell.Abstractions.csproj` 的 `<TargetFramework>` 与 `<LangVersion>` 跟 [`Directory.Build.props`](../../../) 全局设置一致（.NET 10 + C# 14；[ADR-002](../../03-architecture/adr/ADR-002-backend-runtime-dotnet10-aspnetcore.md)）。`Nullable` 启用、`TreatWarningsAsErrors` 启用。**严禁**在本 csproj 引用 `Microsoft.Agents.AI.*` / `StackExchange.Redis` / `Azure.Storage.Blobs` / `Microsoft.EntityFrameworkCore.*` / `Npgsql.*` / `Minio.*`（[ADR-017 §依赖规则](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + Roslyn `BannedSymbols.txt` CI 强制）。

## 3. 程序文件设计（10 字段 × 10 文件）

### 3.3 `Common/InkwellException.cs`

> **设计说明**（[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)）：本文件**仅**定义 `InkwellConfigurationException` / `InkwellBuilderException` 两个程序错误子类，直继 [`System.Exception`](https://learn.microsoft.com/dotnet/api/system.exception)，无 `Code` 字段、无基类。端口层业务失败走 [.NET BCL 异常类型](https://learn.microsoft.com/dotnet/standard/exceptions/)（详 §5.3），不走本文件。

| 字段         | 内容                                                                                                                                                                                                                                                                                                              |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Common/InkwellException.cs`                                                                                                                                                                                                                                                        |
| 职责         | **程序错误路径专用**——DI 装配错误 / Options 校验失败 / Builder 链不一致两类状况；业务失败不走本文件（走 BCL 异常，详 §5.3）                                                                                                                                                                                       |
| 对外接口     | `public sealed class InkwellConfigurationException(string message, Exception? inner = null) : Exception(message, inner) { }`；`public sealed class InkwellBuilderException(string message, Exception? inner = null) : Exception(message, inner) { }`——不设公共基类，两个类直继 `System.Exception`；无 `Code` 字段 |
| 内部函数或类 | 无（极简两 ctor，不重写 `ToString`，依靠 BCL 默认格式）                                                                                                                                                                                                                                                           |
| 输入数据     | `message` / 可选 `inner`                                                                                                                                                                                                                                                                                          |
| 输出数据     | Exception 实例                                                                                                                                                                                                                                                                                                    |
| 依赖模块     | `System.*`                                                                                                                                                                                                                                                                                                        |
| 错误处理     | 本文件不产生进一步异常。`message` / `inner` 传递到 BCL `Exception` ctor，其错误处理走 BCL 默认（如 `null` message 不报错、仅使 `Message` 为默认描述串）                                                                                                                                                           |
| 日志要求     | catch 此异常时按 §4.2 写 OTel `exception.type=Inkwell.Abstractions.InkwellConfigurationException`（或 `...InkwellBuilderException`） + `exception.message` + `exception.stacktrace`                                                                                                                               |
| 测试要求     | `InkwellExceptionTests.cs`：两子类可实例化；`inner` 链路保留（`(new InkwellConfigurationException("msg", new IOException("db"))).InnerException.Should().BeOfType<IOException>()`）；不需测试 `Code` 字段（字段不存在）                                                                                           |

### 3.4 `Common/Pagination.cs`

| 字段         | 内容                                                                                                                                                                                                  |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Common/Pagination.cs`                                                                                                                                                  |
| 职责         | 分页参数 DTO，跨 `IPersistenceProvider` / 业务 List 接口 / Public API（[ADR-007](../../03-architecture/adr/ADR-007-public-api-token-auth.md)） 复用                                                   |
| 对外接口     | `public sealed record Pagination(int Page, int PageSize) { public const int DefaultPageSize = 20; public const int MaxPageSize = 100; public static Pagination Default => new(1, DefaultPageSize); }` |
| 内部函数或类 | 构造期校验 `Page >= 1` / `PageSize in [1, MaxPageSize]`，违反抛 `ArgumentOutOfRangeException`                                                                                                         |
| 输入数据     | `Page` (1-based) / `PageSize`                                                                                                                                                                         |
| 输出数据     | `Pagination` 实例                                                                                                                                                                                     |
| 依赖模块     | System.*                                                                                                                                                                                              |
| 错误处理     | `Page < 1` / `PageSize < 1` / `PageSize > 100` → `ArgumentOutOfRangeException`                                                                                                                        |
| 日志要求     | DTO 自身不做日志；调用方在分页查询日志中输出 `pagination.page` / `pagination.pageSize`                                                                                                                |
| 测试要求     | `PaginationTests.cs`：边界值（Page=1, PageSize=1, PageSize=100）、越界值（Page=0, PageSize=101）、`Default` 一致性                                                                                    |

### 3.5 `Common/SortOrder.cs`

| 字段         | 内容                                                                                                                                                                                                                         |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Common/SortOrder.cs`                                                                                                                                                                          |
| 职责         | 排序参数 DTO；List 接口默认按 `CreatedAtUtc DESC` 反过来由调用方覆写                                                                                                                                                         |
| 对外接口     | `public enum SortDirection { Ascending, Descending }`；`public sealed record SortOrder(string Field, SortDirection Direction) { public static SortOrder ByCreatedAtDesc => new("CreatedAtUtc", SortDirection.Descending); }` |
| 内部函数或类 | 构造期校验 `Field` 非空；不在 Abstractions 层做白名单（白名单由各 Provider HD 自定义）                                                                                                                                       |
| 输入数据     | `Field` / `Direction`                                                                                                                                                                                                        |
| 输出数据     | `SortOrder` 实例                                                                                                                                                                                                             |
| 依赖模块     | System.*                                                                                                                                                                                                                     |
| 错误处理     | `Field` 为 null/empty/whitespace → `ArgumentException`                                                                                                                                                                       |
| 日志要求     | 调用方在查询日志中输出 `sort.field` / `sort.direction`                                                                                                                                                                       |
| 测试要求     | `SortOrderTests.cs`：构造校验、`ByCreatedAtDesc` 一致性、record equality                                                                                                                                                     |

### 3.6 `Common/TimeRange.cs`

| 字段         | 内容                                                                                                                                                                |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Common/TimeRange.cs`                                                                                                                 |
| 职责         | 时间区间 DTO；用量统计 / Trace 查询 复用；统一使用 [`DateTimeOffset`](https://learn.microsoft.com/dotnet/api/system.datetimeoffset)（带时区）                       |
| 对外接口     | `public sealed record TimeRange(DateTimeOffset Start, DateTimeOffset End) { public TimeSpan Duration => End - Start; public bool Contains(DateTimeOffset point); }` |
| 内部函数或类 | 构造期校验 `Start <= End`，违反抛 `ArgumentException`；`Contains` 半开区间 `[Start, End)`                                                                           |
| 输入数据     | `Start` / `End`                                                                                                                                                     |
| 输出数据     | `TimeRange` 实例；`Duration` / `Contains` 派生值                                                                                                                    |
| 依赖模块     | System.*                                                                                                                                                            |
| 错误处理     | `Start > End` → `ArgumentException`                                                                                                                                 |
| 日志要求     | 调用方在查询日志中输出 `range.start` / `range.end`（ISO-8601）                                                                                                      |
| 测试要求     | `TimeRangeTests.cs`：边界（Start == End 允许）、Start > End 异常、`Contains` 半开区间、跨时区两端等价（同 Instant）                                                 |

### 3.7 ~~`Common/AuditContext.cs`~~（已删除）

> **2026-07-09 决策更新**：Owner 决定 v1 不做审计日志功能，`Common/AuditContext.cs`（原承载"谁在什么时间从什么入口做的什么"，专供已删除的 `IAuditLogger` 消费）已整体删除，不再有对应文件。本节编号保留占位，不重排后续小节。

### 3.8 `Builder/IInkwellBuilder.cs`

| 字段         | 内容                                                                                                                                                                                                              |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Builder/IInkwellBuilder.cs`                                                                                                                                                        |
| 职责         | Builder DSL 的核心接口，承载 Provider 链式装配；公开属性是 Provider 扩展方法的"挂钩点"                                                                                                                            |
| 对外接口     | `public interface IInkwellBuilder { IServiceCollection Services { get; } IConfiguration Configuration { get; } IInkwellBuilder ConfigureOptions(Action<InkwellOptions> configure); IServiceCollection Build(); }` |
| 内部函数或类 | 接口本身无函数；`Services` / `Configuration` 由 `AddInkwell()` 注入；`Build()` 触发 Options 校验 + 必备 Provider 兜底注册                                                                                         |
| 输入数据     | `IServiceCollection`（注册 DI） / `IConfiguration`（读 `appsettings.json`）                                                                                                                                       |
| 输出数据     | `IServiceCollection`（`Build()` 返回，允许继续链 `.AddXxx()`）                                                                                                                                                    |
| 依赖模块     | `Microsoft.Extensions.DependencyInjection.Abstractions` / `Microsoft.Extensions.Configuration.Abstractions` / `Common/InkwellException.cs` / `Options/InkwellOptions.cs`                                          |
| 错误处理     | `Build()` 检测必备端口未注册（如 `IPersistenceProvider`）→ `throw new InkwellBuilderException("未注册 IPersistenceProvider；请调用 .UseSqlServer/.UsePostgres 之一")`                                             |
| 日志要求     | 不直接打日志（DI 装配阶段无 `ILogger`）；通过抛 `InkwellException` 让 host `Program.cs` 兜底打 fatal                                                                                                              |
| 测试要求     | `IInkwellBuilderContractTests.cs`：契约测试（验证接口形态稳定）；具体行为测试在 `InkwellBuilderTests.cs`                                                                                                          |

### 3.9 `Builder/InkwellBuilder.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Builder/InkwellBuilder.cs`                                                                                                                                                                                                                                                                                                           |
| 职责         | `IInkwellBuilder` 的**唯一**内部实现；累积 Use*/Configure* 调用，到 `Build()` 时统一校验 + 注入                                                                                                                                                                                                                                                                     |
| 对外接口     | `internal sealed class InkwellBuilder : IInkwellBuilder`；构造仅供 `InkwellServiceCollectionExtensions.AddInkwell` 调用（`internal` 可见）                                                                                                                                                                                                                          |
| 内部函数或类 | `private readonly List<Action<InkwellOptions>> _optionsConfigurators` 累积链；`Build()` 内：(1) 应用所有 `_optionsConfigurators`；(2) 注册 `IValidateOptions<InkwellOptions>`；(3) 检测必备端口（`IPersistenceProvider` / `ICacheProvider` / `IQueueProvider` / `IFileStorageProvider` / `IAgentRuntime`）；(4) 缺失则抛 `InkwellBuilderException`                  |
| 输入数据     | 构造接收 `IServiceCollection` + `IConfiguration`                                                                                                                                                                                                                                                                                                                    |
| 输出数据     | `Build()` 返回 `IServiceCollection`（链可续）                                                                                                                                                                                                                                                                                                                       |
| 依赖模块     | `Common/InkwellException.cs` / `Options/InkwellOptions.cs` / `Microsoft.Extensions.DependencyInjection.Abstractions`                                                                                                                                                                                                                                                |
| 错误处理     | 重复 `Build()` 调用 → `throw new InvalidOperationException("AddInkwell().Build() 已调用，不可重入")`；缺必备端口见 §3.8                                                                                                                                                                                                                                             |
| 日志要求     | 同 §3.8（DI 装配期不打日志）                                                                                                                                                                                                                                                                                                                                        |
| 测试要求     | `InkwellBuilderTests.cs`：单次 Build 成功、二次 Build 抛错、缺端口抛错、Options 校验失败抛 `InkwellConfigurationException`、`ConfigureOptions` 链式累积顺序                                                                                                                                                                                                         |

### 3.10 `Builder/InkwellServiceCollectionExtensions.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                 |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Builder/InkwellServiceCollectionExtensions.cs`                                                                                                                                                                                                                                        |
| 职责         | 公开 `AddInkwell()` 静态扩展方法，**唯一**用户入口                                                                                                                                                                                                                                                                   |
| 对外接口     | `public static class InkwellServiceCollectionExtensions { public static IInkwellBuilder AddInkwell(this IServiceCollection services, IConfiguration configuration, string sectionName = "Inkwell"); public static IInkwellBuilder AddInkwell(this IServiceCollection services, Action<InkwellOptions> configure); }` |
| 内部函数或类 | 重载 1（Configuration 路径）：从 `configuration.GetSection(sectionName)` 绑定到 `InkwellOptions`；重载 2（程式化）：直接 `Action<InkwellOptions>` 配置；二者均 new `InkwellBuilder`、注册 `IOptions<InkwellOptions>`、返回 builder                                                                                   |
| 输入数据     | `IServiceCollection` + (`IConfiguration` 或 `Action<InkwellOptions>`)                                                                                                                                                                                                                                                |
| 输出数据     | `IInkwellBuilder`                                                                                                                                                                                                                                                                                                    |
| 依赖模块     | `Builder/InkwellBuilder.cs` / `Options/InkwellOptions.cs` / `Microsoft.Extensions.Configuration.Binder`                                                                                                                                                                                                              |
| 错误处理     | `services` 为 null → `ArgumentNullException`；`configuration` 为 null → `ArgumentNullException`；`sectionName` 不存在 → 不抛错（用空 Options 起步），但 `Build()` 时 `IValidateOptions` 会拒绝                                                                                                                       |
| 日志要求     | 不直接打日志                                                                                                                                                                                                                                                                                                         |
| 测试要求     | `InkwellServiceCollectionExtensionsTests.cs`：两个重载 happy path、`null` 参数守护、`sectionName` 缺失行为                                                                                                                                                                                                           |

### 3.11 `Options/InkwellOptions.cs`

> **2026-05-10 errata（F9 形态 C）**：根 Options 引入顶层 `Providers` 选择器段——子 Options 不再承载 `Provider` 字段；Provider 选择 / 一致性校验由 Builder DSL 在装配期对照 `Inkwell:Providers:<Module>` 完成（违反抛 `InkwellBuilderException`，消息包含冲突 Module 名）。

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Options/InkwellOptions.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| 职责         | 根 Options，承载全局设置 + Provider 选择器段 + 各端口子 Options 的引用槽位                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| 对外接口     | `public sealed class InkwellOptions { [Required] public string ServiceName { get; init; } = "inkwell"; [Required] public string Environment { get; init; } = "dev"; public InkwellProvidersOptions Providers { get; init; } = new(); public PersistenceOptions Persistence { get; init; } = new(); public FileStorageOptions FileStorage { get; init; } = new(); public CacheOptions Cache { get; init; } = new(); public QueueOptions Queue { get; init; } = new(); public AgentRuntimeOptions AgentRuntime { get; init; } = new(); public VectorStoreOptions VectorStore { get; init; } = new(); }`                                                         |
| 内部函数或类 | 子 Options 类（`PersistenceOptions` 等）由 HD-002 ~ HD-008 各自补全；本 HD 仅声明占位 `public sealed class XxxOptions { }`（空 record/class）；新增 `InkwellProvidersOptions`（§3.11.1）                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| 输入数据     | 由 `appsettings.json` `"Inkwell"` 段绑定（含 `"Inkwell:Providers"` 子段 + 各 `"Inkwell:<Module>"` 详细子段）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| 输出数据     | `InkwellOptions` 实例（DI 通过 `IOptions<InkwellOptions>` 注入）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| 依赖模块     | `System.ComponentModel.DataAnnotations`（`[Required]`）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| 错误处理     | 字段缺失 → `IValidateOptions` 校验失败抛 `OptionsValidationException`（外层 host catch 后转 `InkwellConfigurationException`）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| 日志要求     | DI 启动期 `IValidateOptions` 失败时由 host 打 fatal 日志                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| 测试要求     | `InkwellOptionsTests.cs`：默认值、`appsettings.json` 绑定、缺必填字段校验失败、子 Options 占位类可绑定空对象、`Providers` 选择器段绑定                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |

#### 3.11.1 `Options/InkwellProvidersOptions.cs`（F9 新增）

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Options/InkwellProvidersOptions.cs`                                                                                                                                                                                                                                                                                                                                                                                             |
| 职责         | Provider 选择器段；用值型字符串声明每个端口在当前部署下选用哪个 Provider，供 Builder DSL `.UseXxx()` 装配期交叉校验                                                                                                                                                                                                                                                                                                                                            |
| 对外接口     | `public sealed class InkwellProvidersOptions { [Required] public string Persistence { get; init; } = "PostgreSQL"; [Required] public string FileStorage { get; init; } = "LocalFileSystem"; [Required] public string Cache { get; init; } = "InMemory"; [Required] public string Queue { get; init; } = "Channels"; [Required] public string VectorStore { get; init; } = "InMemory"; [Required] public string AgentRuntime { get; init; } = "AzureOpenAI"; }` |
| 内部函数或类 | 字符串值由对应端口 HD（HD-002 ~ HD-008）锁定取值白名单（如 `Persistence ∈ {"SqlServer","PostgreSQL"}`，[ADR-004](../../03-architecture/adr/ADR-004-data-store-provider-switchable-ef-core.md)，2026-07-08 移除 InMemory 关系型 Provider）；本 HD 仅声明字段，校验逻辑在 Builder DSL 装配期                                                                                                                                                                     |
| 输入数据     | 由 `appsettings.json` `"Inkwell:Providers"` 段绑定                                                                                                                                                                                                                                                                                                                                                                                                             |
| 输出数据     | `InkwellProvidersOptions` 实例                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| 依赖模块     | `System.ComponentModel.DataAnnotations`                                                                                                                                                                                                                                                                                                                                                                                                                        |
| 错误处理     | 字段缺失 → `IValidateOptions` 校验失败；Builder DSL `.UseXxx()` 与 `Providers.<Module>` 取值不一致 / 同一 Module 注册两次 → `throw new InkwellBuilderException($"Provider registration conflict for {moduleName}: ...")`（详 [§3.3](#33-commoninkwellexceptioncs)）                                                                                                                                                                                            |
| 日志要求     | 装配期校验失败由 host 打 fatal；详细字段名 `providers.persistence` / `providers.queue` / 等用于 OTel resource 标签                                                                                                                                                                                                                                                                                                                                             |
| 测试要求     | `InkwellProvidersOptionsTests.cs`：默认值、`appsettings.json` 绑定、缺必填字段校验失败、所有 6 个字段都被绑定                                                                                                                                                                                                                                                                                                                                                  |

### 3.12 `Options/InkwellOptionsValidator.cs`

| 字段         | 内容                                                                                                                                                                                                           |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Options/InkwellOptionsValidator.cs`                                                                                                                                             |
| 职责         | `IValidateOptions<InkwellOptions>` 实现，启动期校验全局 + 子 Options 字段                                                                                                                                      |
| 对外接口     | `internal sealed class InkwellOptionsValidator : IValidateOptions<InkwellOptions> { public ValidateOptionsResult Validate(string? name, InkwellOptions options); }`                                            |
| 内部函数或类 | 内部走 `Validator.TryValidateObject`（DataAnnotations）+ 自定义跨字段规则（如 `Environment` ∈ `{dev, staging, prod}`）；子 Options 的细规则委托给各端口 `IValidateOptions<XxxOptions>`（HD-002 ~ HD-007 实现） |
| 输入数据     | `InkwellOptions` 实例                                                                                                                                                                                          |
| 输出数据     | `ValidateOptionsResult.Success` / `ValidateOptionsResult.Fail(IEnumerable<string>)`                                                                                                                            |
| 依赖模块     | `Microsoft.Extensions.Options` / `System.ComponentModel.DataAnnotations`                                                                                                                                       |
| 错误处理     | 校验失败 → `ValidateOptionsResult.Fail` 含全部错误消息（不抛异常，由 IOptions 链路转）                                                                                                                         |
| 日志要求     | 失败消息会被 `OptionsValidationException` 抛出，host 兜底打 fatal                                                                                                                                              |
| 测试要求     | `InkwellOptionsValidatorTests.cs`：成功路径、`Environment` 越界、`ServiceName` 空、子 Options 校验失败的消息汇总                                                                                               |

## 4. 错误与日志公共约定

### 4.1 错误表达机制

- 端口层错误语义**仅靠 .NET BCL 异常类型表达**，Inkwell 不自建异常类、不定义错误码表、不自建 `Result<T>` / `Error` 抽象。同一类内不同业务语义靠 `Message` 区分（如 "file not found: /var/inkwell/uploads/abc.png" vs "trigger config not found: id=trg-123" 都是 [`FileNotFoundException`](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)，靠 `Message` 区分）
- 业务失败 → BCL 对照表（§5.3 集中定义），调用方按异常类型多态 catch
- 跨进程序列化（如 [`MessageEnvelope`](../../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) RISK-015 traceparent）需传递异常信息时，仅传 OTel 五字段（§4.2），不跨进程重建 Exception 实例
- **业务命名空间（`Inkwell.Core.*`）与端口层遵同一机制**——全走 BCL 异常，不依赖 `Result<T>` / `Error` 抽象。需要返回多项错误时（如批量校验）走 [`ValidationResult`](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.validationresult) / `IEnumerable<string>` 等 BCL 对症抽象；这类场景在业务 HD 起草时判定、不在 HD-001 锁

### 4.2 日志结构化字段

任何端口实现 / 业务消费方在写错误日志时**必须**输出 [OTel 标准 `exception.*` 五字段](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)（[ADR-013](../../03-architecture/adr/ADR-013-observability-otel-self-hosted-grafana.md)）：

| 字段                   | 来源                                     | 备注                                                                                                                        |
| ---------------------- | ---------------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `exception.type`       | `ex.GetType().FullName`                  | 必输出（如 `System.IO.FileNotFoundException` / `Azure.RequestFailedException`）——Loki / Grafana 按此字段过滤错误种类        |
| `exception.message`    | `ex.Message`                             | 必输出                                                                                                                      |
| `exception.stacktrace` | `ex.ToString()` 或 `ex.StackTrace`       | 必输出（[ADR-013](../../03-architecture/adr/ADR-013-observability-otel-self-hosted-grafana.md) 锁 OTel）                    |
| `exception.escaped`    | 是否传到 span 外                         | 可选。OTel 语义为 true 表示未被 catch                                                                                       |
| `exception.id`         | `Activity.Current?.RecordException` 生成 | 可选。在 OTel SDK 自动注入                                                                                                  |
| `trace.id` / `span.id` | OTel `Activity.Current`                  | 必输出                                                                                                                      |
| `service.name`         | OTel resource                            | [ADR-019](../../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) 锁 `inkwell-webapi` / `inkwell-worker` |

### 4.3 取消传播

- 异步方法**必须**接受 `CancellationToken ct = default`（picker Q6）
- 取消触发 → 抛 [`OperationCanceledException`](https://learn.microsoft.com/dotnet/api/system.operationcanceledexception) / [`TaskCanceledException`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.taskcanceledexception)（**不**走 `Result.Failure`——取消不是业务失败）
- WebApi 路径：ASP.NET Core 自动注入 [`HttpContext.RequestAborted`](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/parameter-binding) 给端点参数；端点把它转给 service 层
- Worker 路径：`BackgroundService.ExecuteAsync(CancellationToken)` 接到的 token 顺着调用链传下去

## 5. 6 大端口接口公共约定（纲领 / 不写具体接口）

> 具体接口形态由 HD-002 ~ HD-008 起草。本节仅锁定**所有端口共享的命名 / 签名 / 错误处理 / 日志规则**，让后续 HD 直接复用。

### 5.1 命名

- 接口形如 `I<Capability>Provider`（基础设施端口）/ `I<Module>Service`（业务端口）
- 异步方法以 `Async` 结尾
- 请求 DTO `<Action><Entity>Request`，响应 DTO `<Action><Entity>Response`
- Options 命名 `<Provider>Options`（`PersistenceOptions` / `FileStorageOptions` / ...）

### 5.2 签名

> **2026-05-11 ADR-023 翻转**：端口层（`IXxxProvider` / `IXxxService`）签名采用裸 `Task<T>` + 异常，禁 `Task<Result<T>>` / `Result<T>` 包装；详 [ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)。

- 异步签名（返回数据）：`Task<TResponse> XxxAsync(TRequest request, CancellationToken ct = default)`
- 异步签名（无返回值）：`Task XxxAsync(...)`
- 异步签名（自解释 `bool`）：`Task<bool> XxxAsync(...)`——查询 / 幂等 `Delete` / `Exists` 类
- 流式签名：`IAsyncEnumerable<T> XxxAsync(..., [EnumeratorCancellation] CancellationToken ct = default)`
- 同步签名（仅 in-memory 操作）：`T Xxx(...)` / `void Xxx(...)`
- **调用方语义约定**（命名前缀决定失败语义）：
  - `Find*Async` → `Task<T?>`：实体不存在 = `null`，不抛
  - `Get*Async` → `Task<T>`：实体不存在抛 `KeyNotFoundException`
  - `Exists*Async` → `Task<bool>`：仅查询，网络故障抛 `IOException`
  - `Delete*Async` → `Task<bool>`：幂等（`true` = 实际删除 / `false` = 本不存在）
  - `List*Async` → `IAsyncEnumerable<T>` 或 `Task<PagedResult<T>>`
  - `Create*Async` / `Update*Async` / `Upload*Async` / ... → `Task<T>`，失败抛 `InkwellException`
- **禁止**：用 `null` 代表“失败”（`null` 仅在 `Find*Async` 表“不存在”语义）；端口层签名出现 `Task<Result<T>>` / `Result<T>` 包装（违反 [ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)，CI grep `rg -n 'Task<Result<' src/core/Inkwell.Abstractions/` 期望 0 行）

### 5.3 错误处理

- **业务失败** → 按语义选 [BCL 异常类型](https://learn.microsoft.com/dotnet/standard/exceptions/)：
  - 实体 / key 不存在 → [`KeyNotFoundException`](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)（适用于 cache / queue / store 查不到） / [`FileNotFoundException`](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception)（适用于 FileStorage 对象不在）
  - 唯一约束冲突 / 状态错误 → [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)
  - 参数违反 → [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception) / [`ArgumentNullException`](https://learn.microsoft.com/dotnet/api/system.argumentnullexception) / [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)
  - 未授权 / 身份不足 → [`UnauthorizedAccessException`](https://learn.microsoft.com/dotnet/api/system.unauthorizedaccessexception)
  - 不支持的操作 → [`NotSupportedException`](https://learn.microsoft.com/dotnet/api/system.notsupportedexception)
  - 超时 → [`TimeoutException`](https://learn.microsoft.com/dotnet/api/system.timeoutexception)
  - I/O 故障（网络断 / 远端 5xx / 磁盘错误）→ [`IOException`](https://learn.microsoft.com/dotnet/api/system.io.ioexception)；SDK 提供表达性子类时优先（如 [`Azure.RequestFailedException`](https://learn.microsoft.com/dotnet/api/azure.requestfailedexception) / [`StackExchange.Redis.RedisConnectionException`](https://stackexchange.github.io/StackExchange.Redis/) / [`Npgsql.PostgresException`](https://www.npgsql.org/doc/api/Npgsql.PostgresException.html)）原样上抩
- **程序错误（DI / Builder 专用）** → `throw new InkwellConfigurationException(message, inner?)` 或 `throw new InkwellBuilderException(message, inner?)`（详 [§3.3](#33-commoninkwellexceptioncs)；两个子类直继 `System.Exception`，无 `Code` 字段）
- **Options 校验失败** → `IValidateOptions` 返回 `ValidateOptionsResult.Fail`，host 启动期自动抛 [`OptionsValidationException`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.options.optionsvalidationexception)
- **取消** → `OperationCanceledException`（按 [BCL 惯例](https://learn.microsoft.com/dotnet/standard/threading/cancellation-in-managed-threads) 不包装，见 §4.3）
- **端口实现不得吞底层异常**——能原样上抩就上抩（SDK 异常本身已表达充分）；需要语义翻译时 `try/catch` 转为上表 BCL 类型乶在 `inner` 保原始异常（OTel `exception.stacktrace` 自动展开链，[ADR-013](../../03-architecture/adr/ADR-013-observability-otel-self-hosted-grafana.md)）
- **调用方 catch 按异常类型多态分流**（如 `catch (FileNotFoundException ex)` / `catch (TimeoutException ex)`）；同类内不同业务语义靠 `ex.Message` / `ex.InnerException.GetType()` 区分

### 5.4 Options 注册

- 每个端口子 Options 通过 `InkwellOptions.<Module>` 槽位绑定（如 `InkwellOptions.Persistence`）
- 每个端口实现负责注册自己的 `IValidateOptions<XxxOptions>`
- 配置段位置统一 `appsettings.json` 顶层 `"Inkwell"` 下子段（如 `"Inkwell:Persistence"` / `"Inkwell:Cache"`）

## 6. Builder DSL 使用模式

### 6.1 典型用法（与 [architecture.md §3](../../03-architecture/architecture.md) 示例对齐）

```csharp
// Inkwell.WebApi/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInkwell(builder.Configuration)
    .UseSqlServer(builder.Configuration.GetConnectionString("Inkwell"))   // HD-002 提供
    .UseRedisCache(builder.Configuration.GetConnectionString("Redis"))    // HD-004 提供
    .UseRedisQueue(builder.Configuration.GetConnectionString("Redis"))    // HD-005 提供
    .UseAzureBlobFileStorage(builder.Configuration.GetSection("Inkwell:FileStorage:AzureBlob"))  // HD-003 提供
    .UseQdrantVectorStore(builder.Configuration.GetConnectionString("Qdrant"))  // HD-008 提供
    .UseAzureOpenAIAgentRuntime(builder.Configuration.GetSection("Inkwell:AgentRuntime"))  // HD-006 提供
    .Build();

var app = builder.Build();
```

### 6.2 生命周期

- `AddInkwell()` 创建一个 `InkwellBuilder` 实例（不入 DI 容器，仅返回给链上）
- 每个 `Use*()` 扩展方法**只**调用 `builder.Services.AddXxx()` 注册服务，返回 `IInkwellBuilder`
- `Build()` 触发：(1) Options 校验 (2) 必备端口存在性检查 (3) 返回 `IServiceCollection`
- **`Build()` 不可重入**——重复调用抛 `InvalidOperationException`
- Provider 切换由 `appsettings.json` 不同环境的不同 `Use*` 调用决定，**不在运行期切换**

### 6.3 Provider 扩展方法约定（给 HD-002 ~ HD-008 的契约）

- 每个 Provider csproj 提供**唯一**入口扩展：`public static IInkwellBuilder UseXxx(this IInkwellBuilder builder, ...)`
- 扩展方法**必须**：(1) 校验入参非 null；(2) 调用 `builder.Services.AddXxx()` 注册接口实现；(3) 注册自己的 `IValidateOptions<XxxOptions>`；(4) 返回 `builder`
- 同一接口的多个 Provider（如 `IPersistenceProvider` 三 Provider）**互斥注册**——后调用者覆盖前调用者；`Build()` 不会校验顺序，由用户 Program.cs 自己保证

## 7. 性能 / 安全 / 可观测性

- **性能**：本 HD 不引入 hot-path 抽象（`InkwellException` 两子类仅在 DI 装配期抛出，不在调用热路径）。**性能预算 picker 不在 HD-001 范围**——业务接口 SLA 由各业务 HD 起草时锁定，参照 [requirements.md NFR](../../01-requirements/requirements.md)。
- **安全**：HD-001 本身不处理敏感字段。调用方写日志前应过滤敏感字段。
- **可观测性**：错误语义靠 OTel `exception.type` 字段表达（[ADR-013](../../03-architecture/adr/ADR-013-observability-otel-self-hosted-grafana.md)）；Grafana 告警可按 `exception.type = "System.IO.FileNotFoundException"` 等 BCL 类型名维度聚合

## 8. 测试要求

### 8.1 单元测试

- 测试项目：`tests/core/Inkwell.Abstractions.Tests/`（[MSTest.Sdk 4.x](https://github.com/microsoft/testfx) + MTP runner，[ADR-019 + tech-selection §18](../../03-architecture/tech-selection.md)）
- 每个文件至少一个 `*Tests.cs` 配对（见 §3 各小节"测试要求"）
- 覆盖率门槛：`Common/*` ≥ 95%，`Builder/*` ≥ 90%，`Options/*` ≥ 85%

### 8.2 契约测试

- 接口 ABI 契约用 [`PublicApiAnalyzers`](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) 锁定（v1 之后接口任何破坏性变更都会被 CI 拦截）
- `IInkwellBuilder` 形态变更 → 需新建 ADR + 影响所有 Provider HD

### 8.3 集成测试

- HD-001 范围**不**起集成测试（无外部依赖）；端口集成测试由 HD-002 ~ HD-008 起草

## 9. 部署 / 配置

- `Inkwell.Abstractions.csproj` 与 `Inkwell.Core` / 全部 `providers/*` 一同打 Docker 镜像（[ADR-005](../../03-architecture/adr/ADR-005-deployment-docker-compose-aks.md) + [ADR-019](../../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md)），无独立部署
- `appsettings.json` 顶层 `"Inkwell"` 段示例（**形态 C**：选择器集中 + 详细独立）：

```json
{
  "Inkwell": {
    "ServiceName": "inkwell-webapi",
    "Environment": "prod",
    "Providers": {
      "Persistence":  "PostgreSQL",
      "FileStorage":  "AzureBlob",
      "Cache":        "Redis",
      "Queue":        "Redis",
      "VectorStore":  "Qdrant",
      "AgentRuntime": "AzureOpenAI"
    },
    "Persistence":  { "/* 由 HD-002 锁定（不含 Provider 字段） */": null },
    "FileStorage":  { "/* 由 HD-003 锁定（不含 Provider 字段） */": null },
    "Cache":        { "/* 由 HD-004 锁定（不含 Provider 字段） */": null },
    "Queue":        { "/* 由 HD-005 锁定（不含 Provider 字段） */": null },
    "AgentRuntime": { "/* 由 HD-006 锁定（不含 Provider 字段） */": null }
  }
}
```

- **Builder DSL 装配期校验**（F9）：每个 `.UseXxx()` 扩展方法读取 `IConfiguration.GetSection("Inkwell:Providers:<Module>")` 与自身名称交叉比对——不一致 / 同一 Module 注册两次 → `throw new InkwellBuilderException($"Provider registration conflict for {moduleName}: ...")`。

## 10. 决策记录（Picker 拍板）

| 字段                    | 选定值                                         | Picker 时间 | 选项来源证据                                                                                                                                                                                                                                                                                                                                         |
| ----------------------- | ---------------------------------------------- | ----------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ~~Q1 `Result<T>` 形态~~ | ~~A 自研轻量 readonly struct + Error record~~  | 2026-05-10  | [ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 后废止——业务命名空间不再需要 `Result<T>` 抽象                                                                                                           |
| ~~Q2 错误码 ID 风格~~   | ~~A 全局 string ID，`INK-<MODULE>-<NNN>`~~     | 2026-05-10  | [ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md) 后废止——错误语义走 BCL 异常类型                                                                                                                                                                                                                   |
| Q3 Builder DSL 入口     | A `IServiceCollection` extension（唯一）       | 2026-05-10  | [architecture.md §3 示例](../../03-architecture/architecture.md) + [ADR-018](../../03-architecture/adr/ADR-018-queue-abstraction-channels-default.md) / [ADR-019](../../03-architecture/adr/ADR-019-process-topology-webapi-worker-split.md) / [ADR-020](../../03-architecture/adr/ADR-020-vector-store-microsoft-extensions-vectordata.md) 已写示例 |
| Q4 Options 注册         | A 全量走 `IOptions<T>` + `IValidateOptions<T>` | 2026-05-10  | [ADR-002 §DI](../../03-architecture/adr/ADR-002-backend-runtime-dotnet10-aspnetcore.md)                                                                                                                                                                                                                                                              |
| Q5 DTO 风格             | A `record class` 为主（`init` 不可变）         | 2026-05-10  | .NET 6+ DTO 通用最佳实践                                                                                                                                                                                                                                                                                                                             |
| Q6 取消令牌             | A 必须带 `CancellationToken ct = default`      | 2026-05-10  | [ADR-013](../../03-architecture/adr/ADR-013-observability-otel-self-hosted-grafana.md) trace cancellation 联动                                                                                                                                                                                                                                       |

## 11. 待补 / 后续 HD 衔接

- 本 HD 不锁定子 Options 字段——`PersistenceOptions` / `FileStorageOptions` / `CacheOptions` / `QueueOptions` / `AgentRuntimeOptions` 内容由对应端口 HD-002 ~ HD-006 锁定
- 本 HD 不锁定向量存储 Builder DSL 钩子——`UseQdrantVectorStore` / `UseInMemoryVectorStore` / `UseAzureOpenAIEmbeddings` 由 HD-008 锁定
- 本 HD 不锁定业务命名空间错误语义——[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流) + [errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 后业务命名空间错误语义全走 BCL 异常类型（§5.3 集中列表）；零 `Result<T>` / `Error` 抽象、零错误码表。需返回多项错误场景（如批量校验）走 [`ValidationResult`](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.validationresult) / `IEnumerable<string>` 等 BCL 对症抽象，不锁在本 HD
- 本 HD 不锁定性能预算数字——业务接口 SLA 由业务 HD 锁定，参照 [requirements.md NFR](../../01-requirements/requirements.md)
- 待补：`Inkwell.Abstractions` 命名空间下是否需要分析器（Roslyn analyzer）来 `BannedSymbols.txt` 验证 `Inkwell.Core.*` 不出现 `using Microsoft.Agents.AI.*` —— 本设计建议**需要**，但 analyzer csproj 单独走 HD-XXX 起草（不在 Foundation 范围）
- **F9 联动**：每个 `Use*` 扩展方法（HD-002 ~ HD-008）必须读取 `IConfiguration.GetSection("Inkwell:Providers:<Module>")` 做装配期交叉校验，违反抛 `InkwellBuilderException`；HD-002 ~ HD-008 起草时在自己的 §6 Builder DSL 衔接补 boilerplate 实现

## 12. 同步追加跨模块文件

- [`docs/04-detailed-design/file-structure.md`](../file-structure.md) — 首次创建 + 追加 `## Inkwell.Abstractions` 章节，详见该文件

## 13. Errata 记录

### 2026-05-10 首轮变更

本 HD `status: draft` 期间，根据 [`design-review-report.md` §6.4](../design-review-report.md) Owner 二次答复一次性落以下变更（已嵌入 §3 / §9 / §11，本节是变更摘要）：

- **F2 时间字段命名**：`AuditContext.OccurredAtUtc` → `OccurredTime`（已随 `AuditContext.cs` 一并删除，本条保留作为历史记录）
- **F9 InkwellOptions 形态 C**：
  - §3.11 `InkwellOptions` 增加 `Providers` 字段引用新建 §3.11.1 `InkwellProvidersOptions`
  - 新增 §3.11.1 `Options/InkwellProvidersOptions.cs`，6 个端口选择器字段（Persistence / FileStorage / Cache / Queue / VectorStore / AgentRuntime）
  - 子 Options（HD-002 ~ HD-007）**移除** `Provider` 字段（在各 HD errata 中同步落地）
  - §9 appsettings.json 示例改写为 `Inkwell:Providers:*` 选择器 + `Inkwell:<Module>:*` 详细段并列结构
  - §11 增加 F9 联动指引

### 2026-05-11 第四轮变更——删 `Result<T>` / `Error` 抽象

**触发**：Owner 2026-05-11 会话中主动指令“`Result` 和 `Error` 两个类都不需要了”。Picker：`result-error-scope` = A（完全删除）+ `session-pause` = A（三轮规约翻新单向收紧、继续推进）。上游决策证据为 [**ADR-023 errata·02**](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常)（accepted by Inkwell 2026-05-11，本 HD 翻新与 ADR errata·02 起草在同会话往返完成）。

**变更清单**：

- **§2.2 文件树**：删 `Common/Result.cs` / `Common/Error.cs` 两行
- **§1.2 文件清单**：“Common Result/Error” 行 → “Common Exception”行（仅 `InkwellException.cs`）
- **§1.3 / §10 Q1 决策**：~~自研轻量 readonly struct + Error record~~ strikethrough + 标 ADR-023 errata·02 后废止
- **§3.1 / §3.2 整段删除**：`Common/Result.cs` / `Common/Error.cs` 10 字段表不再存在；§3 标题不变但“10 字段 × 12 文件” → “10 字段 × 10 文件”（§3.3 ~ §3.12 编号不调整以保追溯追跟不断；§3.1 / §3.2 作为“已废”锁位保留不重用）
- **§4.1 第四条**：“业务命名空间可选使用 `Result<T>` / `Error`” → “与端口层遵同一机制，全走 BCL 异常”；多项错误场景走 [`ValidationResult`](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.validationresult)
- **§5.3 末条**：删除 “`Result<T>` 工具保留——业务命名空间按需选用”条
- **§7 性能**：删 “`Result<T>` 是 readonly struct” 句；§7 安全条删 `Error.Context` PII 提示（`Error` 本身不存在）
- **§11 业务命名空间错误语义条**：改为 “零 `Result<T>` / `Error` 抽象、零错误码表”
- **§14.1 csproj→namespace 表**：`Common/Result.cs` 示例行 → `Common/Pagination.cs` 示例行（表达同样的“§子目录不入 namespace”语义）

**上游补齐落地**：[ADR-023 errata·02](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata02删-commonresultcs--commonerrorcs-抽象业务命名空间错误处理一律-bcl-异常) 已由 `h2-architect-advisor` agent 在同会话切片落地（accepted by Inkwell 2026-05-11），本 HD §0 / §1.3 / §10 / §11 中 6 处“待 H2 起草”措辞已同步去除。下游待办：[HD-002](HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-003](HD-003-Inkwell.Abstractions-file-storage-port.md) 第二批 errata 在新会话切片处理；[HD-004 ~ HD-008](.) 直接用新规约起草。

### 2026-07-05 errata——§5.2 调用方语义约定遗留字面量翻新（B11）

**触发**：[design-review-report.md §14.3 B11](../design-review-report.md#b11hd-001-52-遗留-inkwellexceptionentitynotfound--inkwellexceptionconnectionfailed-字面量未随-adr-023-errata0102-同步c38)（HD-004 增量评审发现一致性冲突）——Owner picker 拍板修复路径为**选项 1**：§5.2 遗留的 `InkwellException(EntityNotFound)` / `InkwellException(ConnectionFailed)` 字面量是 [ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)（废错误码机制、改走 BCL 异常类型分流）之前的旧写法，未随本文件 §5.3 BCL 异常对照表同步翻新，二者自相矛盾。

**变更清单**：

- **§5.2 调用方语义约定**：`Get*Async` 实体不存在场景 `InkwellException(EntityNotFound)` → `KeyNotFoundException`；`Exists*Async` 网络故障场景 `InkwellException(ConnectionFailed)` → `IOException`（与 §5.3 BCL 异常对照表一致：实体 / key 不存在走 `KeyNotFoundException`，I/O 故障走 `IOException`）

本次为 errata 级修订，不改变 §5.3 既有决策，仅补齐 §5.2 遗漏的同步翻新；`status: reviewed` 不打回 `draft`。

### 2026-07-06 errata·第六轮——B15 对称纳入 `Microsoft.Extensions.AI.Abstractions` 白名单

**触发**：[design-review-report.md §18.3 B15](../design-review-report.md#b15q5比照-vectordata-先例缺物理落地机制iembeddinggenerator-依赖白名单例外未实际生效c91)（HD-008 增量评审发现一致性冲突 C91）——HD-008 §6 / §13 Q5 将 `IEmbeddingGenerator<string, Embedding<float>>` 允许业务命名空间直接注入的决策依据表述为“比照本 HD 自身对 `Microsoft.Extensions.VectorData.Abstractions` 的处理先例”，但 `Microsoft.Extensions.AI.Abstractions` 当时并未真正走过与 VectorData 相同的物理落地步骤（未纳入 `Inkwell.Abstractions.csproj` 白名单 + `GlobalUsings.cs`），若业务命名空间直接注入会被 CI `BannedSymbols.txt` 拦下。Owner 通过 chat picker 拍板修复方向为**选项 1**：把 `Microsoft.Extensions.AI.Abstractions` 也对称纳入 `Inkwell.Abstractions.csproj` 白名单 + `GlobalUsings.cs`，与 VectorData 处理完全同构，不触碰 `AGENTS.md`。

**变更清单**：

- **§1.1 拓扑依据 callout**：`Inkwell.Abstractions` 零外部包依赖例外清单追加 `Microsoft.Extensions.AI.Abstractions`
- **§2 文件结构**：`Inkwell.Abstractions.csproj` 注释追加 `+ Microsoft.Extensions.AI.Abstractions (HD-008 起用，2026-07-06 errata·第六轮 B15)`
- **§14.3 Global usings**：推荐例追加 `global using Microsoft.Extensions.AI;`

本次为 errata 级修订，不改变 §10 既有决策，仅补齐 HD-008 Q5 决策所需的物理落地机制；`status: reviewed` 不打回 `draft`。下游联动：[HD-008 §2 / §6 / §13 Q5](HD-008-Inkwell.Abstractions-vector-store-type-alias.md) + [file-structure.md `## Inkwell.Abstractions.VectorStore`](../file-structure.md#inkwellabstractionsvectorstore) + [design-review-report.md §18](../design-review-report.md#18-hd-008-vector-store-type-alias--builder-dsl-钩子首轮评审2026-07-06) 同会话切片同步处理。

## 14. 命名空间与代码风格约定（横切规约）

> **2026-05-11 新增**（Owner 拍板）：本节为跨 12 csproj 的代码风格横切规约，由 HD-001 作地基锁定；HD-002 ~ HD-008 及 providers/* 各自 HD 起草时**不重复本节内容**，仅引用 `[HD-001 §14](HD-001-Inkwell.Abstractions-foundation.md#14-命名空间与代码风格约定横切规约)`。下次会话补 ADR-024 作为根决策证据。

### 14.1 命名空间

| csproj                                           | 完整名                       | 命名空间                         | 示例                                                                                                                   |
| ------------------------------------------------ | ---------------------------- | -------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| `src/core/Inkwell.Abstractions/`                 | `Inkwell.Abstractions`       | **`Inkwell`**                    | `Common/Pagination.cs` 内 `namespace Inkwell;`（不是 `Inkwell.Abstractions.Common`）                                   |
| `src/core/Inkwell.Core/`                         | `Inkwell.Core`               | **`Inkwell`**                    | `Auth/AuthService.cs` 内 `namespace Inkwell;`（不是 `Inkwell.Core.Auth`）                                              |
| `src/core/providers/Inkwell.Persistence.EFCore/` | `Inkwell.Persistence.EFCore` | **`Inkwell.Persistence.EFCore`** | `Entities/AgentEntity.cs` 内 `namespace Inkwell.Persistence.EFCore;`（**不**是 `Inkwell.Persistence.EFCore.Entities`） |
| `src/core/providers/Inkwell.FileStorage.MinIO/`  | `Inkwell.FileStorage.MinIO`  | **`Inkwell.FileStorage.MinIO`**  | 任意子目录都平平落 `namespace Inkwell.FileStorage.MinIO;`                                                              |
| `src/core/Inkwell.WebApi/`                       | `Inkwell.WebApi`             | **`Inkwell.WebApi`**             | 不受子目录影响                                                                                                         |
| `src/core/Inkwell.Worker/`                       | `Inkwell.Worker`             | **`Inkwell.Worker`**             | 同上                                                                                                                   |

**规则小结**：

- `Inkwell.Abstractions` + `Inkwell.Core` 两个 csproj 里的所有类都使用 `namespace Inkwell;`（最扁；Owner 拍板）；不出现 `Inkwell.Abstractions.Common` / `Inkwell.Core.Agents` 等子命名空间
- 其他 csproj（`providers/*` / `Inkwell.WebApi` / `Inkwell.Worker`）**默认使用 csproj 名作命名空间**；子目录**不**跟进命名空间（如 `Inkwell.Persistence.EFCore/Entities/` 下的类仍是 `namespace Inkwell.Persistence.EFCore;`）
- 类名冲突额外准则：同名类在同一命名空间里冲突时，优先跟 [ADR-022](../../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md)“Model 默认无后缀 / 撞名降级 `XxxDefinition`”规则处理

### 14.2 File-scoped namespace

- 所有 `.cs` 文件一律使用 [File-scoped namespace](https://learn.microsoft.com/dotnet/csharp/language-reference/proposals/csharp-10.0/file-scoped-namespaces)（C# 10+）
  - ✅ 推荐：`namespace Inkwell;` （表后一个分号，类在顶格定义）
  - ❌ 禁止：`namespace Inkwell { ... }`（block-scoped namespace）
- 项目级强制：[`Directory.Build.props`](../../../) 设 `<LangVersion>14</LangVersion>` + [Roslyn analyzer IDE0161](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0161) `dotnet_style_namespace_match_folder = false`（因为 14.1 明确允许子目录不跟进命名空间）+ IDE0161 警告等级设 `error`使 block-scoped namespace 被 CI 拦住
- CI 自检：`rg -n '^\s*namespace\s+\w[\w.]*\s*\{' src/core/ providers/` 期望 0 行（无 block-scoped）

### 14.3 Global usings

- 每个 csproj **必须**提供一份 `GlobalUsings.cs`，集中声明高频 using。推荐例（`src/core/Inkwell.Abstractions/GlobalUsings.cs`）：

  ```csharp
  // Auto-applied to every .cs file in this csproj (C# 10+ global using)
  global using System;
  global using System.Collections.Generic;
  global using System.Linq;
  global using System.Threading;
  global using System.Threading.Tasks;
  global using Microsoft.Extensions.Configuration;
  global using Microsoft.Extensions.DependencyInjection;
  global using Microsoft.Extensions.Logging;
  global using Microsoft.Extensions.Options;
  global using Microsoft.Extensions.VectorData; // HD-008 起用：VectorStore / VectorStoreCollection<TKey, TRecord> 等类型全项目可用
  global using Microsoft.Extensions.AI; // HD-008 起用，2026-07-06 errata·第六轮（B15）：IEmbeddingGenerator<,> 等类型全项目可用，与 VectorData 同构
  ```

- 本 csproj由于是抽象层，**禁止**在 `GlobalUsings.cs` 中 `global using Microsoft.Agents.AI.*` / `StackExchange.Redis` / `Azure.Storage.Blobs` / `Microsoft.EntityFrameworkCore.*` / `Npgsql.*` / `Minio.*`（[ADR-017 §依赖规则](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md) + Roslyn `BannedSymbols.txt` CI 强制）
- `Inkwell.Core/GlobalUsings.cs` 与本 csproj 同样是抽象层纪律；**仅** `Inkwell.Core.AgentRuntime` 命名空间（唯一允许 `using Microsoft.Agents.AI.*` 的位置，[ADR-017](../../03-architecture/adr/ADR-017-backend-module-topology-ports-and-adapters.md)）**才可**在**该命名空间的文件级**加 `using Microsoft.Agents.AI.*`——**不追到 `GlobalUsings.cs`**，避免泄露到全 csproj
- CI 自检：`rg -n 'using Microsoft\.Agents\.AI' src/core/Inkwell.Core/GlobalUsings.cs` 期望 0 行；`rg -n '^global using ' src/core/Inkwell.Abstractions/GlobalUsings.cs` 期望 ≥ 1 行作为“GlobalUsings 文件存在”证据

### 14.4 联动章节

- [§2 文件结构](#2-文件结构)：已加 `GlobalUsings.cs` 行
- [§3 程序文件设计](#3-程序文件设计10-字段--10-文件)：各文件 10 字段表不重复声明 `namespace Inkwell;`，默认遵§14.1
- [file-structure.md 总体拓扑](../file-structure.md)：顶部加一行“命名空间规约参 HD-001 §14”引用
- [§14.6 文件编码与换行符](#146-文件编码与换行符)：仓库根级配置（`.editorconfig` + `.gitattributes`）锁定 UTF-8 + LF，与本 csproj 文件拓扑中并；H5 起步任务在仓根创建实体文件
- HD-002 ~ HD-008 + providers/* HD：起草时**不重复**本节内容，仅在§2 文件结构加 `GlobalUsings.cs` 行 + 加一行“命名空间 / 编码规约见 HD-001 §14”引用
- 下次会话补 ADR-024 作为跨 csproj 根决策证据（覆盖命名空间 + file-scoped + GlobalUsings + UTF-8/LF 四项横切规约）

### 14.5 picker 拍板记录

- 2026-05-11 Owner 主动指令（非 picker）：“`Inkwell.Abstractions` 和 `Inkwell.Core` 都用 `Inkwell` / 其他类库默认用类库名、子目录不加入命名空间 / file-scoped namespace / GlobalUsings”——无歧义语义，遵“不能选就别让填”反向原则跳过 picker、直接锁为本节。
- 2026-05-11 Owner 主动指令（非 picker）：“所有代码使用 utf-8 编码 / LF 换行符 / 配到 .editorconfig 和 git 提交过滤”——同样是闭合枚举，跳过 picker直接锁为 §14.6。

### 14.6 文件编码与换行符

> **2026-05-11 新增**（Owner 拍板）：仓库所有文本型文件统一 UTF-8 编码 + LF 换行符。本小节是跨 csproj 横切规约，由 HD-001 锁定；仓库根级 `.editorconfig` + `.gitattributes` 是唯一实体载体，**不**叠加 csproj 级配置。

#### 14.6.1 事实源

- 编码：**UTF-8 不带 BOM**。[Roslyn / MSBuild 默认](https://learn.microsoft.com/dotnet/core/tools/csproj) 读不带 BOM 的 UTF-8 源代码；带 BOM 的文件会造成 git diff / 工具链（`rg` / `sed` / shell here-doc）出现不可见字符。与 [Microsoft Agent Framework dotnet 仓库惯例](../../../../microsoft/agent-framework/dotnet/) 一致
- 换行符：**LF**（`\n`）。包括 `.cs` / `.csproj` / `.props` / `.targets` / `.json` / `.md` / `.yml` / `.yaml` / `.sh` / `.ps1` / `.editorconfig` / `.gitattributes` / `Dockerfile` / `Directory.Build.props` 等所有文本型文件。Windows 开发机以 LF 存 / 以 LF 提，依靠 Git 全局 `core.autocrlf=input` 或 仓库级 `.gitattributes` 强制
- 香贴习惯：所有文本型文件末尾留一个空行（[POSIX 文本文件定义](https://pubs.opengroup.org/onlinepubs/9699919799/basedefs/V1_chap03.html#tag_03_403)）、不留尾部空格

#### 14.6.2 `.editorconfig`（仓库根 / H5 起步任务创建）

必包含最小片段（H5 起步任务创建实体文件时拷贝）：

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
indent_style = space
indent_size = 4
trim_trailing_whitespace = true
insert_final_newline = true

[*.{json,yml,yaml,md}]
indent_size = 2

[*.{ps1,psm1,psd1}]
charset = utf-8-bom
```

**说明**：

- `[*]` 顶格锁 utf-8 不带 BOM + LF + 末行空行 + 尾部空格修剪，[VS Code](https://code.visualstudio.com/docs/getstarted/settings) / [Visual Studio](https://learn.microsoft.com/visualstudio/ide/create-portable-custom-editor-options) / [Rider](https://www.jetbrains.com/help/rider/Using_EditorConfig.html) / `dotnet format` 均会读取。全仓库 100% 文本型文件都应命中
- `*.{ps1,psm1,psd1}` 例外：PowerShell 脸本被 Windows PowerShell 5.1 读取时**必须**带 BOM，否则中文脸本中字符乱码（[PowerShell encoding](https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_character_encoding)）。[install.ps1](../../../install.ps1) / [uninstall.ps1](../../../uninstall.ps1) 同样遵。Inkwell 项本身不写 PowerShell，但保留例外项防未来加脚本踩坑
- 不起业务意义的详细设置（如 `csharp_*` 风格项）留到 [.he docs/instructions-layout.md](../../../.he/docs/instructions-layout.md) 推荐的 `.github/instructions/coding-discipline.instructions.md` 另行锁，本 HD 不探

#### 14.6.3 `.gitattributes`（仓库根 / H5 起步任务创建）

必包含最小片段：

```gitattributes
# auto detect text files; force LF on checkin & checkout
* text=auto eol=lf

# explicit text
*.cs       text eol=lf
*.csproj   text eol=lf
*.props    text eol=lf
*.targets  text eol=lf
*.json     text eol=lf
*.md       text eol=lf
*.yml      text eol=lf
*.yaml     text eol=lf
*.sh       text eol=lf
*.editorconfig  text eol=lf
*.gitattributes text eol=lf
Dockerfile text eol=lf
Directory.Build.props text eol=lf
Directory.Packages.props text eol=lf

# PowerShell stays CRLF (BOM 嵌入后其实两者都可，但 CRLF 免 Windows PS 5.1 虚假同名问题）
*.ps1   text eol=crlf
*.psm1  text eol=crlf
*.psd1  text eol=crlf

# binary, do not normalize
*.png   binary
*.jpg   binary
*.jpeg  binary
*.gif   binary
*.ico   binary
*.webp  binary
*.pdf   binary
*.zip   binary
*.7z    binary
*.tar.gz binary
*.dll   binary
*.exe   binary
*.nupkg binary
*.snk   binary
```

**说明**：

- `* text=auto eol=lf` 是默认兑底；Git 以为是文本的任何未明示映射的扩展名均被强制 LF，避免 Windows 上提交 CRLF 肦胀 diff
- 明示列举所有仓库出现过的文本扩展名是决胜手——`text=auto` 只看内容探测，对 ambiguous 文件（如混包含二进制串的 `.json`）什么都不做；明列后不依赖探测
- `*.ps1` / `*.psm1` / `*.psd1` 锁 CRLF 与 §14.6.2 PowerShell 例外保持一致；Windows PS 5.1 读 LF + BOM 的 `.ps1` 能跳坑但仓库练习不努力去验证多版本 PS 的联动，这是 Microsoft Agent Framework dotnet 仓库同样选择
- 二进制扮明 `binary`，不走 `text=auto`——`prototypes/` 下 H1 截屏累计 [9 张 PNG](../../02-prototype/) + 依赖 `.nupkg` 都走本段

#### 14.6.4 CI 自检

| 检项                             | 命令                                                                                                                                       | 期望                                                              |
| -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------- |
| 仓库根存在 `.editorconfig`       | `test -f .editorconfig`                                                                                                                    | exit 0                                                            |
| 仓库根存在 `.gitattributes`      | `test -f .gitattributes`                                                                                                                   | exit 0                                                            |
| `.editorconfig` 锁 utf-8 + LF    | `rg -n 'charset = utf-8' .editorconfig` 与 `rg -n 'end_of_line = lf' .editorconfig`                                                        | 各 ≥ 1 行                                                         |
| `.gitattributes` 锁全局 LF       | `rg -n '\* text=auto eol=lf' .gitattributes`                                                                                               | ≥ 1 行                                                            |
| 全仓库无 BOM                     | `rg -n -l '^\xef\xbb\xbf' -g '!*.ps1' -g '!*.psm1' -g '!*.psd1' -g '!*.{png,jpg,jpeg,gif,ico,webp,pdf,zip,7z,tar.gz,dll,exe,nupkg,snk}' .` | 0 行（PowerShell 例外 + 二进制排除）                              |
| 全仓库无 CRLF（PowerShell 除外） | `git ls-files -z \| xargs -0 grep -l $'\r$' \| grep -Ev '\.(ps1\|psm1\|psd1)$'`                                                            | 0 行                                                              |
| `dotnet format` 免漂检           | `dotnet format --verify-no-changes`                                                                                                        | exit 0（[`AGENTS.md` §1](../../../AGENTS.md) 已锁定为提交前门禁） |

#### 14.6.5 责任划分

- **HD-001 锁**：编码 + 换行符 + `.editorconfig` / `.gitattributes` 最小片段示例（本节）
- **H5 起步任务锁**：在仓库根**创建**实体 `.editorconfig` + `.gitattributes` 文件（`docs/` 层仅锁内容，不创建实体文件）；同时创建 `Directory.Build.props` / `Directory.Packages.props` 与本 HD §2 文件结构中所锁的 csproj 骨架
- **CI 锁**：§14.6.4 七条检项进 [GitHub Actions](https://docs.github.com/actions) PR 门禁流水线（另 H5 起步任务锁定）
- **`docs/` 豁免**：本会话**不**动 `.editorconfig` / `.gitattributes` 实体文件（[Agent §6.2 禁区](../../../.github/agents/h3-detailed-design-author/AGENT.md)）
