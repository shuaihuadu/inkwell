# Inkwell

> AI 协作单一事实源——遵循 [AGENTS.md 跨工具开放约定](https://agents.md/)（OpenAI / Cursor / Factory，2025-08）。
> 本仓库采用 [Harness Engineering 规范](.he/HANDBOOK.md) 作为工程骨架。

## 1. 项目身份

<!--
项目负责人签字位。AI 不能替写。一句话讲清：目标用户是谁、核心价值是什么。
最多 2 行。模板示例：
> Inkwell 是给个人技术博客作者用的 AI 内容工厂：
> 输入题目 → AI 多轮迭代磨稿 → 输出可直接发布的 Markdown。

完成签字后下一步：
1. 改 docs/01-requirements/repo-impact-map.md 第 3 节，把 GAP-001 标为"已关闭（写日期）"，
   同时把 0.1 节"AGENTS.md"行从"无"改"有"。
2. 提交一条 commit 记录这次签字，参考 .github/instructions/commit-format.instructions.md。
3. 第 4 节"模块边界 / 禁区"维持 TODO 不动——H2 选型完成后再回来填。
-->
> Inkwell 是 Harness Engineering + Microsoft Agent Framework 的 dogfooding 项目：
> 用真实项目验证这套规范+工具链端到端能不能打造一个可工作的"智能体工厂"。
>
> **self-review**：2026-05-07 已确认

**当前阶段**：H1（dogfooding Harness Engineering 流程，custom-agent 管理 UI 原型审完，待 H2 启动）。

## 2. AI 工具与入口

本项目当前**唯一启用**的 AI 编码工具是 **GitHub Copilot Chat**。

- 工具入口：[`.github/copilot-instructions.md`](.github/copilot-instructions.md)（Copilot 自动加载）
- 配套 Custom Agent / Skill / Prompt：落在 `.github/agents/` `.github/skills/` `.github/prompts/`，速查见 [`.he/HANDBOOK.md` 第 6 节](.he/HANDBOOK.md)
- 工具白名单 / 自定义改法：[`.he/HANDBOOK.md` 第 7 节](.he/HANDBOOK.md)

> 切到 Codex / Claude Code / Cursor 等其他工具时，本文件作为跨工具事实源不变；新工具的入口文件用 import 指向本文件，避免维护两份。

## 3. 硬约束

详见 [`.github/copilot-instructions.md` 第 1 节](.github/copilot-instructions.md)（六字段提交、`docs/` 是真相源、文档先行、`dotnet test` / `dotnet format` 必跑）。本文件不复述，避免漂移。

## 4. 模块边界 / 禁区

<!--
H2 架构选型完成后由项目负责人补充：
  - 哪些目录是其他模块的私有领域，跨模块调用的允许 / 禁止清单
  - 哪些目录禁止 AI 自动修改（须人工评审）
  - 与外部依赖（登录平台 / 模型网关 / 搜索服务商）耦合的边界

H1-RepoImpactMapper、H3-DesignReviewer、H5-CodingExecutor 都依赖本节做边界判断。

完成本节签字后下一步：
1. 跑一次 /run-gate H2 做机械复核。
2. 评审 docs/03-architecture/ 下产出，把 status: draft → reviewed。
3. 切到 H3 起草 docs/04-detailed-design/<feature>/HD-NNN.md。
-->

> **self-review**：2026-05-07确认当前 greenfield，暂无既有禁区。

## 5. 文档入口

| 内容                    | 位置                                                                                          |
| ----------------------- | --------------------------------------------------------------------------------------------- |
| 操作手册（10 分钟上手） | [`.he/HANDBOOK.md`](.he/HANDBOOK.md)                        |
| 阶段细则 H1–H6          | [`.he/docs/stages.md`](.he/docs/stages.md)                  |
| 项目目录规范            | [`.he/docs/repo-layout.md`](.he/docs/repo-layout.md)        |
| 当前需求                | [`docs/01-requirements/requirements.md`](docs/01-requirements/requirements.md)                |
| UI / 用户流 / 验收      | [`docs/01-requirements/`](docs/01-requirements/)（ui-spec / user-flow / acceptance-criteria） |
| 仓库影响图              | [`docs/01-requirements/repo-impact-map.md`](docs/01-requirements/repo-impact-map.md)          |
| H1 评审纪要             | [`docs/07-reviews/`](docs/07-reviews/)                                                        |
| 任务看板                | [`docs/06-tasks/task-board.md`](docs/06-tasks/task-board.md)                                  |
| 架构设计                | [docs/03-architecture/](docs/03-architecture/)（H2 self-review approved 2026-05-07）          |
| 详细设计                | `docs/04-detailed-design/`（H3 完成后补）                                                     |
| 测试设计                | `docs/05-test-design/`（H4 完成后补）                                                         |
| 发布说明                | `docs/08-releases/`（H6 完成后补）                                                            |

## 6. 提交规范

详见 [`.github/instructions/commit-format.instructions.md`](.github/instructions/commit-format.instructions.md)。每条 commit 必须含 `Design / Tests / Verify / Docs / Risk / Task` 六字段。

---

> 本文件是**采用方自有产物**，不在 `.he/manifest.json` 登记，不会被 `install.ps1` / `uninstall.ps1` 覆盖。维护它是项目负责人的责任。
