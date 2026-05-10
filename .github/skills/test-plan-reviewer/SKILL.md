---
name: test-plan-reviewer
description: 对 H4 已产出的测试计划、测试矩阵、测试用例做证据级核查。当用户说"H4 评审"、"看看测试设计行不行"、"测试用例够不够"、"准备从 H4 进 H5"、"TestCaseAuthor 写完了帮我审"时主动调用。它与 phase-gate-runner 的差异是粒度：phase-gate-runner 是"清单是否齐"，本 Skill 是"每条 REQ 是否真有 TC 覆盖、TC 字段是否齐、Mock 边界是否真的写了、通过标准是否可机械判定"——重点机械检查 REQ × TC 矩阵的完整性与每条 TC 的可执行性。
when_to_use: |
  - H4 产物（test-plan / test-matrix / test-cases）已落盘，准备进 H5
  - TestCaseAuthor 自审通过后的二次复核（避免"AI 自我满足"）
  - H5 CodingExecutor 报"任务卡引用的 TC 字段不全"时回炉
when_not_to_use: |
  - H4 文档还在 draft——先让 TestCaseAuthor 补到 reviewed 再来
  - 用户在写新 TC——那是 TestCaseAuthor 的工作
  - 用户在做测试代码（H5 实现）——那是 CodingExecutor 的工作
---

# Skill: 测试用例证据评审器（Test Plan Reviewer）

## 1. 目的与原理

H4 是 Harness Engineering 反馈层的核心：测试用例不齐 / 不可执行，下游 H5 的"AI 自我修复"就失去对照基准。`TestCaseAuthor` 已经按 [`docs/stages/h4-test-design.md`](../../../docs/stages/h4-test-design.md)产出 TC，但它**自己写自己审**会自动开绿灯。本 Skill 提供独立的、机械化的证据核查表。

> 与 phase-gate-runner 的关系：phase-gate-runner 跑 H4 12 条勾选清单的"是否打勾"；本 Skill 答"REQ × TC 矩阵的具体哪一格缺、哪条 TC 字段不全、哪条通过标准是空话"。

## 2. 输入

必需：

- `docs/05-test-design/test-plan.md`（`status` ≥ `reviewed`）
- `docs/05-test-design/test-matrix.md`
- `docs/05-test-design/test-cases/`（全部 TC 文档）
- `docs/01-requirements/requirements.md`（用于核对 REQ 列表）
- `docs/04-detailed-design/`（用于核对每个程序文件是否有对应 TC）
- `docs/04-detailed-design/file-structure.md`（如有，作为"关键程序文件"清单的来源）
- `templates/phase-gate-checklist.md` 的 H4 段

## 3. 步骤

### 3.1 列出 REQ × TC 矩阵

逐项扫描 `requirements.md` 抽取所有 REQ-NNN，逐项扫描 `test-cases/` 抽取所有 TC-NNN（含其 `upstream` 字段或正文中"覆盖需求"），构建矩阵：

| REQ | TC（`upstream` 命中） | 覆盖路径 | 状态 |
| --- | --- | --- | --- |
| REQ-001 | TC-001, TC-002 | 正常+异常 | PASS |
| REQ-002 | TC-005 | 仅正常 | **PARTIAL（缺异常路径）** |
| REQ-003 | -（空） | - | **FAIL（无 TC 覆盖）** |

矩阵覆盖标准：

- 每条 REQ 至少 1 条 TC（[`docs/stages/h4-test-design.md` §4](../../../docs/stages/h4-test-design.md)）
- 涉及外部输入 / 边界值的 REQ → 至少 1 条正常 + 1 条异常
- 涉及权限的 REQ → 至少 1 条权限边界 TC
- 涉及并发 / 重试的 REQ → 至少 1 条并发 / 幂等 TC

### 3.2 对每条 TC 核查字段

按 [`agents/test-case-author/AGENT.md`](../../test-case-author/AGENT.md) 与 [`docs/stages/h4-test-design.md` §4](../../../docs/stages/h4-test-design.md) 列出的字段逐项核对：

| 字段 | 通过标准 | 常见放水形态（→ FAIL） |
| --- | --- | --- |
| 编号 / 标题 | TC-NNN + 一行描述 | 编号撞号 → FAIL |
| 上游 REQ / 设计 | ≥1 条 `REQ-NNN`，可选 `HD/API/DB-NNN` | 缺 `upstream` → FAIL |
| 前置条件 | 数据 / 状态 / 用户 / 权限 各项明确 | "正常状态" → PARTIAL |
| 输入 | 具体值（数据 / 参数 / 请求体） | "合法输入" → FAIL |
| 操作步骤 | 编号步骤，每步可机械执行 | "执行业务流程" → FAIL |
| 预期结果 | 可机械判定（HTTP 状态码 / 返回字段值 / DB 行数 / 日志关键字） | "返回正确" → FAIL |
| Mock 边界 | 显式列出哪些依赖被 Mock，桩返回值 | 缺失 → FAIL（除非显式标"集成测试，无 Mock"） |
| 通过标准 | 与"预期结果"一致或更具体 | 与预期结果重复 → PARTIAL |

每条 TC 得到：`PASS` / `PARTIAL` / `FAIL`。

### 3.3 文件级覆盖核查

读 `file-structure.md` 列出的"关键程序文件"清单，与 `test-matrix.md` 的"被测文件"列做交集：

| 关键文件 | 是否有 TC | 缺哪类 |
| --- | --- | --- |
| `src/Orders/CreateOrderHandler.cs` | TC-014, TC-015 | - |
| `src/Orders/InventoryService.cs` | -（空） | **未被覆盖** |
| `src/Auth/JwtMiddleware.cs` | TC-022 | 仅正常路径，**缺 token 过期 / 篡改** |

[`docs/stages/h4-test-design.md` §4](../../../docs/stages/h4-test-design.md) 要求每个关键程序文件都有定义。"关键"由 `file-structure.md` 决定；如果不写"关键"标记，本 Skill 默认所有列出的文件都需覆盖。

### 3.4 测试矩阵的可追溯性核查

`test-matrix.md` 必须维护以下追溯关系（[`docs/stages/h4-test-design.md` §5](../../../docs/stages/h4-test-design.md)）：

```text
需求编号 → 设计编号 → 文件路径 → 测试用例编号 → 测试文件 → 提交记录
```

机械检查：

- 每行至少前 4 列非空（"测试文件"和"提交记录"在 H4 时可为空）
- 编号格式合规（REQ-NNN / HD-NNN / TC-NNN）
- 引用的编号在对应文档中真实存在（grep 验证）

### 3.5 输出评审报告

报告**不写盘**，输出到对话框，由用户决定归档（建议 `docs/_review-records/H4-test-<YYYY-MM-DD>.md`）：

```markdown
# H4 Test Plan Review · <scope> · <YYYY-MM-DD>

- 受审产物：docs/05-test-design/* @ commit <sha>
- 评审依据：docs/stages/ §7 + templates/phase-gate-checklist.md H4

## REQ × TC 覆盖矩阵

| REQ | TC | 覆盖路径 | 状态 |
| --- | --- | --- | --- |
| REQ-001 | TC-001, TC-002 | 正常+异常 | PASS |
| REQ-002 | TC-005 | 仅正常 | PARTIAL |
| REQ-003 | -（空） | - | **FAIL** |

覆盖率：12/15 条 REQ 通过（80%）

## TC 字段核查

| TC | 上游 | 输入 | 步骤 | 预期 | Mock | 通过标准 | 总评 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| TC-001 | PASS | PASS | PASS | PASS | PASS | PASS | **PASS** |
| TC-005 | PASS | FAIL（输入"合法值"） | PASS | PARTIAL | FAIL | PASS | **FAIL** |
| ... |

## 文件级覆盖核查

- ✓ src/Orders/CreateOrderHandler.cs → TC-014, TC-015
- ✗ src/Orders/InventoryService.cs → 未被任何 TC 覆盖
- ! src/Auth/JwtMiddleware.cs → 仅正常路径，缺 token 过期 / 篡改场景

## 阻塞清单（必须修复后才能进 H5）

1. **REQ-003 无 TC 覆盖**：请补至少 1 条 TC，文件 `docs/05-test-design/test-cases/<group>.md`。
2. **TC-005 输入字段不可执行**："合法值"无法直接转成测试代码。请改为具体值（如 `username='alice', password='Pa$$w0rd'`）。
3. **InventoryService.cs 未覆盖**：在 `file-structure.md` 列为关键文件，但 `test-matrix.md` 无对应 TC。

## 待人工判断（UNKNOWN）

- TC-018 标"集成测试，无 Mock"——是否真的能在 CI 环境跑（需要外部服务）？由测试架构师确认。

## 放行结论

- ✗ 不可放行（3 条阻塞 + 1 条 UNKNOWN）
- 建议：先修阻塞清单，UNKNOWN 项作为评审会议程
```

### 3.6 落盘建议

- **未通过**：阻塞清单登记到任务板，回 `TestCaseAuthor` 修订。
- **通过**：归档报告到 `docs/_review-records/H4-test-<YYYY-MM-DD>.md`。

## 4. 失败模式与回退

- **`test-matrix.md` 不存在**：直接报阻塞——这是 H4 必交产物，不能用零散 TC 替代。
- **被要求评测试用例的"业务正确性"**（"这个验证逻辑该不该这么测"）：拒绝。本 Skill 只查字段齐 / 覆盖齐 / 可执行；评对错是测试架构师的工作。
- **TC 编号撞号**：立即停下，要求用户先去重，不要自己挑一个用。
- **REQ 列表与 requirements.md 不一致**：以 `requirements.md` 为真，要求 H4 修订。

## 5. 示例

见第 3.5 节输出模板。

## 6. 与其它 Skill / Agent 的边界

- 想**写新 TC**：用 `TestCaseAuthor` Agent。
- 想**核对清单**：用 `phase-gate-runner`。
- 想**追溯 REQ ↔ TC**：用 `traceability-linker`。本 Skill 关心"覆盖是否齐"，traceability-linker 关心"链路是否断"。
- 想**评测试代码实现**：用 `CodingExecutor` 的 self-verify 阶段，不是本 Skill。
