# Harness Engineering 配套 Agent

本目录提供一组与 [Harness Engineering 规范](../README.md) 配套的 Agent 规格。它们是规范的可执行延伸：让 H1–H6 流程不再只依赖文档约定，而是可以由具体工具（Claude Code、GitHub Copilot、OpenAI Codex、自研 Agent Runtime 等）真实跑起来。

> 本目录下的所有 Agent 均与具体业务无关。任何采用本规范的项目都可以直接复用，必要时再派生项目专属变体。

## 1. 设计原则

- **业务无关**：Agent 只读规范定义的产物（`docs/01-requirements/` ... `docs/07-release/`、`AGENTS.md`、`templates/`），不假设任何业务领域。
- **模型中立**：所有 prompt 不依赖某个模型的特殊能力（thinking 标签、专有工具格式），任何具备工具调用能力的 LLM 都可装载。
- **工具中立**：以纯 Markdown 描述 Agent 规格，需要落到具体工具时再做轻量适配（详见 §5）。
- **小步交付**：先打通 H1 / H3 / H4 / H5 / H6 与横切共 8 个核心岗位，验证后再扩展 H2 等其余阶段。

## 2. Agent 总索引

| Agent | 阶段 | Harness 层 | 一句话职责 |
| --- | --- | --- | --- |
| [RequirementsInterviewer](./requirements-interviewer/AGENT.md) | H1 | 反馈层 | 接收一句话需求，主动反问以暴露模糊点，产出可评审的 `requirements.md` 草稿 |
| [RepoImpactMapper](./repo-impact-mapper/AGENT.md) | H1↔H3 之间 | 约束层 | 在做计划前扫描真实代码，产出可审核的"仓库影响地图"，拦截"AI 凭空编 API"的失败模式 |
| [DesignReviewer](./design-reviewer/AGENT.md) | H3 | 门禁层 | 机械化校验详细设计的完备性与一致性，挡住"设计没写清"流入 H4/H5 |
| [TestCaseAuthor](./test-case-author/AGENT.md) | H4 | 反馈层 | 从需求与设计反推 `TC-NNN`，确保每条 REQ 至少有可机械判断的覆盖 |
| [CodingExecutor](./coding-executor/AGENT.md) | H5 | 反馈层 | 严格按 `ai-task-brief.md` 完成单个工程单元，同步生成测试与提交元数据 |
| [CommitAuditor](./commit-auditor/AGENT.md) | H5 | 门禁层 | 在 PR / 合并前机械化校验提交信息、改动范围、追溯字段 |
| [ReleaseNoteWriter](./release-note-writer/AGENT.md) | H6 | 反馈层 | 从 commit-records 与追溯链生成 release notes 草稿，回写追溯矩阵 |
| [DocGardener](./doc-gardener/AGENT.md) | 横切 | 门禁层 | 定时巡检 `docs/` 与代码实际行为的偏离，开具修复 PR |

后续候选（暂未交付，避免在缺乏真实样本时过早设计）：

- ArchitectAdvisor（H2）
- PrototypeReviewer（H1 UI / 原型评审辅助）
- IncidentResponder（H6 之后的故障复盘辅助）

## 3. 协作拓扑

```text
H1: RequirementsInterviewer ──► RepoImpactMapper
                                       │
H2: 人工 / A ──► DesignReviewer
                          │
H4: TestCaseAuthor ◄──────┘
        │
H5: CodingExecutor ──► CommitAuditor ──► （CI 钩子 / 项目专属 Linter）
                                                  │
H6: ReleaseNoteWriter ◄───────────────────────────┘er）
                                                  │
H6: 人工 / TraceabilityMatrixBuilder / ReleaseNotesWriter（待补）

横切（定时 / Webhook 触发）：DocGardener
```

## 4. 共享契约

所有 Agent 共用以下两份契约文件，避免每个 `AGENT.md` 重复定义：

- [`_shared/glossary.md`](./_shared/glossary.md)：阶段编号、产物路径、追溯字段等术语统一定义。
- [`_shared/io-contracts.md`](./_shared/io-contracts.md)：输入输出文件命名、frontmatter 字段、提交信息格式、错误返回结构。
- [`_shared/tool-vocabulary.md`](./_shared/tool-vocabulary.md)：Agent 工具能力共享词表，由各 `AGENT.md` §工具集 引用。
- [`_shared/AGENT.md.template`](./_shared/AGENT.md.template) / [`_shared/prompt.md.template`](./_shared/prompt.md.template)：新增 Agent 时使用的干净骨架。

## 5. 接入具体工具

每个 Agent 只交付两份纯 Markdown 文件：

- `AGENT.md`：Agent 规格（定位、触发、输入输出、行为约束、验收标准）。
- `prompt.md`：模型中立的中文系统提示。

落到具体工具时只需做一层轻量包装：

| 工具 | 包装方式 |
| --- | --- |
| Claude Code | 在 `.claude/agents/<name>.md` 加 frontmatter（`name`、`description`、`tools`、`model`），正文 `@` 引用 `prompt.md` |
| GitHub Copilot Chat | 在 `.github/chatmodes/<name>.chatmode.md` 配置工具集，正文引用 `prompt.md` |
| OpenAI Codex / AGENTS.md 体系 | 在 `AGENTS.md` 子目录指向对应 `AGENT.md`，由 Runtime 注入 `prompt.md` |
| 自研 Agent Runtime | 直接读取 `AGENT.md` 的输入输出契约 + `prompt.md` 作为 system prompt |

> **不要**把工具特有的 frontmatter 写进 `AGENT.md` / `prompt.md` 自身。包装文件可以放在使用方仓库（如 `.claude/`、`.github/`），保持本目录的工具中立。

可直接复用的模板见 [`_integrations/`](./_integrations/README.md)，覆盖 Claude Code、GitHub Copilot Chat、OpenAI Codex、自研 Runtime 四类落地方式。

## 6. 版本与演进

- 当前版本：v0.1（与规范 v0.1 同步）
- 状态：试行

### 6.1 修改门槛

`prompt.md` / `AGENT.md` 的修改属于规范级变更，按以下门槛执行：

- **轻微修订**（错别字、格式、链接、不改行为）：直接 PR，1 名维护者评审即可
- **行为微调**（措辞改变 Agent 行为但不改契约）：必须附 1 个真实项目的反例，并在 PR 描述中给出修改前后 Agent 的输出对比
- **契约变更**（修改 `AGENT.md` 输入输出、工具集、阻塞返回条件）：必须先在本目录 §7 登记修订建议，由维护者批量回写

### 6.2 反例采集

每个 Agent 在落地后保留以下输入用于演进：

- 触发阻塞返回的真实输入（脱敏后存入项目内部知识库）
- 产出与规范要求偏离的真实案例
- 评审会中被人工驳回的产物

数量门槛：单个 Agent 累计 ≥ 3 个反例后，才允许提出"行为微调"级别的 PR。

### 6.3 何时新增 Agent

提出新 Agent 前必须先回答：

1. 该职责是否能用现有 Agent + 不同输入完成？若是，**不要**新增
2. 该职责是否真的需要独立系统提示？还是仅在 `AGENT.md` 加一节"工作流变体"即可？
3. 是否已有至少一个真实项目跑出了"缺这个 Agent"的具体卡点？没有就是空想，押后

通过以上三问后，再按 [`_shared/AGENT.md.template`](./_shared/AGENT.md.template) 与 [`_shared/prompt.md.template`](./_shared/prompt.md.template) 起草。

### 6.4 退役

允许把 Agent 标记为 `deprecated`：

- `AGENT.md` 顶部加 `> **状态**：deprecated（自 vX.Y 起）`
- 在本目录 §2 索引表中标灰（不删除条目）
- 给出迁移建议（指向继任 Agent 或人工流程）

退役至少保留两个版本周期再考虑物理删除。

## 7. 对规范的修订建议（占位）

落地 Agent 过程中如果发现规范本身需要调整，集中记录到本节，由维护者批量回写到 [`../README.md`](../README.md)。每条建议格式：

```markdown
- **触发 Agent**：<哪个 Agent 在落地中发现>
- **规范章节**：<README.md 的 §X.Y>
- **问题**：<具体描述>
- **建议**：<修改方向>
- **证据**：<反例 / 链接>
```

当前为空。
