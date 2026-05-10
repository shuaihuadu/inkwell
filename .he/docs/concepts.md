# 概念与核心原则

本文件展开 [`README.md`](../README.md) 没有展开的两件事：

1. **§1**：Harness Engineering 的来源、业界共识定义、三层模型（约束层 / 反馈层 / 质量门禁层），以及主流厂商的对照实现。
2. **§2**：本规范在 SDLC 维度落地三层模型时遵循的六条核心原则。

阅读建议：

- 没听过 *Harness Engineering* 这个术语 → 通读 §1。
- 已熟悉术语，想了解本规范的取舍口径 → 直接跳 §2。
- 想看阶段细则（H1–H6） → 去 [`docs/stages/`](stages/README.md)。

## 1. 三层 Harness：来源、定义、落地对照

### 1.1 概念背景

Harness Engineering（中文社区暂无官方译名，本规范采用"驾驭工程"，与 OpenAI 标语 "Humans steer" 同源）这一术语在 2026 年 2 月由 Mitchell Hashimoto 在其个人博客中首次提出，并由 OpenAI（Ryan Lopopolo）在 2026 年 2 月 11 日发布的文章中给出正式定义。LangChain 把它精炼为一个公式：**Agent = Model + Harness**，OpenAI 给出的标语是 **"Humans steer. Agents execute."（人类掌舵，Agent 执行）**。

业界对 Harness Engineering 的共识定义是：

> 设计环境、约束和反馈回路，让 AI 编码 Agent 在规模化场景下保持可靠的工程学科。

本规范在此基础上按 Agent **行动时序**展开为三层（约束层 / 反馈层取自社区共识，质量门禁层引自 DevOps 词汇，三者按"行动前 / 行动中 / 行动后"分工，避免按机制划分时的归属重叠）：

- **约束层（Constraint Harness）—— 行动前**：规则文件、Lint、类型系统、AGENTS.md / copilot-instructions.md 等前馈控制，缩小 Agent 的解空间。
- **反馈层（Feedback Loop）—— 行动中**：把测试、Lint、构建错误等结构化信号回灌给 Agent，让其自我修复。
- **质量门禁层（Quality Gate）—— 行动后**：在 CI、评审、合并环节进行硬性拦截，阻止不合规产物进入主干。

### 1.2 主流工具厂商落地对照

| 层次       | GitHub Copilot 体系                                                                                                                                        | OpenAI Codex 体系                                                                                      | Anthropic Claude Code 体系                                    |
| ---------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------- |
| 约束层     | `.github/copilot-instructions.md`、`.github/instructions/*.instructions.md`（带 `applyTo` 模式）、`AGENTS.md`、`.github/agents/*.agent.md`（Custom Agent） | `AGENTS.md`（多级，作为"目录"而非百科全书）、自定义 Lint、结构测试、分层架构不变式                     | `CLAUDE.md`（及子目录继承）、Skills、Subagents                |
| 反馈层     | Agent Mode（`#codebase` / `#fetch` / 终端 / Problems 回灌）、MCP 工具集成、Copilot Edits 多文件迭代                                                        | 测试 / 构建 / Chrome DevTools MCP / 本地可观测栈（LogQL、PromQL）、Ralph Wiggum 循环（Agent 自审自改） | Plan Mode、测试与截图验证、Subagents 隔离调查                 |
| 质量门禁层 | Copilot code review（PR 阶段）、Branch protection + Required reviewers、Secret scanning push protection、GitHub Actions CI                                 | 自定义 Lint 硬失败、结构测试、后台 doc-gardening Agent、提交者路径限制                                 | Hooks（确定性拦截）、Permission allowlist、Sandbox、Auto Mode |

本规范作为上述体系中 **"文档化 + SDLC"** 这一层的通用骨架，与底层工具不冲突。

### 1.3 本规范的定位

本规范是上述思想在 **"团队 SDLC + AI 协作"** 场景下的一份具体落地方案，重点放在**约束层和质量门禁层的人工与文档化部分**：用文档、评审、测试和提交记录构成一条 Agent 在工作时必须遵守的硬轨道。它**不试图覆盖**所有运行时层面的 Harness（如 Lint 钩子、CI 拦截、运行时工具调度），这些应由具体项目的 CI/CD 流水线、`AGENTS.md` / `copilot-instructions.md` 等配套机制实现。

一句话定位：

> 本规范是 Harness Engineering 在 SDLC 维度的人工版骨架——用文档、评审、测试、提交和运行回写，把 AI 的创造力约束成可交付、可追溯、可维护的软件工程能力。

## 2. 六条核心原则

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
