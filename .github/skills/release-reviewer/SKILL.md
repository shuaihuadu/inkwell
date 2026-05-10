---
name: release-reviewer
description: 对 H6 已产出的 release-notes.md、traceability-matrix.md 与本次发布范围的 commit-records 做整体复核。当用户说"H6 评审"、"这版能不能发"、"release notes 写完了帮我审"、"准备打 tag 之前再看一眼"时主动调用。它与 commit-auditor 的差异是粒度：commit-auditor 是**单 commit 的硬门禁**（六字段是否齐），本 Skill 是**release 范围的整体复核**（每条变更是否能反向追到 commit + Task/REQ、破坏性变更是否给迁移指引、known-issues 是否登记、追溯矩阵是否回写完毕）。
when_to_use: |
  - 准备打版本 tag / 发包之前的最后一道检查
  - ReleaseNoteWriter 自审通过后的二次复核
  - 已发版本回头审计（合规 / 客户问询时）
when_not_to_use: |
  - 单条 commit 检查——用 commit-auditor
  - release-notes 还没起草——先让 ReleaseNoteWriter 写
  - 发布后的真实运行回写（H6 后半段）——那是 DocGardener / 各 Agent 的事，不是 release 评审
---

# Skill: 发布证据评审器（Release Reviewer）

## 1. 目的与原理

H6 是闭环阶段：每条已合入的代码必须能反向追到一条已评审的 REQ，每条破坏性变更必须给客户迁移指引，每条 known-issue 必须有登记。`ReleaseNoteWriter` 已经按 [`docs/stages/h6-release.md`](../../../docs/stages/h6-release.md)产出 release notes，但它**自己写自己审**会自动开绿灯：

- 漏写一条 PR / commit → release notes 缺条目 → 客户出问题时无法回溯
- 破坏性变更没有迁移指引 → 客户升级踩坑 → 后续返工
- 追溯矩阵没回写 → 下个版本审计无凭证 → 整套 Harness 失效

本 Skill 提供独立的、机械化的整体复核：把 commit 范围、release notes、追溯矩阵三方做交叉对账。

> 与 `commit-auditor` Agent 的关系：commit-auditor 关心"单条 commit 字段齐"，本 Skill 关心"整段 commit 范围的覆盖齐 + 追溯链回写齐"。两者层级不同，必须都跑。
> 与 `phase-gate-runner` 的关系：phase-gate-runner 跑 H6 11 条勾选清单的"是否打勾"；本 Skill 答"打勾的具体证据是什么、追溯矩阵的某行某列为什么是空的"。

## 2. 输入

必需：

- `docs/07-release/release-notes.md`（本次发布版本）
- `docs/07-release/traceability-matrix.md`
- `docs/06-implementation/commit-records.md`
- 发布范围的 git tag / commit 区间（如 `v0.3.0..HEAD` 或两个 sha）
- `templates/phase-gate-checklist.md` 的 H6 段
- `docs/01-requirements/requirements.md`（用于回查 REQ 是否真实存在）

可选：

- `docs/06-implementation/exec-plans/tech-debt-tracker.md`（known-issues 来源）
- 上一版本的 release-notes（用于增量比对）
- CHANGELOG.md（如果团队也维护这个）

## 3. 步骤

### 3.1 圈定发布范围

确认本次评审的 commit 区间。如果用户没给，反问而不是猜：

- 上一个 git tag 是什么？
- 当前要发的 tag / 版本号是什么？
- 是否包含未合入主干的 PR（不应该有，但有时存在）？

落地后用 `git log <prev>..<curr> --oneline` 拉出本次范围内的所有 commit。

### 3.2 commit ↔ release-notes 双向对账

构建两个集合：

- **C 集**：发布范围内的全部 commit（`Task:` 字段非空）
- **N 集**：release-notes.md 中提及的 commit / Task / PR

**正向（C → N）**：每条 commit 必须能在 release-notes 找到对应条目，否则就是"漏写"。例外：

- `chore:` / `ci:` / `build:` / `style:` 等纯工程性 commit 可以聚合到"内部改进"小节
- `docs:` 仅当涉及对外文档（用户手册 / API docs）才进 release notes

**反向（N → C）**：release-notes 提到的每条变更必须能找到至少 1 条 commit 支撑，否则就是"凭印象写"。

### 3.3 追溯链反向核查

对发布范围内每条 commit，按 [`agents/_shared/io-contracts.md` §4](../../_shared/io-contracts.md) 抽出 `Design:` / `Tests:` / `Task:` 字段，验证：

| 检查项 | 怎么查 |
| --- | --- |
| Design: HD-NNN 是否真实存在 | grep `docs/04-detailed-design/` |
| Tests: TC-NNN 是否真实存在 | grep `docs/05-test-design/` |
| Task: TASK-... 是否真实存在 | grep `docs/06-tasks/` |
| 各编号 → 上游 REQ-NNN 是否最终能追到 `requirements.md` | 链式 grep |
| `traceability-matrix.md` 是否已为本次范围回写 REQ ↔ HD ↔ TC ↔ Task ↔ Commit | 取本次 commit sha 列做存在性检查 |

任一断链 → 该 commit 进"追溯失败清单"。

### 3.4 破坏性变更核查

扫 release-notes 的"破坏性变更"小节。每条破坏性变更必须包含：

- **变更内容**（API 字段移除 / 协议变更 / 数据格式变更 / 默认值变更 ……）
- **影响范围**（哪些消费方、哪些版本）
- **迁移指引**（具体步骤，可复制粘贴的命令或代码）
- **回滚指引**（如何回到旧版本）

四项任缺一项 → `PARTIAL`；整段无迁移指引 → `FAIL`。

> 隐藏的破坏性变更检查：扫 commit 中的 `feat!:` / `fix!:` 标记或 commit body 中的 `BREAKING CHANGE:` 段，必须在 release-notes 都有对应条目。漏一条即 `FAIL`。

### 3.5 known-issues 与 tech-debt 核查

- `tech-debt-tracker.md` 中状态为 `open` 且影响发布的条目 → 必须在 release-notes 的"已知问题"小节有对应条目
- 反过来：release-notes 提到的每条 known-issue 必须能在 `tech-debt-tracker.md` 找到登记
- known-issue 必须包含"绕过方案"或"修复时间表"，二者至少有其一

### 3.6 输出评审报告

报告**不写盘**，由用户决定归档（建议 `docs/_review-records/H6-release-<version>-<YYYY-MM-DD>.md`）：

```markdown
# H6 Release Review · v<X.Y.Z> · <YYYY-MM-DD>

- 受审产物：docs/07-release/release-notes.md @ commit <sha>
- 发布范围：v0.2.4..HEAD（共 N 条 commit）
- 评审依据：docs/stages/ §9 + templates/phase-gate-checklist.md H6

## commit ↔ release-notes 对账

- 总 commit 数：N
- release-notes 中已提及：N-2
- **漏写**（C 中有 N 中无）：
  - `abc1234` feat(orders): introduce buffered aggregator → release-notes 未提及（任务卡 TASK-2026-04-15-003）
- **凭印象写**（N 中有 C 中无）：
  - "优化登录性能" → 找不到对应 commit

## 追溯链反向核查

| commit | Design | Tests | Task | 上游 REQ | traceability-matrix 已回写 |
| --- | --- | --- | --- | --- | --- |
| abc1234 | HD-003 ✓ | TC-014 ✓ | TASK-2026-04-15-003 ✓ | REQ-007 ✓ | ✗ 未回写 |
| def5678 | HD-005 ✓ | TC-016 ✗（不存在） | TASK-2026-04-22-001 ✓ | REQ-009 ✓ | ✓ |

## 破坏性变更核查

| 变更 | 内容 | 影响 | 迁移指引 | 回滚 | 总评 |
| --- | --- | --- | --- | --- | --- |
| API `/v1/orders` 移除 `legacy_total` 字段 | PASS | PASS | PASS | FAIL（缺回滚） | PARTIAL |

> 隐藏破坏性变更扫描：发现 commit `def5678` body 含 `BREAKING CHANGE:`，但 release-notes 未单独成节。

## known-issues 核查

- ✓ tech-debt-tracker 中 3 条 open 已全部出现在 release-notes
- ✗ release-notes 中"高并发下偶发超时"未在 tech-debt-tracker 登记

## 阻塞清单（必须修复后才能发版）

1. **abc1234 漏写**：补 release-notes 条目，或显式标记"内部改进"
2. **TC-016 不存在**：commit `def5678` 引用的测试编号在 `docs/05-test-design/` 找不到，请修正
3. **追溯矩阵未回写 abc1234**：在 `traceability-matrix.md` 补对应行
4. **破坏性变更缺回滚指引**：API `/v1/orders` 字段移除条目下补"如何回到旧版本"段
5. **隐藏破坏性变更**：commit `def5678` 的 BREAKING CHANGE 必须在 release-notes 单独成节
6. **未登记的 known-issue**：把"高并发下偶发超时"补到 `tech-debt-tracker.md`

## 待人工判断（UNKNOWN）

- "凭印象写"清单中"优化登录性能"——是否对应到没有 Task 字段的早期 commit？由发版人确认是否补 commit 元数据或删除该条。

## 放行结论

- ✗ 不可放行（6 条阻塞 + 1 条 UNKNOWN）
- 建议：先修阻塞清单，UNKNOWN 项作为发版会议程
```

### 3.7 落盘建议

- **未通过**：阻塞清单登记到任务板，回 `ReleaseNoteWriter` 修订；追溯矩阵缺项可能需要 `traceability-linker` 协助回写。
- **通过**：归档报告到 `docs/_review-records/H6-release-<version>-<YYYY-MM-DD>.md`，作为打 tag / 发包前的放行凭证。

## 4. 失败模式与回退

- **commit 范围模糊**：拒绝"猜个差不多"。要求用户给出明确的起止 sha 或 tag。
- **release-notes 与 CHANGELOG 同步问题**：本 Skill 只看 release-notes；CHANGELOG 与之的同步由项目自定义工具或 docs-gardener 处理。
- **被要求评 release-notes 的"市场表达"是否到位**（"这段写得吸引客户吗"）：拒绝。本 Skill 只查证据齐 / 链路齐 / 字段齐，不评文案。
- **本次发布回退已发版本**：报告"回退发布需特殊流程"，建议同时跑一次 `traceability-linker` 检查反向链路是否仍然成立。

## 5. 示例

见第 3.6 节输出模板。

## 6. 与其它 Skill / Agent 的边界

- 想**写新 release-notes**：用 `ReleaseNoteWriter` Agent。
- 想**单条 commit 硬门禁**：用 `CommitAuditor` Agent。本 Skill 与之必须都跑，层级不同。
- 想**回写追溯矩阵**：用 `traceability-linker`。本 Skill 只检查"是否回写"，不替你回写。
- 想**核对清单**：用 `phase-gate-runner`。
- 想**做 doc gardening**（标记过时文档）：用 `DocGardener` Agent，不是本 Skill。
