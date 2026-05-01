---
applyTo: 'src/**'
---

# 源码编写规范

本规则覆盖 `src/**` 下的全部源码。以下要求与 Harness Engineering 规范 §6（H3 详细设计）和 [`docs/stages.md`](../../.harness-engineering/docs/stages.md) §8（H5 编码与自验证）一致，AI 写代码时必须遵守。

## 1. 改动前

- **必须**先在 `docs/06-implementation/coding-tasks.md` 或 `exec-plans/active/` 找到对应任务；找不到就反问，不要自行扩展范围。
- **必须**先确认 `docs/04-detailed-design/` 中相关章节存在并已 reviewed。
- **必须**列出"允许修改的文件"与"禁止修改的文件"，等用户确认后再动手。

## 2. 编码

- 主语言：C#
- 遵守仓库 `AGENTS.md` 的命名 / 模块 / 注释约定（不在此重复）
- 一次只完成一个编码单元；不要"顺手"重构无关代码
- 同步生成或更新对应测试代码，不允许"先合代码再补测试"

## 3. 自验证

- 提交前必须运行 `dotnet test` 并贴出真实输出
- 测试失败时分析原因再改代码，不要靠"再试一次"通过
- 风格检查：`dotnet format --verify-no-changes`，警告视作错误处理

## 4. 反模式（出现即停止）

照搬 [Harness Engineering 规范 §6.4](../../.harness-engineering/README.md) 的五条：

- **杂烩会话**：当前对话已混入无关任务时，提醒用户开新会话或 `/clear`
- **反复纠错**：同一问题超过两次未调通时停手，提醒用户重置上下文
- **过量规则文件**：不要往 `AGENTS.md` 或本指令里塞能用 Lint / Hooks 表达的规则
- **先信后验缺口**：未跑通验收命令的实现不算完成
- **无界探索**：调研类请求必须给出范围（具体路径或 grep 关键字），否则反问

## 5. 不在范围内

- 评估代码是否优雅 → 由人工 Code Review
- 修改业务需求 → 由 H1 RequirementsInterviewer
- 决定架构方案 → 由 H2 设计阶段
