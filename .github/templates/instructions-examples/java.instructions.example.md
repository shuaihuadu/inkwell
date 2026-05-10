---
applyTo: 'src/**/*.java'
---

<!--
这是一份 Java 代码规范的**参考样例**，不是 Harness Engineering 规范的一部分。

使用方式：
1. 把本文件复制到 .github/instructions/java.instructions.md（去掉 .example 后缀）
2. 按你项目的实际栈裁剪每一节，删除不适用的条目
3. 真正的硬约束应放进 Checkstyle / SpotBugs / google-java-format / Maven enforcer，让 CI 强制
   instructions 文件只对 AI 生效，不对人/CI 生效——能让 Linter 干的事不要写在这里
4. 完成后由项目负责人评审，与 AGENTS.md 的项目身份签字位同源

Harness 不维护本样例的内容深度；详细风格请查 Effective Java（Joshua Bloch）、
Google Java Style Guide、Oracle Java Language Specification、Checkstyle / SpotBugs 规则等权威来源。
-->

# Java 编码规范

> 与 [`coding-discipline.instructions.md`](./coding-discipline.instructions.md)（流程纪律）叠加生效。
> 风格层面的硬约束以 `Checkstyle` + `SpotBugs` + `google-java-format` 为准，本文件只补充 Linter 不便强制的部分。

## 1. 命名

- 类 / 接口 / enum / record：[ 待填，建议 PascalCase ]
- 方法 / 字段 / 局部变量：[ 待填，建议 camelCase ]
- 常量（`static final`）：[ 待填，建议 SCREAMING_SNAKE_CASE ]
- 包名：[ 待填，建议全小写、反向域名，如 `com.example.module` ]
- 类型参数：[ 待填，建议短大写，如 `T`、`E`、`K`/`V` ]
- 测试类：`<TargetClass>Test`，与被测类同包

## 2. 错误处理

- 受检异常 vs 非受检：[ 待填，建议大部分用 `RuntimeException` 子类；只在调用方真有恢复路径时才用 checked ]
- 异常分层：[ 待填，建议自定义业务异常基类继承 `RuntimeException`，基础设施异常不上抛到 API 边界 ]
- 不吞异常：`catch` 后必须重新抛 / 写结构化日志 / 转换异常类型，三选一
- 不空 `catch (Exception e) { }`：若必须吞，注释解释原因
- try-with-resources：所有 `Closeable` 必须用 try-with-resources，禁止裸 `try/finally close()`
- 不用异常做控制流：用 `Optional<T>` / `Result` 风格的返回值显式表达失败

## 3. 并发与异步

- 不裸 `Thread`：用 `ExecutorService` / 虚拟线程（Java 21+） / `CompletableFuture`
- 共享状态：`java.util.concurrent` 类（`ConcurrentHashMap` / `AtomicReference`），不裸 `synchronized` 大块
- 不可变优先：fields 默认 `final`；DTO 用 `record`
- `volatile` 与 happens-before：仅在确实需要 visibility 保证时使用，并写注释说明原因

## 4. 测试

- 测试框架：[ 待填，JUnit 5 / TestNG 选一，建议 JUnit 5 ]
- 测试方法命名：[ 待填，建议 `methodName_scenario_expectedBehavior` 或 `should_X_when_Y` ]
- 三段式：Arrange / Act / Assert（用空行或 `// arrange` 注释分隔）
- Mock：[ 待填，Mockito / EasyMock 选一，建议 Mockito ]，只 mock 你拥有的接口
- 断言：用 `AssertJ` / `assertThat`，可读性优于 `Assert.assertEquals`
- 参数化测试：`@ParameterizedTest` + `@MethodSource` / `@CsvSource`
- 覆盖率门槛：见 CI 配置（不在本文件重复）

## 5. 不在此文件强制

下列项交给自动化工具强制，不写在 instructions 里：

- 格式化（缩进 / 大括号 / import 排序）→ `google-java-format` 或 Spotless 插件
- 静态检查（未使用 import / 圈复杂度 / 命名）→ `Checkstyle` + `google_checks.xml` 或自定义
- bug 模式 → `SpotBugs` + `Find Security Bugs`
- 依赖管理 → `pom.xml` / `build.gradle` + Maven Enforcer / Gradle versions plugin
- Java 版本 / source/target → `<source>` / `<target>` 或 Gradle `sourceCompatibility`
- 测试覆盖率 → CI 任务（`JaCoCo` + 阈值断言）
