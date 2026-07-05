---
id: ADR-023-port-signature-bare-task-with-exceptions
stage: H2
status: accepted
authors:
  - name: H3-DetailedDesignAuthor
    role: agent
reviewers: [ Inkwell ]
created: 2026-05-11
updated: 2026-05-11
upstream:
  - REQ-014
  - NFR-006
  - ADR-002
  - ADR-013
  - ADR-017
  - HD-001
  - HD-002
  - HD-003
downstream: []
---

# ADR-023 端口层签名规约：裸 `Task<T>` + 单一 `InkwellException(code, message)`

## 上下文

H3 Inkwell.Abstractions 起草初期（[HD-001](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) picker 2026-05-10 Q1 / [HD-003](../../04-detailed-design/Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) picker 2026-05-11 Q1=B + Q2=A）锁定端口层方法返回 `Task<Result<TResponse>>`、自研 `Result<T>` / `Error` 工具、业务失败走 `Result.Failure(error)` 与程序错误抛 `InkwellException` 的二分制。设计意图是"业务失败在签名中暴露 / 编译期强制处理 / 业务失败热路径零异常开销"。

2026-05-11 [design-review-report §7](../../04-detailed-design/design-review-report.md#7-hd-003-filestorage-port-增量评审2026-05-11) 评审收尾后，Owner 提出端口层切回与 [.NET BCL](https://learn.microsoft.com/dotnet/standard/runtime-libraries-overview) / [ASP.NET Core](https://learn.microsoft.com/aspnet/core/) / [EF Core](https://learn.microsoft.com/ef/core/) / [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/) / [Azure SDK](https://learn.microsoft.com/dotnet/azure/sdk/azure-sdk-for-dotnet) 一致的"裸 `Task<T>` + 异常"风格——降低端口实现与调用方的认知负担、消除每个调用点 `if (result.IsFailure)` 样板代码、与生态主流模式对齐。

2026-05-11 picker 拍板：

- **Q-scope = A**：端口层（`IXxxProvider` / `IXxxService`）全裸；业务命名空间（`Inkwell.Core.Agents` / `.Models` / `.Tools` / ...）保留 `Result<T>` / `Error` 工具作可选模式
- **Q-errorcode = A**：错误码字符串表（`ErrorCodes.<Module>` 静态类、`INK-<MODULE>-<NNN>` 格式）保留；端口层统一抛 `InkwellException(code, message, inner?)`，不为每错误码起异常子类

驱动因素：

- **与主流 SDK 一致**：[`Microsoft.Agents.AI`](https://github.com/microsoft/agent-framework) / `HttpClient` / `DbContext.SaveChangesAsync` / `IConnectionMultiplexer.GetDatabase` 全裸 `Task<T>` + 抛异常——Inkwell 端口层若引入 `Task<Result<T>>` 反而是新人需要先学的"私有方言"
- **OTel `exception.*` 字段已成标准**：[ADR-013](./ADR-013-observability-otel-self-hosted-grafana.md) 锁定 `exception.type` / `exception.code` / `exception.message` / `exception.stacktrace` 五字段；裸抛与 OTel 模型天然契合，无需在 Result 序列化层做一次桥接
- **错误码表 vs 异常类层级**：6 大端口 × 平均 5 ~ 9 错误码 = 30 ~ 50 个异常类 .cs 文件；保留错误码字符串表 + 单一 `InkwellException` Code 字段，维护成本显著低于"每错误码一异常类"

## 决策

**端口层（`IXxxProvider` / `IXxxService`）方法签名采用裸 `Task<T>` / `Task` / `Task<bool>` / `IAsyncEnumerable<T>` / 同步原生类型；业务失败与程序错误统一通过 `throw new InkwellException(ErrorCodes.<Module>.<Name>, message, inner?)` 抛出；错误码 `INK-<MODULE>-<NNN>` 字符串表保留为静态类（如 [`ErrorCodes.FileStore`](../../04-detailed-design/Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md)），纳入 OTel `exception.code` 字段。**

### 核心边界（5 条）

1. **端口层禁 `Task<Result<T>>` / `Result<T>` 签名**——CI grep `rg -n 'Task<Result<' src/core/Inkwell.Abstractions/` 期望 0 行
2. **错误码字符串表保留**——`ErrorCodes.<Module>` 静态类按 `INK-<MODULE>-<NNN>` 格式定义；`InkwellException.Code` 字段承载
3. **单一 `InkwellException` 统一抛出点**——`new InkwellException(code, message, inner?)`，不为每错误码建异常子类（避免 30+ 异常类碎片化）
4. **`InkwellConfigurationException` / `InkwellBuilderException` 子类保留**——它们是"程序错误"的语义分组（DI 装配错误 / Builder 链错误），与"业务错误"区分清晰；不在本 ADR 范围内变更
5. **`Result<T>` / `Error` 工具保留**——[HD-001 §3.1 / §3.2](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) `Common/Result.cs` / `Common/Error.cs` 不删除；端口层不再强制使用，**业务命名空间**（`Inkwell.Core.*`）按需自由使用（如领域服务内部组合多个端口调用、需要纯函数式聚合结果时）

### 调用方语义约定

| 命名模式 | 返回 | 失败语义 |
| --- | --- | --- |
| `Find*Async(...)` | `Task<T?>` | 实体不存在 → 返回 `null`，不抛 |
| `Get*Async(...)` | `Task<T>` | 实体不存在 → 抛 `InkwellException(EntityNotFound)` |
| `Exists*Async(...)` | `Task<bool>` | 仅查询，网络故障抛 `InkwellException(ConnectionFailed)` |
| `Delete*Async(...)` | `Task<bool>` | 幂等：`true` = 实际删除 / `false` = 本不存在；网络故障抛异常 |
| `List*Async(...)` | `IAsyncEnumerable<T>` 或 `Task<PagedResult<T>>` | 流式或一次性；远端故障在 `MoveNextAsync` 或 await 处抛异常 |
| `Create*Async / Update*Async / Upload*Async / ...` | `Task<T>`（成功返回 DTO） | 业务失败抛 `InkwellException(<具体错误码>)`；网络故障抛 `InkwellException(ConnectionFailed)` |
| 任意方法 | — | 取消 → `OperationCanceledException`（不包装为 `InkwellException`，遵循 [BCL 惯例](https://learn.microsoft.com/dotnet/standard/threading/cancellation-in-managed-threads)） |

### CI 自检

| 编号 | 检查项 | 命令 |
| --- | --- | --- |
| A1 | 端口层无 `Task<Result<` 残留 | `rg -n 'Task<Result<' src/core/Inkwell.Abstractions/` 期望 0 行 |
| A2 | 端口层无 `Result<` 返回类型 | `rg -n -e 'public Result<' -e ': Result<' src/core/Inkwell.Abstractions/ -g '!Common/Result.cs' -g '!Common/Error.cs'` 期望 0 行（`Result.cs` / `Error.cs` 自身定义除外） |
| A3 | 业务命名空间 `Result<T>` 使用允许但不强制 | 无 grep 规则——审计性允许 |
| A4 | `InkwellException` 抛出携带有效 code | `rg -n 'throw new InkwellException\(' src/core/ providers/` 全部命中应使用 `ErrorCodes.<Module>.<Name>` 常量，不允许字面量 |
| A5 | 错误码表存在 | `rg -n 'public static class ErrorCodes' src/core/Inkwell.Abstractions/` 期望 ≥ 6（6 大端口各一个 partial class 段） |

## 备选项

### 备选 A（被选用）：裸 `Task<T>` + 单一 `InkwellException(code)`

- **被选用**：
  1. **与主流 .NET SDK 一致**——降低新人 / 跨项目人员的学习曲线（[ASP.NET Core](https://learn.microsoft.com/aspnet/core/) / [EF Core](https://learn.microsoft.com/ef/core/) / [Azure SDK](https://learn.microsoft.com/dotnet/azure/sdk/) / [`Microsoft.Agents.AI`](../../../../microsoft/agent-framework/dotnet/) 全用这套模式）
  2. **零样板代码**——业务调用点 `var result = await provider.UploadAsync(...)` 直接拿值；无需 `if (r.IsFailure) return Result<...>.Failure(r.Error)` 样板传播
  3. **跨进程 trace 天然**——[ADR-019](./ADR-019-process-topology-webapi-worker-split.md) WebApi / Worker 双进程下，异常通过 OTel `exception.*` 跨进程 trace（[ADR-013](./ADR-013-observability-otel-self-hosted-grafana.md) 已支持）；不需要把 `Result<T>` 序列化进 [`MessageEnvelope`](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)
  4. **错误码 / OTel 字段零变化**——`exception.code` 仍是 `INK-FILESTORE-002` 这种字符串；Loki / Grafana 查询条件、SLA 仪表盘、告警规则全部沿用
  5. **`InkwellException` 已存在**——[HD-001 §3.3](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 起草的 `InkwellException(code, message, inner)` 直接复用，仅在 §5.3 规约语义层把"业务错误"也纳入抛出范围

- **放弃理由（即取舍）**：
  - **业务热路径成本**：抛异常含 stacktrace 收集，比 `Result<T>` 返回慢 100 ~ 1000×。但端口层"业务失败"频率远低于"成功"（典型 < 1%），热路径几乎都走 happy path 零开销；冷路径慢 100× 可接受（ASP.NET Core 路由 / EF Core `SaveChanges` / `HttpClient` 4xx / 5xx 均同样接受）
  - **编译期强制丢失**：`Task<Result<T>>` 编译期逼调用方处理 `IsFailure` 分支；裸 `throw` 不逼。补偿手段：Code Review + [Roslyn analyzer](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/) 兜底（如 `CA2007` / `CA1031` / 自定义 `Inkwell.Analyzers` 规则——已纳入 [HD-001 §11 待补](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)）

### 备选 B：保留 `Task<Result<T>>`（原决策）

- **被否决**：上下文 §驱动因素三条反向论据
  1. 与主流 SDK 不一致——内部"私有方言"
  2. 调用方样板代码（`if (r.IsFailure) {...}` 或 `result.Value` 解包）成本累计高
  3. 跨进程 trace 需要把 `Result<T>` 序列化进 envelope，多一层桥接

### 备选 C：异常类层级（每错误码一异常子类）

- **被否决**：
  1. **维护成本高**：6 大端口 × 5 ~ 9 错误码 = 30 ~ 50 个 .cs 文件
  2. **扩展磨损**：每加一个错误码就要建 .cs + 注册 OTel + 写测试
  3. **C# 多态 catch 优势弱**：多态 catch 在"少数粗分类"场景有效，对 30 ~ 50 个层级反而过度
  4. **OTel 查询不需要类型分流**：Loki / Grafana 按 `exception.code` 字段过滤即可

### 备选 D：两者兼有（异常类层级 + Code 字符串）

- **被否决**：维护成本翻倍；调用方既可按类型 catch 也可按 code 字段 catch，分支多、风格不一致；无明显增益

## 维护影响

- **HD-001 §3.1 / §3.2 / §3.3 callout 更新**：`Result<T>` / `Error` 标"业务层可选"；`InkwellException` 标"端口层统一抛出点"
- **HD-001 §5.2 / §5.3 重写**：规约文本翻为裸 `Task<T>` + 异常模式
- **HD-002 / HD-003 第二轮 errata**（独立批次）：所有 `IXxxProvider` 方法签名翻新；§ErrorCodes 表保留；§Result-related DTO（如 [`FileUploadResult`](../../04-detailed-design/Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) / [`FileDownloadResponse`](../../04-detailed-design/Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md)）保留作"成功返回结构"
- **ADR-015 / ADR-016 / ADR-018 errata 块二次追加**：声明 H3 第二轮规约翻转
- **未起草的 HD-004 ~ HD-008**：直接用新规约
- **测试代码**：`result.IsSuccess.Should().BeTrue()` 翻为 `await act.Should().NotThrowAsync()`；`result.Error.Code.Should().Be(...)` 翻为 `(await act.Should().ThrowAsync<InkwellException>()).Which.Code.Should().Be(...)`
- **design-review-report §8 修订纪要**：声明本 ADR + HD-001 翻新已落地 / HD-002 / HD-003 待第二轮 errata

## 成本 / 性能 / 安全 / 交付影响

- **成本**：开发体验提升——调用方代码少 ~30% 样板；新人不需先学 Result / Match 模式即可上手
- **性能**：业务失败热路径变慢 100 ~ 1000×；但成功路径零开销（vs `Result<T>` 仍有 struct 创建 + boxing 风险）；端口层业务失败频率 < 1%，总体净收益正
- **安全**：错误码字段与 OTel `exception.*` 五字段全部不变；审计日志 [HD-001 §4.2](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) `exception.code` 字段保留；攻击面零增加
- **交付**：HD-002 / HD-003 翻签名 = 第二批 1 个工作单元；HD-004 ~ HD-008 用新规约直接起草 = 加速；总体不延期 H3 完成节点

## 迁移路径

1. **第一批（本 ADR 同会话）**：
   - 新建本 ADR（status: accepted by Inkwell）
   - 翻新 [HD-001 §3.1 / §3.2 / §3.3 / §5.2 / §5.3](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)（callout + 规约文本）
   - [design-review-report §8](../../04-detailed-design/design-review-report.md) 加修订纪要
2. **第二批（下次会话）**：
   - [HD-002](../../04-detailed-design/Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 翻 `IPersistenceProvider` 全方法签名
   - [HD-003](../../04-detailed-design/Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) 翻 §3.1 七方法签名 + §1.3 picker Q1 / Q2 标 superseded + §1.4 偏离表大幅缩减 + frontmatter 加第二轮 errata
   - [ADR-015](./ADR-015-object-storage-provider-switchable.md) 二次 errata 块追加（声明 H3 第二轮翻转）
3. **第三批（后续）**：[HD-004 ~ HD-008](../../04-detailed-design/Inkwell.Abstractions/) 直接用新规约起草
4. **CI 落地**：上方 §决策·CI 自检 5 条命令进 [GitHub Actions](https://docs.github.com/actions) PR 流水线

## 联动提示

- [HD-001 §3.1 / §3.2 / §3.3 / §5.2 / §5.3](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)：本 ADR 同批次翻新
- [HD-002 IPersistenceProvider](../../04-detailed-design/Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)：第二批待 errata
- [HD-003 IFileStorageProvider](../../04-detailed-design/Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md)：第二批待 errata；本轮 5/11 第一轮 errata 块由第二轮 errata 块叠加
- [ADR-015 FileStorage Provider](./ADR-015-object-storage-provider-switchable.md) / [ADR-016 Cache Provider](./ADR-016-cache-provider-redis.md) / [ADR-018 Queue Provider](./ADR-018-queue-abstraction-channels-default.md)：二次 errata 块待落地
- [design-review-report §8](../../04-detailed-design/design-review-report.md)：本 ADR 同批次加修订纪要
- [Microsoft.Extensions.VectorData 抽象](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-vector-data)（[ADR-020](./ADR-020-vector-store-microsoft-extensions-vectordata.md)）：本就裸 `Task<T>` + 异常，规约一致，无需 errata
- [HD-001 picker 2026-05-10 Q1](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)（Result 自研轻量 readonly struct）+ [HD-003 picker 2026-05-11 Q1=B / Q2=A](../../04-detailed-design/Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md)：本 ADR 翻转端口层使用范围，picker 决策本身不撤回（`Result<T>` 工具仍按 Q1 的"自研轻量 readonly struct + 零三方包"形态实现，仅"端口层强制"语义被本 ADR 翻为"业务层可选"）

## errata

### 2026-05-11 errata·01：废错误码机制，改走 .NET BCL 异常类型分流

**触发**：本 ADR `status: accepted` 后 1 小时内，Owner 在同会话 提出"不需要错误码机制、不要太复杂"。[Harness Engineering 反模式"反复纠错"](../../.github/copilot-instructions.md) 在本轮状态主动警示什么都不做、让 Owner 冷静是另一个选项；Owner picker Q-existing=A 拍板继续推进但走 errata 路径保决策证据链。

**picker 拍板（2026-05-11）**：

- **Q-strategy = A**：全 .NET BCL 异常，零自建类型。业务失败 / 程序错误都走 [BCL 异常类型](https://learn.microsoft.com/dotnet/standard/exceptions/)：[`FileNotFoundException`](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception) / [`IOException`](https://learn.microsoft.com/dotnet/api/system.io.ioexception) / [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception) / [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception) / [`UnauthorizedAccessException`](https://learn.microsoft.com/dotnet/api/system.unauthorizedaccessexception) / [`NotSupportedException`](https://learn.microsoft.com/dotnet/api/system.notsupportedexception) / [`TimeoutException`](https://learn.microsoft.com/dotnet/api/system.timeoutexception) 等。同类内不同业务语义靠 `Message` 区分 + OTel [`exception.type`](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/) 字段分流
- **Q-existing = A**：本 ADR `status: accepted` 不变，仅增 errata 节（同 [ADR-022 先例](./ADR-022-entity-domain-mapper-selection.md)）
- **Q-scope = A**：本会话只动根决策层（ADR-023 errata + HD-001 + design-review-report），HD-002 / HD-003 §3.8 ErrorCodes.FileStore.cs 与 §3.1 错误处理字段进第二批 errata

**本次翻转覆盖内容**：

| 原内容 | errata 后 | 定位 |
| --- | --- | --- |
| `InkwellException(string code, string message, Exception? inner)` 作为业务 + 程序错误统一抛出基类 | 删除基类。仅保留 `InkwellConfigurationException` / `InkwellBuilderException` 两个子类，直接继承 [`System.Exception`](https://learn.microsoft.com/dotnet/api/system.exception)；DI / Builder 专用 | §决策·核心边界 · 第 2 / 3 / 5 条 |
| 错误码表 `ErrorCodes.<Module>` + `INK-<MODULE>-<NNN>` 格式 | 废除。不再要求任何 `ErrorCodes` 静态类 | §决策·核心边界 · 第 2 条 |
| OTel `exception.code` 字段 | 不再要求。改为靠 OTel [标准五字段](https://opentelemetry.io/docs/specs/semconv/attributes-registry/exception/)：`exception.type` / `exception.message` / `exception.stacktrace` / `exception.escaped` / `exception.id`；`exception.type` = 全限定名（如 `System.IO.FileNotFoundException`） | §决策·核心边界 · 第 2 条 |
| CI A4 = 验证 `ErrorCodes.<Module>.<Name>` 常量引用 | 废除。改为 CI A4' ：验证端口实现不抛自定义异常类（除 Configuration/Builder）：`rg -n 'throw new \w*Exception' src/core/Inkwell.Abstractions/` + `rg -n 'class \w+Exception' src/core/Inkwell.Abstractions/` 期望仅命中 `InkwellConfigurationException` / `InkwellBuilderException` | §决策·CI 自检 · A4 |
| CI A5 = 验证 `ErrorCodes` 静态类存在 ≥ 6 个 | 废除。A5 整条删 | §决策·CI 自检 · A5 |
| §调用方语义约定表中 `InkwellException(EntityNotFound)` / `InkwellException(ConnectionFailed)` / `InkwellException(<具体错误码>)` | 改为 BCL 对照表：实体不存在 → [`KeyNotFoundException`](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception) / 网络故障 → [`IOException`](https://learn.microsoft.com/dotnet/api/system.io.ioexception) 或 SDK 表达性子类（如 [`Azure.RequestFailedException`](https://learn.microsoft.com/dotnet/api/azure.requestfailedexception)） / 超时 → [`TimeoutException`](https://learn.microsoft.com/dotnet/api/system.timeoutexception) / 参数违反 → [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception) / 状态错误 → [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception) / 未授权 → [`UnauthorizedAccessException`](https://learn.microsoft.com/dotnet/api/system.unauthorizedaccessexception) | §决策·调用方语义约定 |

**本次翻转保留内容**：

- 端口层裸 `Task<T>` / `Task<bool>` / `IAsyncEnumerable<T>` 签名约定（原 Q-scope=A）保持不变
- 端口层禁 `Task<Result<T>>` / `Result<T>` 返回类型约定（原 §核心边界 · 第 1 条）保持不变
- `Result<T>` / `Error` 工具作业务命名空间可选语义（原 §核心边界 · 第 5 条）保持不变（但 `Error.Code` 不再需要正则校验 `^INK-[A-Z]+-\d{3}$`——业务层自由使用字符串）
- 起草 [HD-002](../../04-detailed-design/Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) / [HD-003](../../04-detailed-design/Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) 二次 errata（第二批）均需同步删 ErrorCodes 子表 + 按 BCL 对照表重写错误处理字段

**下一步（本会话同批落地）**：

- [HD-001 §3.3 InkwellException](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 重写——仅保留 Configuration / Builder 子类
- [HD-001 §4](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 重写——错误码命名段删、日志字段表改 OTel `exception.*` 五字段
- [design-review-report §8](../../04-detailed-design/design-review-report.md) 补一个 §8.8 第三轮规约翻转记录

### 2026-05-11 errata·02：删 `Common/Result.cs` + `Common/Error.cs` 抽象，业务命名空间错误处理一律 BCL 异常

**触发**：errata·01 落地后约 30 分钟，Owner 在同会话主动指令“`Result` 和 `Error` 这两个类都不需要了，有点增加了系统的复杂度”。本次质疑的对象是 errata·01 保留下来的「业务命名空间可选 `Result<T>` / `Error` 工具」——errata·01 仅把端口层强制翻成业务层可选，但 Inkwell 仍持有 `Common/Result.cs` + `Common/Error.cs` 两个抽象文件作为业务层可选模式入口。Owner 判断：保留可选会形成两套并存范式，长期沉淀为 Inkwell-private 方言，与本 ADR 主决策“与 .NET 主流 SDK 一致”的初衷相悖。

AI 在收到指令时按 [Harness Engineering 反模式 “反复纠错”](../../../.github/copilot-instructions.md) 主动警示：本会话已是连续第 3 轮规约级翻新（端口层签名 + 错误码废止 + 抽象删除），需评估是否进入“反复纠错”。Owner picker `session-pause = A`（继续推进；理由：本会话三轮均为“单向收紧不来回”，与典型反复纠错的“A 改 B、B 改 A” 不同）。

**picker 拍板（2026-05-11）**：

- **Q-scope = A**：完全删除 `Common/Result.cs` + `Common/Error.cs` 两个文件及全部引用。业务命名空间（`Inkwell.Core.Agents` / `.Models` / `.Tools` / ...）错误处理一律走 [BCL 异常类型](https://learn.microsoft.com/dotnet/standard/exceptions/) + 现成抽象（[`ValidationResult`](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.validationresult) / `IEnumerable<string>` / `record` 业务返回类型）
- **Q-existing = A**：本 ADR `status: accepted` 不变，仅增 errata·02 节叠加在 errata·01 之上（同 [ADR-022 多轮 errata 先例](./ADR-022-entity-domain-mapper-selection.md)）
- **Q-session-pause = A**：本会话三轮规约翻新均为单向收紧（裸 `Task<T>` + 异常 → 废错误码 + BCL 分流 → 删抽象），非典型反复纠错；继续推进至本轮落地后再沉淀

**备选项（picker 选项摘要）**：

- **A（被选用）完全删除**：`Common/Result.cs` + `Common/Error.cs` 两文件不起草、不入 `Inkwell.Abstractions`；业务命名空间一律 BCL；批量校验等多项错误场景走 [`ValidationResult`](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.validationresult) / `IEnumerable<string>`
- **B 保留作业务可选**（errata·01 现状）：被否决——与本 ADR 主决策“与 .NET 主流一致”的初衷相悖，形成两套并存范式的长期方言之争；新人需先学 Inkwell 私有 `Result<T>` 用法
- **C 仅删 `Error`、保 `Result<T>`**：被否决——半完成态；`Result<T>` 失去 `Error` 后退化为 [`Nullable<T>`](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/nullable-value-types) 的高级形式，语义与 BCL 工具重叠且更弱
- **D 推迟到 v2**：被否决——决策证据已成熟（errata·01 + 三轮单向收紧 + Owner 主动指令），推迟无新信息收益，反而让 HD-002 ~ HD-008 起草期间继续累积“业务可选 Result”调用点债务

**本次翻转覆盖内容**：

| 原内容 | errata·02 后 | 定位 |
| --- | --- | --- |
| §决策·核心边界 · 第 5 条「`Result<T>` / `Error` 工具保留」 | 删除整条。`Common/Result.cs` + `Common/Error.cs` 不再存在，无可保留对象 | §决策·核心边界 · 第 5 条 |
| §备选项 A · 放弃理由“`Task<Result<T>>` 编译期逼调用方处理 `IsFailure` 分支……”补偿 | errata·01 已通过 BCL 异常 + [Roslyn analyzer](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/) 补偿；errata·02 进一步移除“业务层可选 `Result<T>`”退路 | §备选项 A · 放弃理由 |
| §维护影响“HD-001 §3.1 / §3.2 callout 更新：`Result<T>` / `Error` 标'业务层可选'” | 改为“HD-001 §3.1 / §3.2 整段删除，`Common/Result.cs` + `Common/Error.cs` 不起草” | §维护影响 |
| §联动提示中“[HD-001 picker 2026-05-10 Q1]……`Result<T>` 工具仍按 Q1 的 '自研轻量 readonly struct + 零三方包' 形态实现” | 改为“HD-001 picker 2026-05-10 Q1 标 superseded by errata·02，`Result<T>` 不再起草任何形态” | §联动提示 |

**本次翻转保留内容**：

- 端口层裸 `Task<T>` / `Task<bool>` / `IAsyncEnumerable<T>` 签名约定（原 Q-scope=A + errata·01）保持不变
- 端口层禁 `Task<Result<T>>` / `Result<T>` 返回类型约定（原 §核心边界 · 第 1 条 + errata·01）保持不变；errata·02 在此基础上**进一步**禁业务命名空间 `Result<T>` / `Error` 使用——CI grep `rg -n -e 'Result<' -e ': Error\b' src/core/ providers/` 期望 0 行（`Microsoft.Extensions.Logging` 等 BCL `Result` 类型靠 import 区分，不在 grep 击中范围内）
- errata·01 BCL 异常分流模式（业务失败抛 BCL 异常，OTel `exception.type` / `.message` / `.stacktrace` / `.escaped` / `.id` 五字段）保持不变
- `InkwellConfigurationException` / `InkwellBuilderException` 两子类保留——DI / Builder 装配期错误专用，与“业务错误走 BCL”边界清晰

**维护影响**：

- [HD-001 §3.1 / §3.2 整段删除](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)：`Common/Result.cs` / `Common/Error.cs` 不起草；§3 标题从“12 字段 × 12 文件”改为“10 字段 × 10 文件”；§3.3 ~ §3.12 编号保持不变（保追溯不断链）
- [HD-001 §1.3 Q1 / §10 Q1](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)：strikethrough + 标 “ADR-023 errata·02 后废止”
- [HD-001 §11 / §13](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)：加 2026-05-11 第四轮 errata 记录
- [HD-002 / HD-003](../../04-detailed-design/Inkwell.Abstractions/) 第二批 errata：业务层错误返回模式表删 `Result<T>` / `Error` 选项，仅保留“成功返回 `record` DTO + 失败抛 BCL 异常 + 多项校验返回 `ValidationResult`”
- [HD-004 ~ HD-008](../../04-detailed-design/Inkwell.Abstractions/) 起草：直接用 errata·01 + errata·02 后的新规约
- [design-review-report §8](../../04-detailed-design/design-review-report.md)：加 §8.9 第四轮纪要——声明本 errata·02 + HD-001 §3.1 / §3.2 已删除已落地
- 测试代码：`result.IsSuccess.Should().BeTrue()` 模式已在 errata·01 阶段大量翻为 `await act.Should().NotThrowAsync()`；errata·02 进一步保证业务命名空间测试一致——零 `Result.Success(...)` / `Result.Failure(...)` 残留

**成本 / 性能 / 安全 / 交付影响**：

- **成本**：净减 2 个 .cs 文件（`Result.cs` + `Error.cs`） + 对应 2 个 `*Tests.cs`；新人不再需要学 Inkwell 内部 `Result<T>` 用法——上手时间收敛到通用 .NET 异常处理；维护面减一
- **性能**：业务热路径 happy path 仍零开销（与 errata·01 一致）；批量校验场景走 [`ValidationResult`](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.validationresult) struct，与 [`record`](https://learn.microsoft.com/dotnet/csharp/language-reference/builtin-types/record) DTO 零分配开销持平；vs 备选 B“业务层可选 `Result<T>`” 无区别
- **安全**：与 errata·01 一致——OTel `exception.*` 五字段覆盖全部失败路径；攻击面零增加
- **交付**：HD-002 / HD-003 第二批 errata 一同删 `Result<T>` / `Error` 引用（同 errata·01 已规划批次），不额外增 PR；HD-004 ~ HD-008 起草加速（少一套抽象选项需 picker）；H3 完成节点不延期

**置信度**：high——三轮单向收紧（裸 `Task<T>` + 异常 → 废错误码 + BCL 分流 → 删抽象）证据链完整；与主流 .NET SDK（[ASP.NET Core](https://learn.microsoft.com/aspnet/core/) / [EF Core](https://learn.microsoft.com/ef/core/) / [Azure SDK](https://learn.microsoft.com/dotnet/azure/sdk/azure-sdk-for-dotnet) / [`Microsoft.Agents.AI`](../../../../microsoft/agent-framework/dotnet/)）模式一致；[HD-001 §13](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) 已记录 Owner 直接指令 + picker 拍板。

**下一步**：

- ✅ [HD-001 §3.1 / §3.2 整段删除](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)（H3 Agent 已先行落地 2026-05-11，本 ADR errata·02 补上游证据）
- ✅ [HD-001 §1.2 / §1.3 Q1 / §2.2 / §4.1 / §5.3 / §10 Q1 / §11 / §13 / §14.1 同步翻新](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)（H3 Agent 已先行落地）
- ⏳ [HD-001 §0 / §1.3 / §10 / §11 / §13 措辞同步](../../04-detailed-design/Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)：去“待 H2 起草” / “(待 H2 ArchitectAdvisor 起草 + accept)” 引号——本 errata·02 落地后由 H3 Agent 同步
- ⏳ [HD-002 / HD-003 第二批 errata](../../04-detailed-design/Inkwell.Abstractions/) 一并删 `Result<T>` / `Error` 引用 + frontmatter 加第四轮 errata callout
- ⏳ [HD-004 ~ HD-008 起草](../../04-detailed-design/Inkwell.Abstractions/) 用 errata·01 + errata·02 后的新规约直接落地
- ⏳ [design-review-report §8.9 第四轮纪要](../../04-detailed-design/design-review-report.md) 由 H3 Agent 在本 errata·02 落地后补
