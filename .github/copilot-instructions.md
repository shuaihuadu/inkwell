# GitHub Copilot Instructions

我们的沟通请使用简体中文进行。

## 项目简介

Inkwell 模拟了一个 AI 内容生产平台的完整业务场景，核心目标是系统性地覆盖 Microsoft Agent Framework（MAF）在真实业务中的主要使用模式。

- **后端**：.NET 10 + ASP.NET Core + Microsoft Agent Framework（MAF），C# 实现，代码位于 `src`
- **前端**：React + TypeScript + Ant Design + Ant Design X，代码位于 `src/app/webapp/src`
- **模型层**：通过 `Microsoft.Extensions.AI` 的 `IChatClient` 抽象，默认对接 Microsoft Foundry
- **持久化**：Entity Framework Core（InMemory / SQL Server 可切）
- **可观测**：OpenTelemetry + Aspire Dashboard
- **部署**：Docker Compose（`webapi` / `webapp` / `sqlserver` 三件套为默认起法；`A2AServer` / `DurableHost` 作为可选独立进程）

参考资料：

- Ant Design：<https://ant.design/docs/spec/introduce-cn/>
- Ant Design X（AI 对话组件）：<https://ant-design-x.antgroup.com/components/introduce-cn>
- Microsoft Agent Framework：<https://github.com/microsoft/agent-framework>

### 代码分层约定

- `src/core/Inkwell.Core`：领域模型、Agent 定义、Workflow Builder、Executor、中间件、Skills、记忆系统等核心能力
- `src/app/services/Inkwell.WebApi`：对外的 REST / SSE API 与控制器
- `src/app/services/Inkwell.A2AServer`、`Inkwell.DurableHost`：可选的独立进程（A2A 协议、DurableTask 托管）
- `src/app/webapp`：前端 SPA

模块边界要清晰：Controller 只负责协议转换和会话路由，业务能力一律落到 `Inkwell.Core`，不要把 MAF / EF 的调用散落到 Web 层。

在为本仓库贡献代码时，请遵循以下指南：

## Microsoft Agent Framework 使用约定

这一节沉淀项目里已经验证过的 MAF 使用方式和踩过的坑，新增 Executor / Workflow 时请按下面的规则来。

### Executor 是单例，跨次运行的状态必须交给 WorkflowContext

**这是最容易出错的一点。** MAF 的 Executor 在工作流中是单例持有的，同一个实例会被多次运行复用。把跨次运行需要保留或重置的状态放在 Executor 的实例字段上，会导致：

- 第一次运行正常
- 第二次运行时字段里还残留着上一轮的数据，出现空输出、重复输出或状态错乱

正确做法是把状态托管给工作流自己的状态机——使用 `IWorkflowContext.ReadStateAsync<T>(key)` / `QueueStateUpdateAsync(key, value)`。参考 `AnalysisAggregationExecutor` / `RankAggregatorExecutor` / `TranslationAggregationExecutor`：它们都用 `BufferKey` 在 Context 里累积分支结果，处理完一批后主动把 buffer 重置为空。

> 记忆点：**Workflow 本质上是一个状态图**。Executor 是图上的节点，节点本身应当是无状态的——一切跨调用的状态都属于这张图。

### 特性标注不能省

给 Executor 正确标注输入/输出的消息类型特性，MAF 的图构建和可视化都依赖它们：

- `[SendsMessage(typeof(T))]`：Executor 会向下游发送 `T` 类型消息
- `[YieldsOutput(typeof(T))]`：Executor 会产出工作流终态输出（`YieldOutputAsync`）

终态输出缺失时 `WorkflowChatClient` 会打出 `未产出任何终态输出（YieldOutputAsync）` 的提示——大多数情况是忘了加 `[YieldsOutput]` 或没有调用 `YieldOutputAsync`。

### 结构化输出优先用 JSON Schema

需要 LLM 返回结构化数据时，用 `ChatResponseFormat.ForJsonSchema<T>()` 强制 Schema，而不是写一堆"请你以 JSON 回复"的自然语言提示。解析失败的回退逻辑放在 Executor 里统一处理（见 `CriticExecutor` 的 `TryParseReviewDecision`）。

### HITL 走 RequestPort，而不是自定义 Hook

人工介入点一律用 `RequestPort.Create<TInput, TResponse>()` 暴露：

- 命中 RequestPort 时 Workflow 会真正挂起，等待外部 `SendResponseAsync`
- 服务端用 `HitlPendingRegistry` 登记挂起态，通过 SSE 把 `<<<HITL_REQUEST:{json}>>>` 标记发给前端
- 不要为了"跑得顺"把 HITL 写成自动批准的桩，那样会绕过 Workflow 的真实行为

### 分支 / 并行用 MAF 的图原语

- 条件分支 / 循环回退：`AddSwitch` + `AddCase<T>(predicate, target)` + `.WithDefault(...)`
- 扇出 / 扇入：`AddFanOutEdge` + `AddFanInBarrierEdge`

不要自己在 Executor 内部用 `if/while` 模拟跳转，交给 MAF 的图去做，可视化和状态管理都依赖这一点。

### 新增 Workflow 的落位

- Workflow Builder 放在 `src/core/Inkwell.Core/Workflows/*Builder.cs`
- 通用 Executor 放在 `src/core/Inkwell.Core/Workflows/Executors/`
- 仅供某个 Builder 使用的 Executor 可以作为同文件 `internal sealed` 类放在 Builder 文件末尾

## 文件编码要求

- 所有文本文件必须使用 UTF-8 编码保存，NoBOM
- 不要每次修改代码之后都要写总结文档
- 是否需要写总结文档取决于修改的复杂程度和影响范围，并获取用户的确认
- 注意模块边界、注意使用良好的设计、不要写碎片代码

## C# 代码规范

以下是适用于所有代码的一些通用规范：

- 所有 C# 文件使用 文件作用域命名空间（file-scoped namespaces）
- 将警告视为错误处理
- 遵循 .editorconfig 中的约定，创建文件的编码和行尾符都要按照 .editorconfig 中的设置
- 写代码之前务必要谨慎思考，保证逻辑正确，且没有语法错误
- 需要写出完整的解决方案并标明清晰的中文注释，注释需要符合C#注释风格的标准
- 系统中的错误消息和抛出的异常信息使用英文
- 所有公共方法和类应包含 XML 文档注释，类、接口、方法写出完整的 summary 注释，例如：
    ```
        /// <summary>
        /// 将项目加入队列
        /// </summary>
        /// <param name="item">要入队的项目</param>
        /// <param name="queueName">队列名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>表示异步操作的任务</returns>
        Task EnqueueAsync(T item, string? queueName = null, CancellationToken cancellationToken = default);
    ```
- 如果是属性的话根据属性的读写类型，注释中需要写出 `获取。。。。` `设置。。。。` 或者`获取或设置。。。。。`
- 访问类成员时请使用 this. 前缀
- 所有异步方法的名称应以 Async 结尾
- 使用显示的类型，而不要使用 var
- 保持代码风格一致，注意空行和缩进
- 对于实现了System.IDisposable，合理使用 using 关键字
- 合理使用 nullable 类型
- 合理使用 async 操作和 ConfigureAwait(false)
- 合理使用 `/// <inheritdoc />` 继承注释
- 避免使用 emoji 表情，无论是在日志还是注释中
- 在功能稳定的前提下，尽量保证较好的性能
- 在 C# 中始终使用主构造函数（Primary Constructor）
- 每次修改代码后都要运行 dotnet build
- 始终优先使用 System.Text.Json，而不是 Newtonsoft
- 新建的类和接口都应放在单独的文件中
- 如果成员可以声明为 static，则应始终声明为 static
- 始终检查自己的代码，确保其一致性、可维护性和可测试性
- 如果需求描述不明确或缺乏足够的上下文，务必主动询问以获得澄清
- 所有 JSON 操作仅使用 System.Text.Json
- 先确保代码质量和功能正确，最终的文档需要获取用户的确认
- 不要每次修改代码之后都要写总结文档

## 前端代码规范

- 使用kebab-case进行文件命名
- 代码和注释符合社区和统一的标准规范

## 单元测试规范

- 使用 MS Tests 写出完整的单元测试
- 为每个测试添加 Arrange、Act 和 Assert 注释
- 确保所有未被继承的私有类都声明为 sealed
- 验证每个测试确实测试了目标行为，例如：我们不应该有创建模拟、调用模拟然后验证模拟被调用的测试，而没有涉及目标代码。我们也不应该有测试语言特性的测试，例如编译器本身会捕获的问题。
- 避免在测试中添加过多注释，应优先使用清晰易懂的代码
- 遵循所在项目或类中的单元测试模式，新增测试时保持一致性
- 修复未通过的单元测试时，只能修改测试代码，如果必须修改业务逻辑代码，则需要获得用户确认

## 注意事项

如果需要你编写的提示词、Skills，请使用中文

### 跨平台开发

本项目在开发过程中有跨平台的开发需求，目标就是保证不同IDE和不同平台开发的一致性，有以下IDE和平台：

1. Windows + Visual Studio + Docker Desktop
2. VSCode SSH 到 Linux Ubuntu 24.04 + Docker
3. Mac OS + VSCode + Mac Docker Desktop
4. Windows + VSCode + Docker Desktop

在完成任务和修改代码的时候，需要保证在团队协作过程中各个平台和IDE均不受环境的影响，每个用户都不受影响，按照业界标准和最佳实践来修改和优化配置。
