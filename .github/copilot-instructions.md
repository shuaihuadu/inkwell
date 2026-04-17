# GitHub Copilot Instructions

我们的沟通请使用简体中文进行。

本项目Inkwell 是模拟了一个**企业级 AI 内容生产平台**的完整实际应用场景。

后台是基于.NET Core 10，使用C#实现，前端是使用React和TypeScript实现的SPA应用程序。
后台代码位于：src
前端的UI设计是基于Ant Design组件库进行构建，
前端代码位于：src\app\webapp\src
相关的参考资料如下：
Ant Design组件库：https://ant.design/docs/spec/introduce-cn/
智能体对话界面：https://ant-design-x.antgroup.com/components/introduce-cn

在为本仓库贡献代码时，请遵循以下指南：

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
