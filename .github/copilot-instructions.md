# Copilot Instructions

本仓库采用 [Harness Engineering 规范](../.harness-engineering/HANDBOOK.md) 作为 AI 协作的工程骨架。下方规则面向 GitHub Copilot，按"目录"原则编写：只列硬约束，详细规则全部指向源文档。

> 项目身份与技术栈以仓库根 `README.md` / `AGENTS.md` 为准，本指令不重复维护，避免漂移。

## 1. 硬约束（不可绕过）

- 所有变更必须能映射到一条 `REQ-NNN`（需求编号）。无对应需求的请求一律先反问，不直接写代码。
- 提交信息必须满足 [`commit-format.instructions.md`](./instructions/commit-format.instructions.md) 的字段要求：`Design / Tests / Verify / Docs / Risk / Task` 六字段齐备。
- 修改源码前先运行 `dotnet test`，确保起点干净；提交前再次运行，确认未引入回归。
- 风格检查：`dotnet format --verify-no-changes`，警告视作错误处理。
- `docs/` 是事实来源（source of truth）：先改文档再改代码，不要让代码先于设计落地。
- 不在 `docs/04-detailed-design/` 之外的位置放设计内容；不在 `AGENTS.md` 里复述能用 Lint / Hooks / CI 强制的规则。

## 2. 何时切换到专用 Custom Agent

当前项目已装的 Custom Agent（在 VS Code Copilot Chat 输入框下方的 Agent 下拉菜单中选择）。Agent 名以 `h<阶段号>-` 开头，对应 [Harness 阶段](../.harness-engineering/docs/stages.md) H1–H6，横切阶段用 `hx-`：

| 场景                             | 使用 Agent                    |
| -------------------------------- | ----------------------------- |
| H1 需求访谈 / 模糊描述变需求初稿 | `h1-requirements-interviewer` |
| H1↔H3 仓库影响面映射             | `h1-repo-impact-mapper`       |
| H2 架构说明 / 技术选型 / ADR     | `h2-architect-advisor`        |
| H3 评审详细设计                  | `h3-design-reviewer`          |
| H4 反推测试用例                  | `h4-test-case-author`         |
| H5 按任务简报编码 + 自验证       | `h5-coding-executor`          |
| H5 提交 / 评审 PR                | `h5-commit-auditor`           |
| H6 发布说明 / 追溯矩阵回写       | `h6-release-note-writer`      |
| 横切文档治理 / GC                | `hx-doc-gardener`             |
| 其他编码                         | 默认 Agent（即本指令）        |

> 默认 Agent 下不要尝试代行上述专用 Agent 的工作。专用 Agent 的判定逻辑已 inline 进各自的 `.github/agents/*.agent.md` 文件，单独运行才能保证机械化。

## 3. 何时使用 Slash 命令

| 命令          | 干什么                                                            |
| ------------- | ----------------------------------------------------------------- |
| `/new-task`   | 起一个 H5 编码任务：草稿 + `task-board.md` 登记，**不直接改代码** |
| `/run-gate`   | 按 phase-gate-checklist 核对当前阶段能否进下一阶段                |
| `/log-review` | 把会议 / PR 评审誊到 `docs/07-reviews/YYYY-MM-DD-*.md`            |
| `/sync-board` | 审计 `task-board.md` 与代码 / commit 的对齐，列失同步项           |

## 4. 关键文档入口

- 操作手册（10 分钟上手）：[`HANDBOOK.md`](../.harness-engineering/HANDBOOK.md)
- 阶段细则：[`docs/stages.md`](../.harness-engineering/docs/stages.md)（H1–H6）
- 目录规范：[`docs/repo-layout.md`](../.harness-engineering/docs/repo-layout.md)
- 技术债务 GC：[`docs/tech-debt-gc.md`](../.harness-engineering/docs/tech-debt-gc.md)
- 项目 ADR：`docs/03-architecture/adr/`
- 模板：[`.github/templates/`](./templates/)（任务卡 / 阶段门 / 评审记录 / 任务板）
- 可复用操作型 SOP（Skills）：`.github/skills/<name>/SKILL.md`。Copilot 按 description 语义命中后自动加载，也可 `/<skill-name>` 显式调用。

## 5. 反模式（出现即拒绝）

五条："杂烩会话 / 反复纠错 / 过量规则文件 / 先信后验缺口 / 无界探索"。当用户请求触发任一条时，先指出反模式，再给替代做法。详见 [Harness Engineering README 第 6.4 节](https://github.com/shuaihuadu/harness-engineering#64-五大反模式与替代做法)。
