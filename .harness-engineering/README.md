# Harness Engineering 规范

版本：v0.0.1  
状态：试行（条款会在 v1.0 前持续收敛）  
适用范围：使用 AI 参与需求、设计、编码、测试、交付和文档维护的软件工程团队

## TL;DR

- **是什么**：一份将 Harness Engineering 思想落到团队 SDLC 的方法论 + 一组可直接使用的 Agent 提示词与文档模板 + 一套把它们一键铺到你仓库的脚本。
- **为谁写**：在真实项目里和 AI 协作，并希望让交付保持可追溯、可评审、可维护的工程团队与个人开发者。
- **解决什么**：把"AI 写得很快但难以验证 / 难以合并 / 难以维护"的现实问题，转化为一条由文档、评审、测试和提交记录组成的硬轨道。
- **核心模型**：按 Agent **行动时序**划分的三层 Harness——约束层（行动前）/ 反馈层（行动中）/ 质量门禁层（行动后），配套 8 个职责单一的 Agent（H1–H6 + 横切两个）。

### 这是什么 / 不是什么

| 这是                                                                                       | 这不是                                                               |
| ------------------------------------------------------------------------------------------ | -------------------------------------------------------------------- |
| 一份**团队 AI 工程规约**：文档结构 + 评审节奏 + Agent 角色定义                             | AI Agent 框架 / SDK / 运行时（不替代 LangGraph、Agent Framework 等） |
| 一套**多工具分发器**：同一份角色定义可同步成 Copilot chatmode / Claude Code subagent / ... | 一键万能解（chatmode 是"打开才用"，没有团队文化基础时就是死代码）    |
| **可被采纳为 standard 的规范**：`.harness-engineering/` 目录直接提交进你的仓库   | prompt 库（提供的是结构与契约，不是预制 prompt 的集合）              |
| 与 `AGENTS.md` / `copilot-instructions.md` / `CLAUDE.md` 等运行时机制**互补**              | 自动化质量门禁（CI / Hooks / Lint 仍需各项目自行接入）               |

## 快速开始（一键集成）

仓库根 `install.ps1` / `install.sh` 把规范文档 vendor 进你的项目并为指定的 AI 编码工具渲染配置：

```powershell
# Windows / 跨平台（PowerShell 7+）
git clone https://github.com/shuaihuadu/harness-engineering.git
cd harness-engineering
./install.ps1 -TargetRepo D:\Path\To\YourRepo
```

```bash
# Linux / macOS（依赖 jq）
git clone https://github.com/shuaihuadu/harness-engineering.git
cd harness-engineering
./install.sh --target-repo /path/to/your/repo
```

默认会做四件事：

1. **Vendor 规范文档**：把 `agents/` `docs/` `templates/` `README.md` 同步进 `<your-repo>/.harness-engineering/`（与安装清单同住，一个隐藏目录装下所有 harness 产物）
2. **渲染 Copilot 配置**：`.github/copilot-instructions.md` + `.github/instructions/*`，链接指向上一步的 vendor 目录
3. **不默认安装任何 chatmode**：自 v0.0.1 起，chatmode 必须显式指定（如 `-Chatmodes commit-auditor,design-reviewer` 或 `-Chatmodes all`），避免在用户未知情时往 `.github/chatmodes/` 落文件
4. **写入安装清单**：`<your-repo>/.harness-engineering/manifest.json` 记录本次写入的所有文件（含 sha256）+ 本次填入的占位符（`replacements`），供 `uninstall` 使用，并在下次重装时自动预填

### 占位符填入策略

需要 7 个占位符（PROJECT_NAME / PROJECT_ONE_LINER / PRIMARY_LANGUAGE / TECH_STACK / TEST_COMMAND / LINT_COMMAND / HARNESS_REPO_REF），优先级：

```
CLI 参数  >  上次 manifest.replacements  >  自动探测  >  交互输入 / 空（→ <未配置>）
```

- **自动探测**：脚本会读取 `*.csproj` / `package.json` / `pyproject.toml` / `go.mod` / `Cargo.toml` / `global.json` 等推断项目名、主语言、技术栈、测试和 Lint 命令。检测到的值作为 prompt 默认（回车采纳）
- **重装零输入**：上次安装的 `replacements` 写在 manifest 里，再次运行 `install` 时会自动作为最高优先级默认，覆盖探测结果
- **可选字段允许留空**：除 `PROJECT_NAME` 和 `HARNESS_REPO_REF` 外，其余字段留空会被填为字面量 `<未配置>`，便于后续用 `grep '<未配置>'` 一次性补充
- **零交互**：`-NonInteractive` 跳过所有 prompt（探测出什么用什么，仍缺则填 `<未配置>`）；`-Force` 隐含 `-NonInteractive` 并自动覆盖一切冲突

需要卸载时：

```powershell
./uninstall.ps1 -TargetRepo D:\Path\To\YourRepo            # 安全卸载（用户改过的文件默认保留）
./uninstall.ps1 -TargetRepo D:\Path\To\YourRepo -Force     # 一并清理用户改过的文件
./uninstall.ps1 -TargetRepo D:\Path\To\YourRepo -DryRun    # 只预览
```

更多用法（多 target、占位符、`-Force` / `-DryRun` / `-NoVendor` 等）见 [`agents/_integrations/README.md`](agents/_integrations/README.md)。

### 关于 vendor 模式

默认行为是把 `agents/`、`docs/`、`templates/`、`README.md` **整份复制**到你的仓库 `.harness-engineering/` 下，这意味着：

- ✅ 离线可用、链接在采用方仓库内可点
- ✅ 规范副本与采用方仓库同步进入版本控制，可 diff、可回滚
- ✅ 与 `manifest.json` 同住一个隐藏目录，不污染 `docs/` 树；整块卸载干净
- ⚠️ 采用方仓库会多 ~300KB Markdown
- ⚠️ 规范升级 = 每个采用方仓库重跑一次 `install`（manifest 会自动 diff，已存在且未改的文件会 skip）

> 想换个目录名？交互模式下 vendor 路径会有 prompt（回车用默认 `.harness-engineering`）；非交互可显式传 `-VendorHarnessTo <path>` / `--vendor-harness-to <path>`。

如果你希望不在采用方仓库落 vendor 副本（例如让链接指向 GitHub 远端），使用：

```powershell
./install.ps1 -TargetRepo X -NoVendor -HarnessRepoRef https://github.com/shuaihuadu/harness-engineering/blob/main
```

`-NoVendor` 模式下 `{{HARNESS_REPO_REF}}` 会被替换成你提供的 URL；缺点是 chatmode 里的链接需要联网才能跳转。两种模式适合不同场景，按需选择。

## 如何使用本仓库

本仓库是**规范型仓库**（specification repo），主体内容即 README 本身。常见使用方式有三种，可单独或组合使用：

1. **作为方法论参考**：通读 README，按 §1–§3 把三层 Harness 与 H1–H6 流程映射到自己团队当前的 SDLC，识别缺口。
2. **作为 Agent 提示词模板**：直接复用 [`agents/`](agents/) 下 8 个 Agent 的 `AGENT.md` + `prompt.md`，按需替换项目专属术语后接入到 Copilot Chat / Claude Code / Cursor / 自建工作流。
3. **作为评审 / 门禁清单**：把 [`templates/phase-gate-checklist.md`](templates/phase-gate-checklist.md) 与 [`templates/review-record.md`](templates/review-record.md) 接入团队的 PR 模板与阶段评审，让规范从纸面落到流程。

> 本规范不强制全部采用。建议从单一痛点切入（例如先落 H4 测试用例 + H5 编码约束），跑通再扩展。

## 仓库结构

```
harness-engineering/
├── README.md                       # 规范主体（你正在阅读的文件）
├── agents/                         # 8 个 Agent 的角色规格与提示词
│   ├── README.md                   # Agent 协作拓扑与 H1–H6 编号说明
│   ├── _shared/                    # 跨 Agent 共享：术语表、I/O 契约、工具词表、模板
│   ├── _integrations/              # 与 Copilot / Claude Code / Cursor / AGENTS.md 的对接说明
│   ├── requirements-interviewer/   # H1 需求访谈
│   ├── repo-impact-mapper/         # H1 代码库影响分析
│   ├── design-reviewer/            # H3 设计评审
│   ├── test-case-author/           # H4 测试用例
│   ├── coding-executor/            # H5 编码执行
│   ├── commit-auditor/             # H5 提交审计
│   ├── release-note-writer/        # H6 发布说明
│   └── doc-gardener/               # 横切：文档治理
└── templates/                      # 可直接复制使用的工作产物模板
    ├── ai-task-brief.md            # AI 任务简报（H5 入口）
    ├── phase-gate-checklist.md     # 阶段门禁清单
    └── review-record.md            # 评审记录
```

每个 Agent 目录均包含：`AGENT.md`（角色定位、输入输出、工作约束）+ `prompt.md`（可直接投喂给 LLM 的系统提示词）。

## 1. 总览

### 1.1 概念背景

Harness Engineering（中文社区暂无官方译名，本规范采用"驾驭工程"，与 OpenAI 标语 "Humans steer" 同源）这一术语在 2026 年 2 月由 Mitchell Hashimoto 在其个人博客中首次提出，并由 OpenAI（Ryan Lopopolo）在 2026 年 2 月 11 日发布的文章中给出正式定义。LangChain 把它精炼为一个公式：**Agent = Model + Harness**，OpenAI 给出的标语是 **"Humans steer. Agents execute."（人类掌舵，Agent 执行）**。

业界对 Harness Engineering 的共识定义是：

> 设计环境、约束和反馈回路，让 AI 编码 Agent 在规模化场景下保持可靠的工程学科。

本规范在此基础上按 Agent **行动时序**展开为三层（约束层 / 反馈层取自社区共识，质量门禁层引自 DevOps 词汇，三者按"行动前 / 行动中 / 行动后"分工，避免按机制划分时的归属重叠）：

- **约束层（Constraint Harness）—— 行动前**：规则文件、Lint、类型系统、AGENTS.md / copilot-instructions.md 等前馈控制，缩小 Agent 的解空间。
- **反馈层（Feedback Loop）—— 行动中**：把测试、Lint、构建错误等结构化信号回灌给 Agent，让其自我修复。
- **质量门禁层（Quality Gate）—— 行动后**：在 CI、评审、合并环节进行硬性拦截，阻止不合规产物进入主干。

主流工具厂商对这三层的落地可作为对照：

| 层次       | GitHub Copilot 体系                                                                                                                            | OpenAI Codex 体系                                                                                      | Anthropic Claude Code 体系                                    |
| ---------- | ---------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------- |
| 约束层     | `.github/copilot-instructions.md`、`.github/instructions/*.instructions.md`（带 `applyTo` 模式）、`AGENTS.md`、自定义 Chat Mode / Custom Agent | `AGENTS.md`（多级，作为"目录"而非百科全书）、自定义 Lint、结构测试、分层架构不变式                     | `CLAUDE.md`（及子目录继承）、Skills、Subagents                |
| 反馈层     | Agent Mode（`#codebase` / `#fetch` / 终端 / Problems 回灌）、MCP 工具集成、Copilot Edits 多文件迭代                                            | 测试 / 构建 / Chrome DevTools MCP / 本地可观测栈（LogQL、PromQL）、Ralph Wiggum 循环（Agent 自审自改） | Plan Mode、测试与截图验证、Subagents 隔离调查                 |
| 质量门禁层 | Copilot code review（PR 阶段）、Branch protection + Required reviewers、Secret scanning push protection、GitHub Actions CI                     | 自定义 Lint 硬失败、结构测试、后台 doc-gardening Agent、提交者路径限制                                 | Hooks（确定性拦截）、Permission allowlist、Sandbox、Auto Mode |

本规范作为上述体系中 **"文档化 + SDLC"** 这一层的通用骨架，与底层工具不冲突。

### 1.2 本规范的定位

本规范是上述思想在 **"团队 SDLC + AI 协作"** 场景下的一份具体落地方案，重点放在**约束层和质量门禁层的人工与文档化部分**：用文档、评审、测试和提交记录构成一条 Agent 在工作时必须遵守的硬轨道。它**不试图覆盖**所有运行时层面的 Harness（如 Lint 钩子、CI 拦截、运行时工具调度），这些应由具体项目的 CI/CD 流水线、`AGENTS.md` / `copilot-instructions.md` 等配套机制实现。

一句话定位：

> 本规范是 Harness Engineering 在 SDLC 维度的人工版骨架——用文档、评审、测试、提交和运行回写，把 AI 的创造力约束成可交付、可追溯、可维护的软件工程能力。

## 2. 核心原则

### 2.1 先说明，后实现

任何编码工作开始前，必须先完成需求说明、UI 说明、架构说明、详细设计和测试用例设计。

如果某项内容无法清晰说明，就不应进入编码阶段。

### 2.2 AI 生成，人类审核

AI 可以参与以下工作：

- 梳理需求
- 设计 UI 和用户流程
- 生成可交互 UI 原型
- 推演技术架构
- 生成详细设计
- 设计测试用例
- 编写代码和测试代码
- 运行测试并自我修复
- 修订文档

但每个关键阶段必须由团队审核。AI 的输出不能直接替代评审结论。

### 2.3 文档即约束

通过评审的 Markdown 文档是后续阶段的工作依据。

AI 编码时，必须严格引用已评审文档中的需求、设计、接口、数据结构、测试用例和验收标准。

### 2.4 测试先于代码

编码前必须先定义测试用例。测试用例应覆盖正常路径、异常路径、边界条件、权限边界、数据一致性和关键性能约束。

### 2.5 小步编码，小步提交

每次只让 AI 完成一个明确的工程单元。每个工程单元必须完成代码、测试、运行验证和提交记录。

推荐粒度：

- 一个程序文件及其测试
- 一个 API 端点及其测试
- 一个数据库迁移及其验证
- 一个配置项及其验证
- 一个后台任务及其测试
- 一个前端组件及其交互测试

### 2.6 运行结果回写文档

系统真实运行后，必须根据实际实现、测试结果、部署记录和运行日志修订需求、设计、测试、运维和用户说明文档。

## 3. 标准流程

本规范把 SDLC 切分为六个 Harness 阶段：

```text
H1 需求、UI 与交互原型
H2 技术架构选型
H3 详细设计
H4 测试用例设计
H5 AI 编码与自验证
H6 运行验证与文档回写
```

阶段编号 H1–H6 与文档目录 `01–07` 的映射关系如下（H1 同时产出需求和原型两类目录，因此目录数比阶段数多 1）：

| 阶段 | 文档目录                                                                   |
| ---- | -------------------------------------------------------------------------- |
| H1   | `docs/01-requirements/` + `docs/02-prototype/` + `prototypes/`（原型源码） |
| H2   | `docs/03-architecture/`                                                    |
| H3   | `docs/04-detailed-design/`                                                 |
| H4   | `docs/05-test-design/`                                                     |
| H5   | `docs/06-implementation/`（任务清单与提交记录）                            |
| H6   | `docs/07-release/`                                                         |

每个阶段都必须满足：

- 输入清晰
- 输出完整
- 假设明确
- 风险记录
- 评审通过
- 可支撑下一阶段

## 4. 阶段细则（H1–H6）

H1–H6 各阶段的输入、输出物、必填章节与评审门禁，详见独立文件 [`docs/stages.md`](docs/stages.md)。原 README §4–§9 的章节编号在该文件中保持不变（§4–§9），便于其他 Agent 与文档继续按 `§4.4`、`§6.5`、`§9.6` 等编号交叉引用。

| 阶段 | 主题                | 跳转                                                      |
| ---- | ------------------- | --------------------------------------------------------- |
| H1   | 需求、UI 与交互原型 | [stages.md §4](docs/stages.md#4-h1需求ui-与交互原型阶段)  |
| H2   | 技术架构选型        | [stages.md §5](docs/stages.md#5-h2技术架构选型阶段)       |
| H3   | 详细设计            | [stages.md §6](docs/stages.md#6-h3详细设计阶段)           |
| H4   | 测试用例设计        | [stages.md §7](docs/stages.md#7-h4测试用例设计阶段)       |
| H5   | AI 编码与自验证     | [stages.md §8](docs/stages.md#8-h5ai-编码与自验证阶段)    |
| H6   | 运行验证与文档回写  | [stages.md §9](docs/stages.md#9-h6运行验证与文档回写阶段) |

## 5. 目录规范与 Agent 套件

项目目录推荐结构、`AGENTS.md` 的使用约定、以及随规范附带的 8 个 Agent 套件说明，详见独立文件 [`docs/repo-layout.md`](docs/repo-layout.md)。原 README §10 的章节编号在该文件中保持不变（§10.1、§10.2）。

## 6. AI 使用规范

### 6.1 AI 输入必须明确

每次让 AI 工作时，必须提供：

- 当前阶段
- 目标产物
- 已通过评审的上游文档
- 不允许修改的范围
- 输出格式
- 验收标准
- 需要重点检查的风险

### 6.2 AI 输出必须可审计

AI 输出必须满足：

- 可审阅
- 可追溯
- 可执行
- 可测试
- 可回滚
- 不引入未确认需求
- 不隐藏假设

### 6.3 AI 禁止直接编码的情况

以下情况不得要求 AI 直接编码：

- 需求未评审
- UI 未评审
- 架构未评审
- 详细设计未完成
- 测试用例未定义
- 文件职责不清
- 接口契约不清
- 数据结构不清
- 验收标准不清

### 6.4 AI 使用反例

以下反模式取自 Anthropic 官方 Claude Code 最佳实践的常见失效模式，在本规范下同样适用。

- **杂烩会话（kitchen sink）**：在同一会话里串联多个不相关任务，上下文被无关内容填满。修正：不同任务开新会话，使用 `/clear` 或重启。
- **反复纠错（correction loop）**：同一问题超过两次仍未调对，说明上下文已被失败尝试污染。修正：重开会话，把学到的信息写进初始提示中。
- **过量规则文件（over-specified `AGENTS.md` / `CLAUDE.md`）**：规则文件越长，Agent 越会忽略重点。修正：定期修剪，能转成 Hooks / Lint 的规则就不要留在文档里。
- **先信后验缺口（trust-then-verify gap）**：AI 交付看似合理的实现，却未覆盖边界条件。修正：始终给出可运行的验收手段（测试、脚本、截图），无法验证的成果不予合并。
- **无界探索（infinite exploration）**：让 AI 调研"这个库怎么回事"而不设边界，它会通过读取大量无关文件把上下文耗尽。修正：限定调研范围，或派发子 Agent / Subagent 在隔离上下文中完成调研。

## 7. 评审规范

每个阶段评审必须保留记录，建议使用 `templates/review-record.md`。

评审记录应包括：

- 评审时间
- 参与人员
- 评审对象
- 通过项
- 修改项
- 风险项
- 结论
- 下一步动作

评审结论分为：

- `Approved`：通过，可进入下一阶段
- `Approved with Changes`：小修改后可进入下一阶段
- `Rejected`：不通过，必须返工
- `Pending`：信息不足，暂缓决策

## 8. 追溯关系

本规范要求所有交付物之间建立完整的追溯关系：

```text
需求 -> UI/原型 -> 架构 -> 详细设计 -> 测试用例 -> 代码/配置 -> 提交 -> 测试报告 -> 部署/运维文档
```

这条链最终沉淀为 `docs/07-release/traceability-matrix.md`，由 H5 阶段维护的 `commit-records.md` 汇总而成。

每个代码文件应能回答：

- 它来自哪个需求？
- 它实现哪个设计项？
- 它对应哪些测试用例？
- 它由哪个提交引入？
- 它是否已在最终文档中体现？

## 9. 质量标准

一个阶段只有在满足以下条件时，才视为完成：

- 输出物完整
- 关键假设明确
- 风险已记录
- 评审已通过
- 修改意见已处理
- 与上游文档一致
- 可支撑下一阶段工作

项目级完成标准见 [`docs/stages.md`](docs/stages.md) §9.6。

## 10. 熵与技术债务 GC

> 该节来自 OpenAI Codex 团队在"Harness engineering"文章中提出的实践经验。在 H6 交付后，AI 代码仓库会随时间产生"状态熵"，需要持续清理。

### 10.1 黄金原则（Golden Principles）

团队应提炼出一组可机械化检查的"黄金原则"，描述项目期望代码库保持的形状：

- 优先使用共享工具包，避免手写重复逻辑
- 在边界始终使用类型验证（parse, don't validate），不凭猜测推送数据形状
- 结构化日志、命名约定、文件大小上限等"品味不变式（taste invariants）"需以 Lint 硬拦截
- 跨层依赖只能沿架构图预设方向，逾越者报错

这些原则需写进 `docs/` 下的权威文档（如 `quality-grade.md`）并同步编码为可执行检查。

### 10.2 定期 GC 任务

建议在仓库中配置定期运行的后台 AI 任务，完成以下事项：

- 扫描代码库与黄金原则的偏离，开启重构 PR
- 扫描 `docs/` 下与代码实际行为不一致的过期文档（doc-gardening），开启修复 PR
- 更新 `docs/06-implementation/exec-plans/tech-debt-tracker.md` 中的未完成项
- 合并选项：质量评级 / quality grade 表可在项目初期仅补充到 `docs/04-detailed-design/` 或 `docs/07-release/` 中

### 10.3 使用原则

- **持续偿还 > 集中重构**：技术债务像高利息贷款，每日少量偿还远优于积压后被迫集中返工。
- **人的品味一次捕获，机器永久执行**：评审心得、重构经验、线上故障复盘，要么转化为 `AGENTS.md` / Skill 里的指导，要么转化为 Lint / Hooks / CI 检查。
- **允许小幅 PR 自动合并**：GC 产出的 PR 如果可以在一分钟内评审完毕，应设置成可自动合并。

## 11. 附录：阶段门禁摘要

| 阶段 | 核心产物                     | 通过标准                                       |
| ---- | ---------------------------- | ---------------------------------------------- |
| H1   | 需求、UI、原型               | 需求清晰，UI 可评审，验收标准明确              |
| H2   | 架构说明、技术选型           | 技术路线可落地，风险有缓解方案                 |
| H3   | 详细设计                     | 数据、接口、文件、配置、日志、部署和监控均明确 |
| H4   | 测试计划、测试用例           | 每个关键文件和核心需求都有测试覆盖             |
| H5   | 代码、测试、提交、执行计划   | 小步实现，测试通过，提交可追溯                 |
| H6   | 最终文档、测试报告、运维资料 | 实现与文档一致，系统可运行可维护               |

## 12. 参考资料

本规范在设计中参考了以下公开资料：

- OpenAI · [*Harness engineering: leveraging Codex in an agent-first world*](https://openai.com/index/harness-engineering/)（Ryan Lopopolo，2026-02-11）
- OpenAI Cookbook · [*Codex Execution Plans*](https://cookbook.openai.com/articles/codex_exec_plans)
- Anthropic · [*Best Practices for Claude Code*](https://code.claude.com/docs/en/best-practices)
- Anthropic · [*Claude Code Memory（CLAUDE.md）*](https://code.claude.com/docs/en/memory)
- Anthropic · [*Claude Code Skills*](https://code.claude.com/docs/en/skills)
- [AGENTS.md 跨工具开放约定](https://agents.md/)（2025-08）
- LangChain Blog · [*The Anatomy of an Agent Harness*](https://blog.langchain.com/the-anatomy-of-an-agent-harness/)
- Mitchell Hashimoto · [*My AI Adoption Journey*](https://mitchellh.com/writing/my-ai-adoption-journey)
- Red Hat Developers · [*Harness engineering: Structured workflows for AI-assisted development*](https://developers.redhat.com/articles/2026/04/07/harness-engineering-structured-workflows-ai-assisted-development)（Marco Rizzi，2026-04-07）
- Augment Code · [*Harness Engineering for AI Coding Agents: Constraints That Ship Reliable Code*](https://www.augmentcode.com/guides/harness-engineering-ai-coding-agents)（2026-04-16）

## 协议与引用

本规范以**开放共享**为原则，欢迎任何团队、个人在自有项目中借鉴、改写或派生。

- **协议**：建议按 [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/deed.zh) 共享文档内容，按 [MIT](https://opensource.org/licenses/MIT) 共享 `agents/` 与 `templates/` 中的提示词与模板代码。仓库根目录的 `LICENSE` 文件以最终版本为准。
- **引用**：如在博客、白皮书或公开演讲中引用本规范，建议注明：
  > Harness Engineering 规范（驾驭工程），v0.1，<https://github.com/shuaihuadu/harness-engineering>
- **派生**：派生版本请保留对原仓库的引用，并在派生说明中标注差异点。

## 贡献

本规范处于试行阶段，欢迎以下形式的贡献：

- 在真实项目中落地后，反馈条款的可执行性与缺口（Issue）
- 提交针对具体 Agent 提示词、模板、术语表的修订（Pull Request）
- 补充新的工具厂商对照（OpenAI / Anthropic 之外的 Cursor、Factory、Augment 等）

提交前请阅读 [`templates/phase-gate-checklist.md`](templates/phase-gate-checklist.md)，确保改动本身也满足本规范的最小自一致要求（变更说明、影响范围、是否需要同步更新 Agent 与术语表）。
