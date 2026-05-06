---
title: 项目任务看板
status: living
updated: <YYYY-MM-DD>
maintainer: <人或调度 Agent>
---

# 项目任务看板

> 本文件是项目级"任务总览"，回答的是"现在在做什么、做到哪一步、对应文档落在哪里"。
> 它**不是**待办列表，也不替代 issue tracker；它是给新接手的人或新一轮的需求分析快速建立全局视野的入口。
> 维护原则：谁推进任务，谁顺手更新对应行；不允许只挂"漂亮但没人维护"的状态。
>
> **运行时位置**：本模板在采用方仓库的运行时实例位于 `docs/06-tasks/task-board.md`。`/new-task` 第一次运行时会自动按本模板创建到该路径，**不要把它复制到仓库根**。
> 阶段定义见 `.harness-engineering/docs/stages.md`，仓库结构入口见 `.harness-engineering/docs/repo-layout.md`。

## 1. 在跑任务（H1–H5）

| 任务编号 | 标题 | 当前阶段 | 文档目录 | 阻塞 / 风险 | 最近一次推进 |
| ---- | ---- | ---- | ---- | ---- | ---- |
| TASK-YYYY-MM-DD-001 |  | H1 / H2 / H3 / H4 / H5 | `docs/0X-xxx/...` |  | YYYY-MM-DD |

字段约定：
- **当前阶段**：取自 H1–H6（阶段定义见 `.harness-engineering/docs/stages.md`）；阶段切换时更新本字段，旧阶段的产物路径在文档目录字段保留可点
- **文档目录**：当前阶段的主要产物落点（如 `docs/04-detailed-design/feature-x/`）；缺失时填 "—"
- **阻塞 / 风险**：写"阻塞了谁、需要谁解"，不写"觉得有点问题"
- **最近一次推进**：上次有 commit / 评审记录 / 文档更新的日期，超过两周未动应进入"暂缓"

## 2. 等待人工决策

| 任务编号 | 阻塞点 | 需要谁拍板 | 截止 | 暂缓后果 |
| ---- | ---- | ---- | ---- | ---- |
|  |  |  |  |  |

> 对应 Agent 阻塞返回（详见源仓 `agents/_shared/io-contracts.md` 第 5 节）的人工出口：阻塞条目应在这里有一行登记，避免只停在某个会话里。

## 3. 已交付任务

| 任务编号 | 标题 | 交付日期 | 发布说明 | 追溯矩阵 |
| ---- | ---- | ---- | ---- | ---- |
| TASK-YYYY-MM-DD-001 |  | YYYY-MM-DD | `docs/07-release/release-notes.md#...` | `docs/07-release/traceability-matrix.md#...` |

> 已交付任务保留至少一个版本周期，便于"新需求是不是旧需求的延续"这种判断。

## 4. 暂缓 / 取消

| 任务编号 | 标题 | 暂缓原因 | 决策日期 | 复活条件 |
| ---- | ---- | ---- | ---- | ---- |
|  |  |  |  |  |

## 5. 与其他索引的关系

- 代码侧的"从哪进门"：见 `.harness-engineering/docs/repo-layout.md`（仓库结构、目录职责、模块落点）
- 阶段定义与门禁：见 `.harness-engineering/docs/stages.md`
- 当前活跃执行计划：`docs/06-implementation/exec-plans/active/`

任务看板偏需求侧（"在做什么、做到哪了"），repo-layout 偏代码侧（"该改在哪"）。两个入口配合使用，新接手的人或下一轮的需求分析 Agent 才能在不重读全部历史的前提下建立全局视野。
