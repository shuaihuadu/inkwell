---
name: traceability-linker
description: 校验并补全 Harness Engineering 追溯链（REQ ↔ HD/API/DB ↔ TC ↔ TASK ↔ Commit）。当用户给出任意一条需求编号、设计编号、测试用例编号、任务编号或提交信息，需要查清它的上下游关联是否完整、有没有断链、缺哪条凭证时使用。设计评审、测试设计、提交审计、release notes 起草、影响分析等场景都会反复用到这条 SOP，凡是涉及"这条改动到底对应哪个需求/设计/测试"的提问都应该主动调用本 Skill，而不是凭印象回答。
when_to_use: |
  - 用户给出 REQ-/HD-/API-/DB-/TC-/TASK- 编号或一段提交信息，问"这条对应到哪些上下游"
  - 设计评审、测试评审、提交评审前需要确认追溯字段齐全
  - Release notes 起草需要从 commit 反查需求
  - 影响分析需要从需求正向展开到代码改动
when_not_to_use: |
  - 用户在询问编号本身的命名规则（那属于 glossary，不是追溯）
  - 用户只是想生成一条新提交信息（用 commit-message-formatter）
  - 用户在做需求/设计的内容评审（用对应 Agent，不是本 Skill）
---

# Skill: 追溯链补全器（Traceability Linker）

## 1. 目的与原理

Harness Engineering 的核心约束是：**任何一行落到主分支的代码，都能反查到一条已评审的需求**。这条链路由若干编号串起来：

```text
REQ-NNN ──► HD-NNN / API-NNN / DB-NNN ──► TC-NNN ──► TASK-YYYY-MM-DD-NNN ──► Commit (Design/Tests/Task 字段)
```

链路上任何一环断掉，都会让"评审 → 实现 → 验证 → 发布"的闭环失效。本 Skill 的职责是：**给定链路上任意一个节点，机械地查出其余节点，并在出现断链时给出可执行的修复指令，而不是凭印象补全**。

> 为什么不直接交给某个 Agent？因为追溯检查会在 H3 评审、H4 测试设计、H5 提交审计、H6 release notes 多个阶段反复发生。把它沉淀为 Skill，避免在每个 Agent 的 prompt 里重复同一段流程，也避免各 Agent 实现得不一致。

## 2. 输入

接受以下任一形式的输入：

- **编号**：单个或多个 `REQ-001` / `HD-003` / `API-007` / `DB-002` / `TC-014` / `TASK-2026-04-30-001`
- **提交信息**：完整或片段的 commit message，关心其中的 `Design:` / `Tests:` / `Task:` 字段
- **文件路径**：`docs/01-requirements/REQ-001.md` 之类，需从 frontmatter 抽 id

如输入歧义（例如只给"那个权限相关的需求"而无编号），先反问而不是猜。

## 3. 步骤（按顺序执行）

### 3.1 锚定起点

确认输入对应到哪一个节点（REQ / HD / API / DB / TC / TASK / Commit）。如果是文件，读取 frontmatter 的 `id` 与 `upstream` / `downstream`（见 `agents/_shared/io-contracts.md` 第 2 节）。

### 3.2 沿链双向展开

**向上回溯**（找出处）：

| 当前节点 | 上游来源                                                          |
| -------- | ----------------------------------------------------------------- |
| Commit   | `Task:` 字段                                                      |
| TASK     | 任务卡内的 `上游文档` / `设计引用` / `测试引用`                   |
| TC       | TC 文档 frontmatter 的 `upstream` 或正文中"覆盖需求"列出的 REQ/HD |
| HD/API/DB | HD 文档 frontmatter 的 `upstream`，最终指向某条 REQ              |
| REQ      | 已是源头，无需上溯                                                |

**向下展开**（找去处）：使用文件搜索工具（`grep_search` / `file_search`）在 `docs/` 与提交记录中查找当前编号被引用的位置。匹配范围：

- frontmatter 的 `upstream:` 列表
- 正文中以 `REQ-` / `HD-` / `API-` / `DB-` / `TC-` / `TASK-` 开头的字符串
- Git log 的提交信息

### 3.3 输出追溯矩阵

按下面的表格输出。**实际查到几行就写几行，未查到的格子写 `<缺失>`，不要伪造**：

```markdown
## 追溯矩阵

| REQ      | HD/API/DB | TC      | TASK                  | Commits                  |
| -------- | --------- | ------- | --------------------- | ------------------------ |
| REQ-001  | HD-003    | TC-014  | TASK-2026-04-30-001   | abc1234, def5678         |
| REQ-001  | <缺失>    | TC-015  | <缺失>                | -                        |
```

### 3.4 列出断链

对每一条 `<缺失>`，给出**断在哪、按规范应当由哪个阶段补、可执行的下一步**。例如：

```markdown
## 断链清单

1. **REQ-001 → HD 缺失**：H3 详细设计未覆盖该需求。
   - 阻塞：H4 测试用例无法回溯到设计。
   - 下一步：补 `docs/04-detailed-design/HD-XXX.md`，frontmatter `upstream: [REQ-001]`，或在 REQ 上标注"无需详细设计"并说明理由。

2. **TC-015 → TASK 缺失**：测试用例未被任何 H5 任务消费。
   - 阻塞：测试可能未实际执行。
   - 下一步：在最近覆盖这条测试的 TASK 卡 `测试引用` 字段补上 `TC-015`，或新建一条 TASK。
```

### 3.5 给出验证命令（如适用）

如果链路涉及代码改动，列出可让用户复现验证的命令（来自 TASK 卡的 `验收命令` 或提交信息的 `Verify:` 字段）。

## 4. 失败模式与回退

- **多个 REQ 同名编号**：项目历史上撞号了。立即停下，要求用户先在 `docs/01-requirements/` 里去重，不要自己挑一个用。
- **commit 没有 `Task:` 字段**：标记为"违反 io-contracts.md 第 4 节的提交规范"，建议补 `--amend` 或新提交一条 fixup（前提是未推送），并提示用户走 `commit-auditor` 流程。
- **frontmatter 缺失或不合法**：不要"宽容解析"——直接报告该文档不符合 io-contracts.md 第 2 节，请先修复 frontmatter 再重试本 Skill。
- **跨仓库追溯**：本 Skill 默认只看当前仓库。涉及外部依赖时只在断链清单里标记"需跨仓库验证"，不要尝试自动跳出仓库。

## 5. 示例

**示例 1：从一条 commit 反查**

输入：

```text
feat(core): introduce buffered aggregator

Design: HD-003
Tests: TC-014, TC-015
Verify: <test-runner>
Docs: updated
Risk: none
Task: TASK-2026-04-30-001
```

输出（节选）：

```markdown
## 追溯矩阵
| REQ     | HD     | TC      | TASK                | Commits |
| ------- | ------ | ------- | ------------------- | ------- |
| REQ-007 | HD-003 | TC-014  | TASK-2026-04-30-001 | <本次>  |
| REQ-007 | HD-003 | TC-015  | TASK-2026-04-30-001 | <本次>  |

## 断链清单
（无）
```

**示例 2：从一条孤立 TC 出发**

输入：`TC-022`

执行 3.2 节上溯发现 frontmatter 没有 `upstream`，正文也未写覆盖需求。

输出：

```markdown
## 追溯矩阵
| REQ    | HD/API/DB | TC    | TASK    | Commits |
| ------ | --------- | ----- | ------- | ------- |
| <缺失> | <缺失>    | TC-022 | <缺失> | -       |

## 断链清单
1. **TC-022 → REQ 缺失**：测试用例未声明覆盖的需求。
   - 阻塞：H4 评审无法判定该用例的存在价值。
   - 下一步：在 `docs/05-test-cases/TC-022.md` 的 frontmatter 加 `upstream: [REQ-XXX]`，或删除该用例。
```

## 6. 与其它 Skill / Agent 的边界

- 想**生成**新的提交信息：用 `commit-message-formatter`。
- 想**评审**详细设计内容：用 `design-reviewer` Agent，本 Skill 不评内容质量。
- 想**核对阶段产物完整性**：用 `phase-gate-runner`。本 Skill 关注的是"编号链路"，不是"清单项是否齐全"。
