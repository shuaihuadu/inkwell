# 输入输出契约

本文件定义所有 Agent 共用的 I/O 契约。每个 `AGENT.md` 引用本文件而非重复定义。

## 1. 文件路径与编码

- 所有产物文件使用 **UTF-8 NoBOM** 编码。
- 行尾统一为 `LF`（项目级 `.editorconfig` 兜底）。
- 路径一律使用相对仓库根的正斜杠形式，如 `docs/01-requirements/requirements.md`。
- Agent 工具能力名一律取自 [`tool-vocabulary.md`](./tool-vocabulary.md)。

## 2. Markdown frontmatter 约定

需要被 Agent / 工具自动消费的 Markdown 文档应在文件首部加 YAML frontmatter：

```yaml
---
id: REQ-001                    # 编号，见 glossary.md §4
stage: H1                      # 所属阶段
status: draft                  # draft / reviewed / approved / deprecated
authors:
  - name: <人或 Agent 名>
    role: human | agent
reviewers:                     # 进入 reviewed 状态后填写
  - <reviewer>
created: 2026-04-27
updated: 2026-04-27
upstream:                      # 上游依赖产物
  - REQ-000
downstream: []                 # 下游消费产物（由后续阶段回填）
---
```

未列出的字段允许扩展，但不得改变上述字段的语义。

## 3. 编码任务说明（H5 输入）

H5 任务说明在 `templates/ai-task-brief.md` 基础上必须填齐以下字段：

- `当前阶段`：固定 `H5`
- `任务编号`：`TASK-YYYY-MM-DD-NNN`
- `允许修改的文件`：完整路径列表
- `禁止修改的文件`：完整路径列表
- `上游文档`：需求 / UI / 架构 / 详细设计 / 测试用例的文件路径或编号
- `设计引用`：`HD-xxx`、`API-xxx`、`DB-xxx`
- `测试引用`：`TC-xxx`
- `验收命令`：可执行的测试 / 构建命令

## 4. 提交信息约定

```text
<type>(<scope>): <summary>

Design: HD-xxx
Tests: TC-xxx, TC-yyy
Verify: <可复现的命令行>
Docs: updated | not needed
Risk: none | <简述>
Task: TASK-YYYY-MM-DD-NNN
```

`<type>` 取值：`feat` / `fix` / `refactor` / `docs` / `test` / `chore` / `perf` / `build` / `ci`。

`Design` / `Tests` / `Task` 三字段是追溯链的核心，缺失任意一项视为提交不合规，CommitAuditor 会拒绝。

## 5. Agent 错误返回结构

当 Agent 因输入不满足前置条件而无法继续时，**必须**返回结构化错误，而非凭空补全。建议格式：

```yaml
status: blocked
reason: <简短原因，如 "REQ-001 未通过评审">
missing_inputs:
  - path: docs/01-requirements/requirements.md
    expected_status: approved
    actual_status: draft
suggested_next_action:
  - 完成 H1 评审并将 frontmatter status 改为 approved
  - 或在任务说明中显式声明降级处理理由
```

## 6. 上下文卫生约定

- **禁止**在单次会话中跨阶段操作（如同一会话先做 H1 又做 H5）。
- **禁止**在不引用上游产物路径的情况下进行实现。
- **建议**：探索性查阅交给 subagent / 隔离会话，避免污染主会话上下文。

## 7. 不做范围

以下行为不属于任何 Agent 的职责，不得在 `AGENT.md` 里被赋予：

- 直接合并 PR（必须由人工或独立 CI 系统完成）
- 直接发布制品（H6 之后的发布动作不在本规范的 Agent 体系内）
- 修改本目录下的规范文件（`harness-engineering/README.md` 与本目录下任何 `AGENT.md` / `prompt.md`）
