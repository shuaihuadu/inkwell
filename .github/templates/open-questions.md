---
id: open-questions-<feature>
stage: H1 # 或 H2 / H3
status: pending # pending / partially-resolved / closed
authors:
  - name: <Agent 名 / 起草人姓名>
    role: agent # 或 human
reviewers: [] # 评审人由人工填写，格式见 io-contracts.md 第 2 节
created: <YYYY-MM-DD>
updated: <YYYY-MM-DD>
upstream: [] # 关联的上游产物 id（如 requirements-<feature>）
downstream: []
---

# <特性名> · 待澄清清单（Open Questions）

> **本文件谁动手 / 在哪填**：
>
> - **Agent 起草** OQ-NNN 题干 + 候选答 + 影响范围（status: `pending`）。
> - **人工填答**：每条 OQ 末尾 `**回答**` 行就是你的输入位——把 `> **[ 待填 ]**：...` 整行替换成你的选择（例如 `> **回答**：选 A，理由：...`）。
> - **Agent 回写**：人工答完之后，由对应的 Agent 在评审中把 `卡点等级` 改成 `closed YYYY-MM-DD` 并回填 `回写` 行（指向上游产物的具体段落）。
>
> 不会用 `<TBD>` 占位混过去；Agent 答不出来的全部进 OQ。

> **OQ 关闭时的交互形式**：Agent 在 chat 里收集"回答 / 决策日期 / 决策人"三字段时，**必须**按 [`io-contracts.md` §6.1](../agents/_shared/io-contracts.md#61-交互式输入约定pick-over-type) 用一次 `ask.user` picker 同时问完三项：候选答从下方 A/B/C/D 直接生成 options[]，决策日期 options=[今天, 昨天, 自由输入]，决策人 options=[`git config user.name`, 历史 reviewer..., 自由输入]。**禁止**让用户在 chat 里手动打字填这三个字段。

## 字段说明

- **问题**：一句话讲清楚要决策什么，配 1–2 句"为什么需要答"。
- **影响范围**：列出哪些 REQ / NFR / UI / DB / 异常 / 风险会被这条 OQ 的回答改写。
- **候选答**：列出 **有限可选项**（A/B/C/D）让用户挑，不替用户拍默认值。每条候选要写清楚"选这条会带来什么后果"。
- **回答**：**人工输入位**——把 `> **[ 待填 ]**：...` 整行替换成你的选择。允许写"自定义"，但要明确给出与候选答的差异。
- **决策日期** / **决策人**：人工填，用于追溯。
- **卡点等级**：
  - `blocking` = 不答这条，对应阶段无法进入下一关（Agent 会硬阻塞）。
  - `non-blocking` = 不答这条，可继续，但会以风险形式追踪。
  - `closed YYYY-MM-DD` = 已关闭，由人工答 + Agent 回写后置位。
- **回写**：Agent 在 OQ 关闭时回填——记录回写到哪个上游产物的哪段 / 哪条。

---

## OQ-001 <一句话标题>

- **问题**：<一句话讲要决策什么>
- **为什么需要答**：<不答会导致什么阶段卡住、什么决策悬空>
- **影响范围**：<REQ-001、NFR-002、风险 R3 等>
- **候选答**：
  - **A**. <选项 A 的具体方案>。后果：<选 A 之后会引发什么>。
  - **B**. <选项 B 的具体方案>。后果：<选 B 之后会引发什么>。
  - **C**. <选项 C，可选>。后果：<选 C 之后会引发什么>。
  - **D**. 其他（请显式列出）。
- **回答**：

  > **[ 待填 ]**：在下一行替换本行——写下你的选择（A / B / C / 自定义），1–2 句话补充理由。

- **决策日期**：

  > **[ 待填 ]**：YYYY-MM-DD

- **决策人**：

  > **[ 待填 ]**：<姓名 / 角色>

- **卡点等级**：blocking
- **回写**：<由 Agent 在关闭时回填，例：requirements.md 第 5 节 REQ-001 标 MVP；REQ-005 撤销并入 ND-007>

---

## OQ-002 ...

（同上结构复制）

---

> **完成后下一步**：
>
> 1. 把所有 `blocking` 项的"回答 / 决策日期 / 决策人"三行都换成实际内容（最少需要的一步）。
> 2. 切到对应阶段的 Agent（H1 → `H1-RequirementsInterviewer` / `H1-UISpecAuthor`，H2 → `H2-ArchitectAdvisor`），让它把答案回写到上游产物（`requirements.md` / `architecture.md` 等），并把对应 OQ 的"卡点等级"改成 `closed YYYY-MM-DD`、"回写"行填上具体落点。
> 3. 上游产物 status 维持 `draft`——评审纪要走 `/log-review` 之后由人工把 `status: draft → reviewed`（参见 `.he/HANDBOOK.md` Q7）。
> 4. 仍有 `non-blocking` 项时不阻塞推进，但要在 `risk-analysis.md` 或下一阶段 `open-questions-<stage>.md` 里继续追踪。
