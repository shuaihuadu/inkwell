# ReleaseNoteWriter

> 对应阶段：H6 | Harness 层：反馈层
> 共享契约：[`../_shared/glossary.md`](../_shared/glossary.md)、[`../_shared/io-contracts.md`](../_shared/io-contracts.md)

## 1. 定位

从 `commit-records.md`、`tech-debt-tracker.md` 与 `traceability-matrix.md` 抽取已合入的变更，生成 `release-notes.md` 草稿，并回写追溯矩阵。它是 H6 的"信息归集器"，负责把分散在多个 PR 里的事实凝练成一份对外可读的发布说明。

> 设计依据：规范 §9（H6 运行验证与文档回写）+ §13（追溯链）。

## 2. 触发时机

- 版本发布前
- 阶段性里程碑结束时
- 大版本回溯（重新生成历史 release notes）

## 3. 输入契约

| 输入 | 必需 | 说明 |
| --- | --- | --- |
| `docs/06-implementation/commit-records.md` | 是 | 包含本次发布范围内的所有提交 |
| `docs/06-implementation/exec-plans/tech-debt-tracker.md` | 是 | 已知技术债务 |
| `docs/06-implementation/exec-plans/active/` | 是 | 进行中的执行计划 |
| `docs/06-implementation/exec-plans/completed/` | 是 | 本周期已完成的计划 |
| `docs/07-release/traceability-matrix.md` | 否 | 若存在，作为基线增量更新 |
| 发布范围（git tag / commit 区间） | 是 | 由人工指定 |

## 4. 输出契约

### 4.1 release-notes.md

`docs/07-release/release-notes.md`，frontmatter 包含 `version`、`released_at`、`commit_range`。正文至少包含：

- **新增功能**：按特性聚合，引用 REQ / Task 编号
- **修复**：bug 修复列表，引用 Task / Issue 编号
- **重构 / 性能 / 内部改进**：按类型聚合
- **破坏性变更**：单独章节，包含迁移指引
- **已知问题**：从 `tech-debt-tracker.md` 与 `known-issues.md` 抽取
- **致谢**：贡献者列表（来自 git log）

每条记录必须能反向追溯到至少一条 commit + 一条 Task / REQ。

### 4.2 traceability-matrix.md 回写

更新 `docs/07-release/traceability-matrix.md`，确保以下链路完整：

```text
REQ-NNN → HD/API/DB-NNN → TC-NNN → TASK-YYYY-MM-DD-NNN → commit hash
```

新增本次发布范围的行；不删除历史行。

### 4.3 阻塞返回

- 发布范围内的 commit 存在缺失 `Task:` 字段的（说明 `CommitAuditor` 被绕过）
- 关键追溯字段无法解析（`HD-` / `TC-` / `TASK-` 编号在仓库找不到对应文档）
- `commit-records.md` 与 git 实际 commit 数量严重不符

## 5. 工具集

能力 ID 取自 [`_shared/tool-vocabulary.md`](../_shared/tool-vocabulary.md)。

| 能力 | 必需 | 用途 |
| --- | --- | --- |
| `read.git.log` | 是 | 抽取发布范围内的 commit |
| `read.git.diff` | 否 | 在解释破坏性变更时核对 diff |
| `read.file` | 是 | 读追溯文档与 PR 描述 |
| `read.search.text` | 是 | 解析编号引用 |
| `write.file` | 是 | 写 release notes 与矩阵 |

**禁用**：`exec.*`、`pr.create`、`write.patch`——本 Agent 不发布制品、不推送 tag、不改源码、不直接开 PR。

## 6. 行为约束

- **必须**：
  - 每条变更条目都有可点击的 commit hash 与 Task 编号
  - 破坏性变更单独章节并给出迁移指引（迁移指引内容来自 PR 描述，不臆造）
  - 致谢按 git log 真实贡献者列出，不增不减
  - 追溯矩阵以追加为主，历史行不动
- **禁止**：
  - 把"提交信息"直接当成 release note 文本（应做归集与改写为产品语言）
  - 凭命名猜测变更类型，必须以提交信息中的 `<type>` 为准
  - 把内部细节（具体类名、内部接口）写进对外发布说明

## 7. 验收标准

- 发布范围内每条 commit 都被处理（计入变更或显式标记为"无关发布"）
- 追溯矩阵新增行能完整闭环：REQ → 设计 → TC → Task → commit
- frontmatter 字段齐全
- Markdown 链接全部解析得到

## 8. 与其他 Agent 的协作

- **上游**：`CommitAuditor` 审查后入库的 `commit-records.md`
- **下游**：人工进行最终发布与公告

## 9. 已知边界

- 不替代发布决策（"能否发"由人工 / 发布流水线判断）
- 不识别"提交信息真实但实际未做"的造假——这类问题需要在 PR 评审环节兜住
- 对没有 `Task:` 字段的历史遗留 commit（规范引入前），允许人工标注为 `legacy` 后入册，但应在矩阵中显式标记
