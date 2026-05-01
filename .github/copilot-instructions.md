# inkwell - Copilot Instructions

> <未配置>

本仓库采用 [Harness Engineering 规范](../.harness-engineering) 作为 AI 协作的工程骨架。下方规则面向 GitHub Copilot，按"目录"原则编写：只列项目身份与硬约束，详细规则全部指向源文档。

## 1. 项目身份

- **名称**：inkwell
- **主语言**：C#
- **技术栈**：.NET 10
- **测试命令**：`dotnet test`
- **代码风格检查**：`dotnet format --verify-no-changes`
- **规范出处**：见 .harness-engineering

## 2. 硬约束（不可绕过）

- 所有变更必须能映射到一条 `REQ-NNN`（需求编号）。无对应需求的请求一律先反问，不直接写代码。
- 提交信息必须满足 [`commit-format.instructions.md`](./instructions/commit-format.instructions.md) 的字段要求：`Design / Tests / Verify / Docs / Risk / Task` 六字段齐备。
- 修改源码前先运行 `dotnet test`，确保起点干净；提交前再次运行，确认未引入回归。
- `docs/` 是事实来源（source of truth）：先改文档再改代码，不要让代码先于设计落地。
- 不在 `docs/04-detailed-design/` 之外的位置放设计内容；不在 `AGENTS.md` 里复述能用 Lint / Hooks / CI 强制的规则。

## 3. 何时切换到专用 chatmode

| 场景               | 使用模式                              |
| ------------------ | ------------------------------------- |
| 起草 / 修订需求    | `RequirementsInterviewer`（如已部署） |
| 评审 H3 详细设计   | `DesignReviewer`                      |
| 反推测试用例       | `TestCaseAuthor`                      |
| 准备提交 / 评审 PR | `CommitAuditor`                       |
| 撰写 release notes | `ReleaseNoteWriter`（如已部署）       |
| 其他编码           | 默认 chatmode（即本指令）             |

> 默认 chatmode 下不要尝试代行上述 Agent 的工作。Agent 的判定逻辑写在各自的 `chatmode.md` 里，单独运行才能保证机械化。

## 4. 关键文档入口

- 阶段细则：`../.harness-engineering/docs/stages.md`（H1–H6）
- 目录规范：`../.harness-engineering/docs/repo-layout.md`
- 反模式：`../.harness-engineering/README.md` §6.4
- 项目 ADR：`docs/03-architecture/adr/`

> `.harness-engineering` 默认指向已 vendor 的本地副本（`.harness-engineering/`）；如使用 `-NoVendor` 则指向远端 URL。

## 5. 反模式（出现即拒绝）

照搬 [Harness Engineering 规范 §6.4](../.harness-engineering/README.md) 的五条："杂烩会话 / 反复纠错 / 过量规则文件 / 先信后验缺口 / 无界探索"。当用户请求触发任一条时，先指出反模式，再给替代做法。
