# 共享术语表

本文件统一定义本目录下所有 Agent 引用的术语，避免每个 `AGENT.md` 重复定义。所有术语与 [Harness Engineering 规范](../../README.md) 的章节保持同步。

## 1. 阶段术语

| 编号 | 名称 | 主要产物目录 |
| --- | --- | --- |
| H1 | 需求、UI 与交互原型 | `docs/01-requirements/`、`docs/02-prototype/`、`prototypes/` |
| H2 | 技术架构选型 | `docs/03-architecture/` |
| H3 | 详细设计 | `docs/04-detailed-design/` |
| H4 | 测试用例设计 | `docs/05-test-design/` |
| H5 | AI 编码与自验证 | `docs/06-implementation/` |
| H6 | 运行验证与文档回写 | `docs/07-release/` |

## 2. 三层 Harness

- **约束层（Constraint Harness）**：通过 `AGENTS.md`、Lint、类型系统等前馈控制缩小 Agent 解空间。
- **反馈层（Feedback Loop）**：通过测试、构建、运行结果向 Agent 回灌结构化信号。
- **门禁层（Quality Gate）**：在 CI / 评审 / 合并环节硬拦截不合规产物。

## 3. 关键产物

| 产物 | 路径 | 用途 |
| --- | --- | --- |
| 顶层规则 | `AGENTS.md` | Agent 规则的"目录"，限 100 行内 |
| 任务说明 | `templates/ai-task-brief.md` 派生 | H5 单次编码任务的输入 |
| 编码任务索引 | `docs/06-implementation/coding-tasks.md` | H5 任务总索引 |
| 提交记录 | `docs/06-implementation/commit-records.md` | 提交→设计→测试映射 |
| 执行计划 | `docs/06-implementation/exec-plans/active/<task-id>.md` | 跨多个设计项的复杂任务计划 |
| 技术债务 | `docs/06-implementation/exec-plans/tech-debt-tracker.md` | 已知技术债务追踪 |
| 追溯矩阵 | `docs/07-release/traceability-matrix.md` | 需求→设计→代码→测试→提交的最终追溯 |

## 4. 编号约定

| 前缀 | 含义 | 示例 |
| --- | --- | --- |
| `REQ-` | 需求项 | `REQ-001` |
| `UI-` | UI 项 | `UI-007` |
| `ADR-` | 架构决策记录 | `ADR-003` |
| `HD-` | 详细设计项（High-level Design item） | `HD-042` |
| `API-` | 接口设计项 | `API-005` |
| `DB-` | 数据库设计项 | `DB-012` |
| `TC-` | 测试用例 | `TC-128` |
| `TASK-` | H5 编码任务 | `TASK-2026-04-21-001` |

编号一旦发布即视为不可变，废弃项标记 `[Deprecated]` 而不是删除编号。

## 5. 评审结论

- `Approved`：通过，可进入下一阶段
- `Approved with Changes`：小修改后可进入下一阶段
- `Rejected`：不通过，必须返工
- `Pending`：信息不足，暂缓决策
