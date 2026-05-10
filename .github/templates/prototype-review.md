---
id: prototype-review-<feature>
stage: H1
status: draft # draft / reviewed / approved / needs-revision；本字段由人工在 picker 选完决议、确认无误后翻
authors:
  - name: H1-PrototypeReviewer
    role: agent
reviewers: [] # 评审人由人工 picker 选定后写入；格式见 io-contracts.md 第 2 节
created: <YYYY-MM-DD>
updated: <YYYY-MM-DD>
upstream:
  - ui-spec-<feature>
  - user-flow-<feature>
  - acceptance-criteria-<feature>
  - prototypes/<feature>/
downstream: []
---

# H1 原型评审记录 · `<feature>`

> **本文件谁动手 / 在哪填**：
>
> - **Agent 起草**：第 1–4 节（受审产物清单、12 条机械化核对、阻塞汇总、Agent 建议结论）由 `H1-PrototypeReviewer` 自动填写；第 5 节"评审决议"的字段由 Agent 在 chat 里通过 `ask.user` picker 收集人工选择后回写。
> - **人工签字**：检查第 1–4 节的证据是否充分；通过 picker 选第 5 节的"评审决议 / 主审人 / 评审日期"；如要 override Agent 的某条结论，picker 选"override"并补理由；最后人工把本文件 frontmatter `status: draft → reviewed` 并提交。
> - **AI 不给自己开绿灯**：Agent 决不会自行把 `status` 翻成 `reviewed`；评审决议的 picker **没有默认选项**，必须人工显式选择 Approved / Approved with Changes / Rejected / Pending 之一。

---

## 1. 受审产物清单（Agent 自动填）

| 产物 | 路径 | 状态 / 数量 |
| --- | --- | --- |
| 需求说明 | `docs/01-requirements/requirements.md` | `<status>` |
| UI 说明 | `docs/01-requirements/ui-spec.md` | `<UI-NNN 数量>` / `<status>` |
| 用户流 | `docs/01-requirements/user-flow.md` | `<流数量>` / `<status>` |
| 验收标准 | `docs/01-requirements/acceptance-criteria.md` | `<AC-NNN 数量>` / `<status>` |
| 原型目录 | `prototypes/<feature>/` | markdown 描述 `<N>` 份 / 截图 `<N>` 张 |

## 2. 12 条机械化核对（Agent 自动填）

> 字段口径见 `agents/prototype-reviewer/prompt.md` 第四步表格；本节由 Agent 严格按 phase-gate-checklist H1 那 12 条原文照搬。"人工 override" 列默认空，仅当评审会上人工对 Agent 结论持不同意见时由 picker 写入"改判结论 + 简短理由"。

| # | 模板项 | Agent 结论 | 证据 / 原因 | 人工 override |
| --- | --- | --- | --- | --- |
| 1 | 需求背景清楚 | PASS / FAIL / UNKNOWN | `<文件:行号 / 截图文件名>` | (空) |
| 2 | 用户角色明确 | ... | ... | (空) |
| 3 | 核心场景完整 | ... | ... | (空) |
| 4 | 功能范围明确 | ... | ... | (空) |
| 5 | 不做范围明确 | ... | ... | (空) |
| 6 | UI 页面清单完整 | ... | ... | (空) |
| 7 | 页面状态完整 | ... | ... | (空) |
| 8 | 异常提示明确 | ... | ... | (空) |
| 9 | 权限边界明确 | ... | ... | (空) |
| 10 | 验收标准可验证 | ... | ... | (空) |
| 11 | 可交互原型已评审 | ... | ... | (空) |
| 12 | 评审记录已保存 | UNKNOWN | 本文件即评审记录，待人工签署后由 Agent 改写为 PASS | (空) |

## 3. 阻塞汇总（Agent 自动填）

> Agent 把所有 `FAIL` 与"会卡住 H2"的 `UNKNOWN` 收进这里。`UNKNOWN-12（评审记录已保存）`本身**不算阻塞**——它就是本文件存在的理由。

- [ ] `<FAIL 项 #N · 项名>` · 缺口：`<具体描述>` · 补救：`<回到哪个 Agent / 哪个文档补>`
- [ ] `<UNKNOWN 项 #N · 项名>` · 缺信息：`<reason>` · 如何补：`<how_to_resolve>`

## 4. Agent 建议结论（Agent 自动填，仅供参考）

- [ ] 建议 PASS：12 条全部 PASS（或仅第 12 条 UNKNOWN）→ 建议人工选择 `Approved`
- [ ] 建议 FAIL：存在 FAIL 项 → 建议人工选择 `Rejected` 或 `Approved with Changes`
- [ ] 建议补充：存在 UNKNOWN 项（且不止第 12 条）→ 建议人工选择 `Pending`

> 本节是 Agent 的"机械化建议"，**不是评审决议**。决议必须由人工在第 5 节通过 picker 显式选择。

## 5. 评审决议（人工填，picker 收集）

<!-- ask.user batch (一次调用，5 个 questions):
     - decision: options=[Approved, Approved with Changes, Rejected, Pending] · 必选，无默认
     - chair:    options=[<git config user.name>, <历史 reviewer...>, 自由输入]
     - date:     options=[<今天 YYYY-MM-DD>, <昨天>, 自由输入]
     - overrides: 多选 from 第 2 节中 Agent 给出 PASS 的项；如本场会议人工要 override 某条改成 FAIL/UNKNOWN，在此选中并补理由
     - modifications: 自由 prose（修改项 / 后续动作 / 评审会讨论纪要）
     io-contracts.md §6.1 → MUST picker；decision 字段不允许 Agent 自行预填
-->

- **评审决议**：

  > **[ 待填 ]**：在下一行替换本行——picker 选 `Approved` / `Approved with Changes` / `Rejected` / `Pending` 之一，并补 1–2 句理由。

- **主审人**：

  > **[ 待填 ]**：picker 选当前用户 / 历史 reviewer / 自由输入。

- **评审日期**：

  > **[ 待填 ]**：picker 选今天 / 昨天 / 自由输入（YYYY-MM-DD）。

- **第 2 节 override 项**（人工对 Agent 结论持不同意见时）：

  > **[ 待填 ]**：列出"第 #N 项原结论 X，本会议改为 Y，理由：…"；无 override 写"无"。

- **修改项 / 后续动作**：

  > **[ 待填 ]**：列出本次评审会讨论的修改项 / 待跟进事项。无则写"无"。

## 6. 完成后下一步

> 人工选完第 5 节决议后，按以下规则操作；本节不需要填，仅作 checklist。

- **Approved**：把本文件 frontmatter `status: draft → reviewed`、在 `reviewers:` 加一行（参见 [`io-contracts.md` 第 2 节](../agents/_shared/io-contracts.md#2-markdown-frontmatter-约定)）；触发 `/log-review` 把本评审摘要归档到 `docs/07-reviews/YYYY-MM-DD-h1-prototype-review.md`；可进入 H2。
- **Approved with Changes**：同 Approved，但需在第 5 节"修改项"里把每条改动派负责人 + 截止时间，由对应 Agent 跟进；H2 可启动，但 H2 入口的 `repo-impact-map.md` 评审会复核这些修改是否落实。
- **Rejected**：上游产物 `status` 保留 `draft`；本文件 `status` 标 `needs-revision`；把"返工方向"贴到 `docs/06-tasks/task-board.md` 第 2 节"等待人工决策"。
- **Pending**：第 2 节 UNKNOWN 项的 `how_to_resolve` 已经是补救清单；登记到 `task-board.md` 第 2 节，补完信息后重跑本 Agent。

---

> **AI 边界**：本文件由 `H1-PrototypeReviewer` 起草，但 `status` 字段、`reviewers` 字段、第 5 节的人工填答位**只能由人写或经 picker 显式选择后由 Agent 回写**——Agent 永远不会自己把决议写成 `Approved` 来"自我通过"。这是 [`docs/stages/h1-requirements-and-prototype.md`](../docs/stages/h1-requirements-and-prototype.md) §6 评审门禁的硬约束。
