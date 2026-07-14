## 重要提示：请务必使用中文与我沟通交流！！！

项目身份、模块边界、依赖规则见 [AGENTS.md](../AGENTS.md)。本文件只约定代码风格与协作细节，不重复架构内容。

## 协作规范

- 保持自己独立的技术判断，不要单纯顺着我的话说；如果我的方案有问题、有更优替代方案，或者你不认同，直接指出来并说明理由，跟我讨论清楚再动手
- 明确指令（比如"删掉这个文件"）照做即可；涉及方案选型、设计取舍类的讨论，该反对就反对，不要为了让我满意而附和有问题的决定
- 有分歧摆出来讨论，但最终拍板权在我；讨论过程不等于抗命，达成一致或我明确决定后要执行到位，不要反复纠缠已经拍板的结论

## 后端分层实现边界

以下规则是后端实现与代码评审的强制约束。新增或修改代码前必须先确定职责归属，并将实现放置在对应层。现有代码违反边界时，不得以保持旧有模式为由继续扩散，应在本次改动范围内将相关职责调整到正确层。确需变更分层边界时，必须先向 Owner 说明原因、影响与替代方案，获得明确确认后再实施。

### WebApi 层

- **职责**：`Inkwell.WebApi` 负责 HTTP 与传输协议适配、调用上下文提取和 Service 调用。HTTP 专属职责包括路由、请求绑定、请求 DTO 格式校验、`HttpContext`、Header、客户端 IP、Claims、Authentication / Authorization、Rate Limiting、文件上传、SSE / 流式响应以及请求取消传播。
- **上下文传递**：从 `HttpContext` 或 Claims 提取的 `UserId` 等调用者信息，必须转换为明确的业务参数传入 Service。Service 的执行结果或异常由 WebApi 映射为 HTTP 状态码、响应 DTO 或 Problem Details；重复映射应收敛到全局异常处理或统一的 WebApi 组件。
- **禁止事项**：WebApi 不得实现业务规则，不得直接调用 Repository，不得在 Controller、Endpoint、Middleware 或 Authentication Handler 中完成资源所有权判断、账号锁定规则、业务状态流转、跨资源操作编排或事务控制。

### Service 层

- **职责**：Service 负责业务规则与用例编排，包括业务授权、状态转换、跨资源操作顺序、事务边界和失败语义；输入与输出使用明确的业务 DTO、值对象和调用者标识。
- **依赖边界**：Service 按用例需要调用 Repository、`IPersistenceProvider`、`ICacheProvider`、`IQueueProvider`、`IFileStorageProvider`、`IAgentFactory`、`IModelRegistryService`、`TimeProvider` 等抽象端口，以及职责单一且依赖方向合规的其他业务 Service。具体依赖白名单以 [AGENTS.md §3.2](../AGENTS.md) 为准。
- **禁止事项**：Service 不得依赖 `HttpContext`、`IHttpContextAccessor`、Controller、`IActionResult`、HTTP Header、HTTP 状态码等传输层概念，也不得依赖 EF Core、Redis、Blob、Queue、MAF Hosting 等具体 Provider 或 SDK 实现。

### Repository 层

- **职责**：Repository 负责持久化数据的读取与写入，包括 CRUD、业务语义查询、筛选、排序、分页、投影、批量读写、Entity / Model 映射、乐观并发和数据库约束处理。Repository 方法可以使用 `Get*`、`Find*`、`List*` 等业务语义名称，但其实现必须保持为数据访问行为。
- **依赖边界**：Repository 依赖持久化抽象、数据库上下文以及所属 Provider 所需的数据库 SDK；对业务层仅暴露 Model、查询结果和数据访问异常，不暴露 Entity、`DbContext` 或 Provider 私有类型。
- **禁止事项**：Repository 不得读取 `HttpContext` 或 Claims，不得执行业务授权，不得决定账号锁定、Agent 发布或其他业务状态流转，不得编排多个 Repository 的事务，不得调用外部模型或业务 Service。
- **职责判定**：Repository 决定如何读取或保存数据；Service 决定是否执行该操作，以及用例中的执行顺序和业务后果。

## 文档规范

- 不要每次修改代码之后都要写总结文档；是否需要写总结文档取决于修改的复杂程度和影响范围，并需获取用户确认后再写
- `docs/` 目录已有 H1–H6 阶段产出体系（见 AGENTS.md §4），过程性/临时性说明一律追加到对应阶段文档里的"决策更新"小节，不要另建新文档
- 文档里的架构图 / 拓扑图 / 流程图统一用 [Mermaid](https://mermaid.js.org/) 语法（` ```mermaid ` 代码块），不要用 ASCII art 画框图；Mermaid 在 GitHub / VS Code 里能正确渲染，ASCII 框图混排中英文字符宽度不一致时容易错位，且后续改动难以维护对齐

## C# 代码规范

- 所有 C# 文件使用文件作用域命名空间（file-scoped namespaces）
- 将警告视为错误处理（`TreatWarningsAsErrors=true`）
- 遵循 `.editorconfig` 中的约定（如果仓库尚无 `.editorconfig`，创建文件前先跟我确认要不要建、参照哪个基线）
- 写代码之前务必谨慎思考，保证逻辑正确，且没有语法错误
- 需要写出完整的解决方案并标明清晰的中文注释，注释需符合 C# 注释风格标准
- **注释只写当前业务逻辑 / 算法本身的描述，不要写"为什么改成这样""历史上是从 A 改成 B"这类决策叙事或自我辩解**；
  已有的 `（ADR-XXX §…）`/`（HD-XXX §…）` 追溯引用是既定规范（见 AGENTS.md §5 追溯链要求），可以保留，
  但不要在此基础上展开大段"因为……所以……"的论证性文字
- 系统中的错误消息和抛出的异常信息使用英文
- 所有公共方法和类应包含 XML 文档注释，类、接口、方法写出完整的 `summary` 注释；方法的每个参数和返回值也要写清楚，例如：
  ```csharp
  /// <summary>
  /// 将项目加入队列
  /// </summary>
  /// <param name="item">要入队的项目</param>
  /// <param name="queueName">队列名称</param>
  /// <param name="cancellationToken">取消令牌</param>
  /// <returns>表示异步操作的任务</returns>
  Task EnqueueAsync(T item, string? queueName = null, CancellationToken cancellationToken = default);
  ```
- 如果是属性，根据属性的读写类型，注释中需要写出"获取……"/"设置……"/"获取或设置……"
- 访问类成员时请使用 `this.` 前缀（主构造函数捕获的参数除外，参数本身按惯例不加 `this.`）
- 所有异步方法的名称应以 `Async` 结尾
- **使用显式类型，而不要使用 `var`**
- 保持代码风格一致，注意空行和缩进
- 对于实现了 `System.IDisposable` 的类型，合理使用 `using` 关键字
- 合理使用 nullable 类型（项目已全局 `<Nullable>enable</Nullable>`）
- 合理使用 `async` 操作和 `ConfigureAwait(false)`
- 实现接口成员时合理使用 `<inheritdoc />` 继承注释，避免重复整段 XML 文档
- 避免使用 emoji 表情，无论是在日志还是注释中
- 在功能稳定的前提下，尽量保证较好的性能
- 在 C# 中始终使用主构造函数（Primary Constructor）
- 每次修改代码后都要运行 `dotnet build`
- 始终优先使用 `System.Text.Json`，而不是 Newtonsoft.Json
- 新建的类和接口都应放在单独的文件中（一个文件一个类型）
- 如果成员可以声明为 `static`，则应始终声明为 `static`
- 所有未被继承的私有/内部类都声明为 `sealed`
- 始终检查自己的代码，确保其一致性、可维护性和可测试性
- 如果需求描述不明确或缺乏足够上下文，务必主动询问以获得澄清
- 所有 JSON 操作仅使用 `System.Text.Json`
- EF Core Migration 必须通过 `dotnet ef migrations add` 由 EF Core CLI 生成，禁止手写、复制或人工创建 Migration、Designer 与 ModelSnapshot 文件；生成后仅当 EF Core CLI 无法表达必要的数据转换时，才允许对生成结果做最小调整，并必须说明原因与验证方式
- 先确保代码质量和功能正确，最终的文档需要获取用户确认

## 前端代码规范（`src/app/desktop/`，尚未开工，先定规矩）

- 前端是 **Electron 应用**（Electron + React + Vite + TypeScript，见 AGENTS.md §2.1），不是普通的纯浏览器 SPA——写代码和评审时务必区分：
  - **主进程（main process）**：`electron/` 目录，Node.js 环境，可以访问文件系统/系统 API，负责窗口管理、自动更新、跨锁屏长连接保活等（见 [ADR-011](../docs/03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md)）
  - **渲染进程（renderer process）**：`src/features/`、`src/shared/` 等，React 代码，运行在 Chromium 沙箱里，**不能**直接访问 Node.js/文件系统 API
  - 主进程与渲染进程之间只能通过 **IPC**（`ipcMain`/`ipcRenderer`，或社区推荐的 `contextBridge` 封装）通信，禁止关闭 `contextIsolation`、禁止开启 `nodeIntegration`（Electron 安全基线，避免渲染进程直接拿到 Node.js 能力）
  - 涉及自动更新、系统托盘、原生菜单、跨锁屏保活这类"只有桌面应用才有"的能力，一律放在主进程里实现，不要试图在渲染进程里模拟
- 文件命名使用 kebab-case
- 代码和注释符合社区统一标准规范
- 视觉风格参考 Ant Design Pro（AGENTS.md §2.1 已定）

## 单元测试规范

- 使用 MSTest 写出完整的单元测试（项目已用 MSTest.Sdk + Microsoft.Testing.Platform，见 AGENTS.md §2.4）
- 为每个测试添加 Arrange、Act 和 Assert 注释
- 确保所有未被继承的私有测试辅助类都声明为 `sealed`
- 验证每个测试确实测试了目标行为：不要写"创建 mock、调用 mock、验证 mock 被调用"但没有真正触达被测代码的测试，也不要写测试语言特性本身（编译器就能捕获的问题）的测试
- 避免在测试中添加过多注释，优先使用清晰易懂的代码
- 遵循所在项目或类中已有的单元测试模式，新增测试时保持一致性
- **修复未通过的单元测试时，只能修改测试代码**；如果确实需要修改业务逻辑代码，必须先获得用户确认
