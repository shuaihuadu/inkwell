---
title: 目录规范与配套 Agent 套件
parent: ../README.md
---

# 目录规范

本文件是 Harness Engineering 规范的目录与套件章节，从 [`../README.md`](../README.md) 抽出。原 §10 的章节编号在本文件中**保持不变**，便于其他文档继续按 `§10.1`、`§10.2` 引用。

## 10. 目录规范

推荐项目目录：

```text
AGENTS.md                  # 顶层：Agent 规则的"目录"，推荐 100 行以内
CLAUDE.md                  # 可选：针对 Claude Code 的别名/补充，可以仅 import AGENTS.md
.github/copilot-instructions.md  # 可选：针对 GitHub Copilot 的别名/补充

docs/
  01-requirements/         # H1
    requirements.md
    user-flow.md
    ui-spec.md
    acceptance-criteria.md
  02-prototype/            # H1
    prototype-review.md
  03-architecture/         # H2
    architecture.md
    tech-selection.md
    risk-analysis.md
    adr/
  04-detailed-design/      # H3
    detailed-design.md
    database-design.md
    api-design.md
    process-design.md
    file-structure.md
    config-design.md
    log-design.md
    monitoring-design.md
    deployment-design.md
    performance-boundary.md
  05-test-design/          # H4
    test-plan.md
    test-matrix.md
    test-cases/
  06-implementation/       # H5
    coding-tasks.md
    commit-records.md
    exec-plans/
      active/
      completed/
      tech-debt-tracker.md
  07-release/              # H6
    software-manual.md
    requirements-final.md
    design-final.md
    ops-manual.md
    deployment-guide.md
    test-report.md
    release-notes.md
    known-issues.md
    traceability-matrix.md

prototypes/                # H1，与 docs/ 平行，存放可交互 UI 原型源码
  <feature-name>/

templates/
  ai-task-brief.md
  phase-gate-checklist.md
  review-record.md

agents/                    # 配套 Agent 套件（与业务无关，详见 §10.2）
  README.md
  _shared/                 # 共享术语与 I/O 契约
  _integrations/           # 落到具体工具的轻量包装模板
  <agent-name>/
    AGENT.md
    prompt.md
```

### 10.1 AGENTS.md 的使用约定

`AGENTS.md` 是 2025-08 由 OpenAI、Cursor、Factory 等联合提出的跨工具开放约定，已被主流 AI 编码工具广泛识别。本规范采纳其作为 Agent 规则的唯一权威文件，其他工具专用文件（`CLAUDE.md`、`.github/copilot-instructions.md` 等）可以用 import / 软链的方式指向它，避免多处维护。

根据 OpenAI 的实践经验，`AGENTS.md` **必须是一份"目录"，而不是百科全书**：

- 顶层 `AGENTS.md` 控制在 100 行以内，只写项目身份、关键约束、常用命令、文档入口
- 领域知识、详细架构、测试策略由 `AGENTS.md` **指向** `docs/` 下的权威文档，不重复书写
- 子目录（如 `src/core/AGENTS.md`、`src/app/webapp/AGENTS.md`）可增量补充**仅在子范围适用**的约束，由工具自动按路径层级拼接
- 如果一条规则可以用 Lint / Hooks / CI 强制执行，就不要只在 `AGENTS.md` 里说它——文档是 advisory，工具才是 deterministic

判断一条规则到底该不该写进 `AGENTS.md` 的常用问法：**"如果删了这一行，Agent 还会照做吗？"** 如果是，删。

### 10.2 配套 Agent 套件

本规范附带一组与业务无关、模型与工具中立的 Agent 规格，落在 [`../agents/`](../agents/README.md) 目录。这些 Agent 是规范的**配套基础设施**，并非项目业务实现：

- `agents/_shared/`：共享的术语表与 I/O 契约（frontmatter、提交格式、错误结构），所有 Agent 文件通过相对链接引用，避免漂移
- `agents/<agent-name>/AGENT.md`：定位、触发时机、输入/输出契约、工具集、行为约束、验收标准、协作关系、已知边界
- `agents/<agent-name>/prompt.md`：可直接喂给任何能跑系统提示的 LLM 的纯文本工作流

首批覆盖三层 Harness 的关键岗位：

| Agent                   | 阶段   | Harness 层          |
| ----------------------- | ------ | ------------------- |
| RequirementsInterviewer | H1     | 反馈层              |
| RepoImpactMapper        | H1↔H3  | 约束层              |
| DesignReviewer          | H3     | 质量门禁层          |
| TestCaseAuthor          | H4     | 反馈层              |
| CodingExecutor          | H5     | 反馈层              |
| CommitAuditor           | H5/H6  | 质量门禁层          |
| ReleaseNoteWriter       | H6     | 反馈层              |
| DocGardener             | 跨阶段 | 质量门禁层 + 反馈层 |

采用方可以选择性接入，无需一次性引入全部 Agent。落到具体工具时使用 [`../agents/_integrations/`](../agents/_integrations/README.md) 提供的模板（覆盖 Claude Code、GitHub Copilot Chat、OpenAI Codex、自研 Runtime 四类），保持 `AGENT.md` / `prompt.md` 自身工具中立。
