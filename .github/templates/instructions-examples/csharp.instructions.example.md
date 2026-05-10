---
applyTo: 'src/**/*.cs'
---

<!--
这是一份 C# 代码规范的**参考样例**，不是 Harness Engineering 规范的一部分。

使用方式：
1. 把本文件复制到 .github/instructions/csharp.instructions.md（去掉 .example 后缀）
2. 按你项目的实际栈裁剪每一节，删除不适用的条目
3. 真正的硬约束应放进 .editorconfig + Roslyn analyzer + dotnet format，让 CI 强制
   instructions 文件只对 AI 生效，不对人/CI 生效——能让 Linter 干的事不要写在这里
4. 完成后由项目负责人评审，与 AGENTS.md 的项目身份签字位同源

Harness 不维护本样例的内容深度；详细风格请查 Microsoft .NET Coding Guidelines、
Roslyn analyzer 文档、xUnit / NUnit 官方推荐等权威来源。
-->

# C# 编码规范

> 与 [`coding-discipline.instructions.md`](./coding-discipline.instructions.md)（流程纪律）叠加生效。
> 风格层面的硬约束以 `.editorconfig` + Roslyn analyzer 为准，本文件只补充 Linter 不便强制的部分。

## 1. 命名

- 公共 API（type / member / namespace）：[ 待填，建议 PascalCase ]
- 私有字段：[ 待填，建议 `_camelCase` ]
- 局部变量 / 参数：[ 待填，建议 camelCase ]
- 常量：[ 待填，建议 PascalCase ]
- 接口：[ 待填，建议以 `I` 前缀，如 `IRepository` ]
- 异步方法：[ 待填，建议以 `Async` 后缀 ]
- 不在本文件重复 `.editorconfig` 已强制的命名规则

## 2. 错误处理

- 异常分层：[ 待填，建议在领域边界包装为业务异常，基础设施异常不上抛 UI 层 ]
- 不吞异常：`catch` 必须重新抛出或写结构化日志
- 不用异常做控制流：状态判定走 `Result<T>` / `bool TryXxx` 等显式返回
- 取消令牌：所有 IO / 长跑方法接 `CancellationToken cancellationToken = default`

## 3. 并发与异步

- IO 必须 `async` / `await`，不阻塞线程（不用 `.Result` / `.Wait()`）
- 异步方法签名以 `Async` 结尾、返回 `Task` / `Task<T>` / `ValueTask<T>`
- 库代码默认 `ConfigureAwait(false)`（应用代码通常不需要）
- 共享状态用 `Channel<T>` / `ConcurrentDictionary<,>` / `Interlocked.*`，避免裸 `lock`

## 4. 测试

- 测试框架：[ 待填，xUnit / NUnit / MSTest 选一并固定 ]
- 测试项目命名：`<TargetProject>.Tests`，与被测项目同结构
- 测试方法命名：[ 待填，建议 `Method_Scenario_ExpectedBehavior` 或 `Should_XXX_When_XXX` ]
- 三段式：Arrange / Act / Assert 注释或空行分隔
- Mock：[ 待填，Moq / NSubstitute 选一 ]，只 mock 你拥有的接口
- 覆盖率门槛：见 CI 配置（不在本文件重复）

## 5. 不在此文件强制

下列项交给自动化工具强制，不写在 instructions 里：

- 缩进 / 大括号 / 空行 / using 排序 → `.editorconfig` + `dotnet format`
- 命名约束的机械检查 → Roslyn analyzer（`StyleCop.Analyzers` / 自定义 analyzer）
- 静态分析告警 → `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` + `dotnet build`
- 包版本 / 目标框架 → `Directory.Build.props` + `global.json`
- 测试覆盖率 → CI 任务（如 `coverlet` + 阈值断言）
