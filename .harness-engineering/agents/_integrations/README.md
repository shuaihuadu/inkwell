# 工具包装模板

本目录提供把 [`agents/`](../README.md) 下中立 Agent 接入具体 IDE / Runtime 的**模板**。模板本身不是规范的一部分——使用方按需复制到自己的仓库 / 工作区，再做项目化调整。

> 设计原则：保持 `agents/<name>/AGENT.md` 与 `prompt.md` 工具中立。任何工具特有的 frontmatter、文件位置、权限配置都放在本目录的模板里，而不是污染 Agent 自身。

## 1. 模板清单

| 工具                     | 模板                                                                             | 落地位置（使用方仓库）                                                             |
| ------------------------ | -------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| Claude Code              | [`claude-code/agent.md.template`](./claude-code/agent.md.template)               | `.claude/agents/<name>.md`                                                         |
| GitHub Copilot           | [`copilot/`](./copilot/README.md)（指令 + chatmode 套件）                        | `.github/copilot-instructions.md` / `.github/instructions/` / `.github/chatmodes/` |
| OpenAI Codex / AGENTS.md | [`codex/agents.md.snippet`](./codex/agents.md.snippet)                           | 在仓库 `AGENTS.md` 末尾追加                                                        |
| 自研 Runtime             | [`generic/runtime-config.yaml.template`](./generic/runtime-config.yaml.template) | 由 Runtime 项目自管理                                                              |

> Copilot 一栏指向子目录而非单个文件——它包含一份顶层指令（`copilot-instructions.template.md`）、3 份切片指令（`instructions/`）、3 份专用 chatmode（`chatmodes/`），以及一份通用 chatmode 模板 `chatmode.md.template`（可派生其他 5 个 Agent）。详细文件清单与复制步骤见 [copilot/README.md](./copilot/README.md)。

## 2. 通用替换占位符

所有模板中以下占位符在落地时替换：

| 占位符           | 含义                                                                                                                 |
| ---------------- | -------------------------------------------------------------------------------------------------------------------- |
| `{{AGENT_NAME}}` | Agent 名（如 `CodingExecutor`）                                                                                      |
| `{{AGENT_DIR}}`  | 相对仓库根的路径（如 `harness-engineering/agents/coding-executor`）                                                  |
| `{{ONE_LINER}}`  | 一句话职责（来自 `AGENT.md` §定位 第一句）                                                                           |
| `{{TOOL_LIST}}`  | 工具白名单。值取自 [`_shared/tool-vocabulary.md`](../_shared/tool-vocabulary.md)，再按目标工具的真实接口名做一次映射 |

## 3. 使用约定

- **不要**在模板里复制 `AGENT.md` / `prompt.md` 的正文。模板用相对路径 `@` 引用 / `include` 原文，避免双份维护。
- **不要**在模板里加项目业务约束。业务约束属于使用方仓库的 `AGENTS.md`，由工具自动按路径层级拼接。
- 模板需要随 `AGENT.md` 的输入输出契约同步更新。新增 / 调整 Agent 时，先改 `AGENT.md`，再回头检查这些模板。

## 4. 不在范围内

- 各 IDE 的扩展安装、登录配置：使用方自行处理
- CI / Webhook 编排：与 Agent 包装无关，由项目流水线管理
- 模型选择 / 计费：由使用方自管
