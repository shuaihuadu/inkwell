---
id: prototype-review-<slug>
stage: H1
status: draft
authors:
  - name: <姓名或 Agent 名>
    role: <owner|agent>
reviewers: []
created: <YYYY-MM-DD>
updated: <YYYY-MM-DD>
upstream:
  - ui-spec-<slug>
  - user-flow-<slug>
  - acceptance-criteria-<slug>
  - prototypes/<slug>/
downstream: []
---

# H1 原型评审记录 · `<slug>`

> **本文件谁动手 / 在哪填**：
>
> - **Agent 起草**：第 1~4 节（受审产物清单、机械化核对、阻塞汇总、Agent 建议结论）由起草者自动填写；第 5 节"评审决议"的字段由人工通过交互式提问收集选择后回写。
> - **人工签字**：检查第 1~4 节的证据是否充分；在第 5 节明确选择"评审决议 / 主审人 / 评审日期"；如要 override 某条机械化结论，需在第 5 节选中并补理由；最后人工把本文件 frontmatter `status: draft → reviewed` 并提交。
> - **AI 不给自己开绿灯**：起草者决不自行把 `status` 翻成 `reviewed`；评审决议**没有默认选项**，必须人工显式选择 Approved / Approved with Changes / Rejected / Pending 之一。

---

## 1. 受审产物清单

<列出本轮评审覆盖的全部产物：需求说明 / UI 说明 / 用户流 / 验收标准 / 原型目录，各自的路径、当前 `status`、数量规模（如 REQ 编号区间、UI 页面数、原型截图张数）。>

| 产物     | 路径 | 状态 / 数量 |
| -------- | ---- | ----------- |
| 需求说明 |      |             |
| UI 说明  |      |             |
| 用户流   |      |             |
| 验收标准 |      |             |
| 原型目录 |      |             |

## 2. 机械化核对

> 字段口径固定为下列 12 条，逐条给出 PASS / FAIL / UNKNOWN 结论；证据与人工 override 放在结论表之后的子段，避免中英文混排长内容触发表格宽度类 lint 问题。

### 2.1 结论速览

| #   | 检查项           | 结论 |
| --- | ---------------- | ---- |
| 1   | 需求背景清楚     |      |
| 2   | 用户角色明确     |      |
| 3   | 核心场景完整     |      |
| 4   | 功能范围明确     |      |
| 5   | 不做范围明确     |      |
| 6   | UI 页面清单完整  |      |
| 7   | 页面状态完整     |      |
| 8   | 异常提示明确     |      |
| 9   | 权限边界明确     |      |
| 10  | 验收标准可验证   |      |
| 11  | 可交互原型已评审 |      |
| 12  | 评审记录已保存   |      |

### 2.2 逐条证据 + 人工 override

<每条检查项一个子标题（`#### #N <检查项> · <结论>`），下面用 bullet 给出证据（引用具体文件章节 / 具体页面状态数 / 具体编号区间）和"人工 override"字段（默认"无"）。第 12 条"评审记录已保存"在评审记录本身写完前恒为 UNKNOWN，不算阻塞。>

#### #1 需求背景清楚 · <结论>

- **证据**：
- **人工 override**：无。

## 3. 阻塞汇总

> 把所有 `FAIL` 与"会卡住下一阶段"的 `UNKNOWN` 收进这里。"评审记录已保存"本身不算阻塞——它就是本文件存在的理由。

- <列出 FAIL 项，或写"无 FAIL 项">
- <列出会卡住下一阶段的 UNKNOWN 项，或写"无">

## 4. Agent 建议结论（仅供参考）

- [ ] 建议 PASS：全部检查项 PASS（仅"评审记录已保存"UNKNOWN，且待本签字流程关闭）→ 建议人工选择 `Approved`
- [ ] 建议 FAIL：存在 FAIL 项 → 建议人工选择 `Rejected` 或 `Approved with Changes`
- [ ] 建议补充：存在 UNKNOWN 项（且不止"评审记录已保存"一条）→ 建议人工选择 `Pending`

> 本节是机械化建议，**不是评审决议**。决议必须由人工在第 5 节显式选择。

## 5. 评审决议（人工填）

<!-- 人工确认收集点：
     - decision: 选项=[Approved, Approved with Changes, Rejected, Pending] · 必选，无默认
     - chair:    主审人姓名
     - date:     评审日期
     - overrides: 第 2 节中被 override 的检查项 + override 理由（如无则写"无"）
     - modifications: 自由 prose（修改项 / 后续动作 / 评审会讨论纪要）
     decision 字段不允许由 Agent 自行预填
-->

- **评审决议**：

  > <Approved / Approved with Changes / Rejected / Pending，及一句话理由>

- **主审人**：

  > <姓名>

- **评审日期**：

  > `YYYY-MM-DD`

- **第 2 节 override 项**（人工对机械化结论持不同意见时）：

  > <具体 override 项 + 理由，或"无">

- **修改项 / 后续动作**：

  > <具体修改项 + 负责人 + 截止时间，或"无修改项"及评审人原话>

## 6. 完成后下一步

> 人工选完第 5 节决议后，按以下规则操作；本节不需要填，仅作 checklist。

- **Approved**：把本文件 frontmatter `status: draft → reviewed`、在 `reviewers:` 加一行；把评审摘要归档到 `docs/07-reviews/`；把原型 `coverage.md` frontmatter `status: draft → reviewed`；进入下一阶段（架构设计）。
- **Approved with Changes**：同 Approved，但需在第 5 节"修改项"里把每条改动派负责人 + 截止时间；下一阶段可启动，但下一阶段入口评审时需复核这些修改是否落实。
- **Rejected**：上游产物 `status` 保留 `draft`；本文件 `status` 标 `needs-revision`；把"返工方向"登记到任务看板。
- **Pending**：第 2 节 UNKNOWN 项的"如何解决"已经是补救清单；登记到任务看板，补完信息后重跑本评审。

---

> **AI 边界**：本文件的 `status` 字段、`reviewers` 字段、第 5 节的人工填答位**只能由人写或经显式确认后回写**——AI 永远不会自己把决议写成 `Approved` 来"自我通过"。
