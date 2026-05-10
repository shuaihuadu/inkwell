# 评审记录模板

> **Agent 起草指引**：本表 §1 / §3 的字段属于 [`io-contracts.md` §6.1](../agents/_shared/io-contracts.md#61-交互式输入约定pick-over-type) 的"封闭枚举 / 半结构化"类型，**必须**通过 `ask.user` 一次性收集，不要让用户自己打字填表。

## 1. 基本信息

<!-- ask.user batch: 7 questions, options[] from detection where possible:
     - 项目名称: 仓库名 (`basename $PWD`) / 当前 feature 名 / 自由输入
     - 评审阶段: H1 / H2 / H3 / H4 / H5 / H6
     - 评审对象: 当前阶段的事实源文件列表
     - 评审时间: 今天 / 昨天 / 自由输入 (YYYY-MM-DD)
     - 评审地点: 线上 / 现场 / 异步 / 自由输入
     - 主持人 / 记录人: `git config user.name` + `git log --format=%an | sort -u | head -10` + 自由输入
     - 参与人员: 多选 from same git log + 自由输入 -->

- 项目名称：
- 评审阶段：
- 评审对象：
- 评审时间：
- 评审地点：
- 主持人：
- 记录人：
- 参与人员：

## 2. 评审材料

| 材料 | 路径或链接 | 版本 |
| --- | --- | --- |
| 需求说明 |  |  |
| UI 说明 |  |  |
| 架构说明 |  |  |
| 详细设计 |  |  |
| 测试用例 |  |  |
| 代码提交 |  |  |

## 3. 评审结论

<!-- ask.user single picker, options[]:
     - Approved
     - Approved with Changes
     - Rejected
     - Pending
     io-contracts.md §6.1 封闭枚举 → MUST picker, NOT freeform -->

请选择一个结论：

- [ ] Approved：通过，可进入下一阶段
- [ ] Approved with Changes：小修改后可进入下一阶段
- [ ] Rejected：不通过，必须返工
- [ ] Pending：信息不足，暂缓决策

> **完成后下一步**：
>
> 1. **Approved / Approved with Changes**：人工去上游产物把 frontmatter `status: draft` 改成 `reviewed`、在 `reviewers:` 加一行（参见 `.he/HANDBOOK.md` Q7）；本节四类下一阶段动作见 `templates/phase-gate-checklist.md` 各 H 阶段末尾的"完成后下一步"。
> 2. **Rejected**：上游 `status` 保留 `draft`，并把"返工方向"写到本表第 5 节"修改项"，每条都派负责人 + 截止时间；不要让上游产物被动跳过去。
> 3. **Pending**：把"缺什么、问谁、什么时候补齐"登记到 `docs/06-tasks/task-board.md` 第 2 节"等待人工决策"——这是规范层的人工出口，避免阻塞悬空在某个会话里。

## 4. 通过项

| 编号 | 内容 | 说明 |
| --- | --- | --- |
|  |  |  |

## 5. 修改项

| 编号 | 问题 | 负责人 | 截止时间 | 状态 |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |

## 6. 风险项

| 编号 | 风险 | 影响 | 缓解方案 | 负责人 |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |

## 7. 决策记录

| 编号 | 决策 | 原因 | 替代方案 | 影响 |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |

## 8. 下一步动作

> 评审纪要落档后，把每条下一步动作**同时**登记到 `docs/06-tasks/task-board.md`：可立即开工的进第 1 节"在跑任务"；需要人拍板的进第 2 节"等待人工决策"。本表是评审现场快照，task-board 是项目级唯一可信的"下一步去哪"。两边脱钩 = 评审过了但没人推进。

| 动作 | 负责人 | 截止时间 | 验收方式 |
| --- | --- | --- | --- |
|  |  |  |  |

## 9. 备注


