---
applyTo: 'src/**/*.rs'
---

<!--
这是一份 Rust 代码规范的**参考样例**，不是 Harness Engineering 规范的一部分。

使用方式：
1. 把本文件复制到 .github/instructions/rust.instructions.md（去掉 .example 后缀）
2. 按你项目的实际栈裁剪每一节，删除不适用的条目
3. 真正的硬约束应放进 rustfmt + clippy + Cargo.toml，让 CI 强制
   instructions 文件只对 AI 生效，不对人/CI 生效——能让 Linter 干的事不要写在这里
4. 完成后由项目负责人评审，与 AGENTS.md 的项目身份签字位同源

Harness 不维护本样例的内容深度；详细风格请查 Rust API Guidelines、
Rust Book、clippy lint 列表、rust-lang/rfcs 等权威来源。
-->

# Rust 编码规范

> 与 [`coding-discipline.instructions.md`](./coding-discipline.instructions.md)（流程纪律）叠加生效。
> 风格层面的硬约束以 `rustfmt` + `clippy` + `Cargo.toml` 为准，本文件只补充 Linter 不便强制的部分。

## 1. 命名

- crate / 模块：[ 待填，建议 snake_case ]
- 类型 / trait / enum：[ 待填，建议 UpperCamelCase ]
- 函数 / 变量 / 字段：[ 待填，建议 snake_case ]
- 常量 / static：[ 待填，建议 SCREAMING_SNAKE_CASE ]
- 类型参数：[ 待填，建议短大写，如 `T`、`E`、`Item` ]
- 不在本文件重复 `clippy::module_name_repetitions` 等已强制的命名规则

## 2. 错误处理

- **禁用 `unwrap()` / `expect()` 在 library 代码**：用 `?` 操作符传播；二进制 `main` / 测试中可酌情 `expect("reason")`
- 错误类型：[ 待填，`thiserror` / `anyhow` / 自定义 enum 选一并固定使用边界 ]
  - 通常：library 用 `thiserror` 派生具体错误类型；application 用 `anyhow::Error` 兜底
- 错误链：用 `#[source]` / `Box<dyn Error + Send + Sync>` 保留 cause chain
- 不 `panic!` 在 library 代码：除非真正不可恢复（如 invariant 违反）
- `Result` 不忽略：`#[must_use]` 默认在 `Result` 上生效；用 `let _ = ...` 也要带注释

## 3. 所有权与异步

- 借用优先：函数参数优先 `&T` / `&mut T`，必要时才 `T`（消费）
- 生命周期：能 elision 就不显式写；写就写清楚 `'a` 的语义
- `async`：用 `tokio` / `async-std` 选一；不混用
- `Send` / `Sync` bounds：在 `async fn` 返回的 future 上显式标注 trait bound 避免编译歧义
- 不在 hot path 用 `Arc<Mutex<T>>`：考虑 `RwLock` / `parking_lot` / channel

## 4. 测试

- 单元测试：`#[cfg(test)] mod tests { ... }` 与被测代码同文件
- 集成测试：`tests/<name>.rs`
- 测试函数命名：[ 待填，建议 `fn test_<method>_<scenario>` 或 `<scenario>_<expected>` ]
- 属性测试：[ 待填，`proptest` / `quickcheck` 选一（如有需要） ]
- Mock：[ 待填，`mockall` / 接口注入 / 测试 trait 选一 ]
- doctest：public API 鼓励有 `# Examples` doctest
- 覆盖率门槛：见 CI 配置（不在本文件重复）

## 5. 不在此文件强制

下列项交给自动化工具强制，不写在 instructions 里：

- 格式化（缩进 / 行长 / 大括号 / use 排序）→ `rustfmt`（`rustfmt.toml` 配置）
- 静态检查 → `cargo clippy -- -D warnings`，启用 `clippy::pedantic` 看心情
- 编译告警 → `RUSTFLAGS="-D warnings"` 入 CI 或 `Cargo.toml` `[lints.rust] warnings = "deny"`
- Edition / MSRV → `Cargo.toml` `edition` + `rust-version`
- 依赖审计 → `cargo deny` / `cargo audit`
- 测试覆盖率 → CI 任务（`cargo llvm-cov` + 阈值断言）
