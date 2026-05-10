---
name: interactive-form-builder
description: 把 Agent 在 chat 里向用户拿信息的"打字反问"统一改造成 picker（`ask.user`）。当 Agent 即将向用户问 status / stage / 评审人 / 决策日期 / 卡点等级 / 候选答 A/B/C / 发布范围 / git tag / commit 区间 / 文件路径 / 是否破坏性变更 / 越界处置 / OQ 关闭三件套 等任何**封闭枚举或半结构化字段**时，本 Skill 给出统一的检测命令、`options[]` 模板与合并规则——避免对话疲劳，避免让用户从零打字。任何 Agent 调用 `ask.user` 之前都应该先查本 Skill 的范本，再决定是不是把多个字段并到一次调用。
when_to_use: |
  - Agent 准备问"status 翻成什么 / 评审结论是哪个 / 决策日期写哪天 / 哪些候选答" 等封闭枚举
  - Agent 在收集 commit metadata（Design / Tests / Verify / Docs / Risk / Task 6 字段）
  - Agent 在收 review-record 抬头（项目 / 阶段 / 对象 / 时间 / 主持人 / 记录人 / 参与人）
  - Agent 在关闭 open-questions 的 OQ-NNN（回答 / 决策日期 / 决策人）
  - Agent 在 H6 收集发布范围（git tag / commit 区间 / 日期段）
  - Agent 拟写一条"请输入 XXX（A / B / C）"——这就是反模式信号
when_not_to_use: |
  - 字段是真正的自由 prose（业务诉求复述 / 设计理由 / 风险描述 / 不做范围说明）→ 保持 chat 文本反问
  - 字段已经在文件里以 `> **[ 待填 ]**：...` 形式标好（人离线填）→ 走 io-contracts §7，不要在 chat 里再问一次
---

# Skill: 交互式表单构造器（Interactive Form Builder）

## 1. 目的与原理

`ask.user` 工具能力有两种用法：

| 用法 | 形态 | 体验 |
| --- | --- | --- |
| ❌ "请回复 A 或 B" | 自由文本通道 | 用户必须打字、容易写错、不能多选 |
| ✅ `ask.user` + `options[]` | picker | 一键选、可多选、可带默认值 |

[`io-contracts.md` §6.1](../../_shared/io-contracts.md#61-交互式输入约定pick-over-type) 已硬性规定：封闭枚举 / 半结构化 / 预枚举候选答**必须**用 picker。本 Skill 把那条规则变成可执行的范本——给定一个收集场景，直接给出：

- **检测命令**：先猜默认值（`git config user.name` / 今天日期 / 仓库 tag 等）
- **`options[]` 模板**：检测值 + 历史值 + 自由输入兜底
- **合并规则**：哪几个字段必须并到一次 `ask.user` 调用，避免连击 N 个对话框

## 2. 输入

接受任意"我即将问用户的字段清单"。也接受半成品的 picker 调用（已经写了 options 但 options 没用检测命令打底）。

## 3. 标准范本

### 3.1 评审记录抬头（一次问 7 项）

应用场景：`/log-review` 或任意 H1–H6 阶段评审完，要写 `docs/07-reviews/YYYY-MM-DD-*.md` 抬头。

检测命令：

```bash
# 项目名（默认值）
basename "$PWD"

# 评审人候选
git config user.name
git log --format='%an' | sort -u | head -10

# 今天 / 昨天
date +%Y-%m-%d
date -v-1d +%Y-%m-%d   # macOS；linux 用 date -d 'yesterday' +%Y-%m-%d
```

`ask.user` 调用结构（一次调用，7 个 questions）：

```yaml
- header: project
  question: 项目名称
  options: [<basename>, <feature 名>, 自由输入]
- header: stage
  question: 评审阶段
  options: [H1, H2, H3, H4, H5, H6]
- header: target
  question: 评审对象
  options: [<当前阶段事实源文件清单 from file_search>, 自由输入]
  multiSelect: true
- header: time
  question: 评审时间
  options: [<今天 YYYY-MM-DD>, <昨天 YYYY-MM-DD>, 自由输入]
- header: location
  question: 评审地点
  options: [线上, 现场, 异步, 自由输入]
- header: chair
  question: 主持人
  options: [<git config user.name>, <历史 reviewer...>, 自由输入]
- header: scribe
  question: 记录人
  options: [<git config user.name>, <历史 reviewer...>, 自由输入]
```

### 3.2 OQ-NNN 关闭（一次问 3 项）

应用场景：`open-questions.md` 的某条 OQ 用户给出答案后，对应阶段的 Agent 在评审中回写"回答 / 决策日期 / 决策人"。

`ask.user` 调用结构：

```yaml
- header: answer
  question: OQ-{NNN} 的回答
  options: [A. <候选A 摘要>, B. <候选B 摘要>, C. <候选C 摘要>, D. 自定义]
- header: date
  question: 决策日期
  options: [<今天>, <昨天>, 自由输入]
- header: decider
  question: 决策人
  options: [<git config user.name>, <历史 reviewer...>, 自由输入]
```

### 3.3 Commit metadata 收集（一次问 6 项）

应用场景：`H5-CodingExecutor` 跑完 Verify、要起草 commit message。

检测命令：

```bash
# 当前任务的 Design / Tests / Task 编号 — 从 ai-task-brief.md 抽
grep -E '^- (设计编号|测试用例编号|任务编号)' docs/06-tasks/<TASK>.md

# 实际跑过的 Verify 命令 — 从会话上下文取
```

`ask.user` 调用结构：

```yaml
- header: design
  question: Design 引用编号
  options: [<from ai-task-brief HD-/API-/DB->, 自由输入]
  multiSelect: true
- header: tests
  question: Tests 引用编号
  options: [<from ai-task-brief TC->, 自由输入]
  multiSelect: true
- header: verify
  question: Verify 命令
  options: [<会话中实际跑过的命令>, 自由输入]
- header: docs
  question: Docs 状态
  options: [updated, not needed]
- header: risk
  question: Risk 评估
  options: [none, <Agent 检测到的风险一句话>, 自由输入]
- header: task
  question: Task 编号
  options: [<from ai-task-brief>, 自由输入]
```

### 3.4 Status 翻转（一次问 2–5 项）

应用场景：评审通过后人工把 frontmatter `status: draft` 改成 `reviewed`。**注意**：按 [io-contracts.md 第 7 节](../../_shared/io-contracts.md#7-人工输入位约定human-input)，Agent 自身**禁止**直接修改 `status` 字段；这个范本面向"人工辅助工具"或 `/log-review` 流程把答案写到评审纪要里。

```yaml
- header: target_file
  question: 要翻转 status 的文件
  options: [<docs/0[1-6]-*/*.md from file_search 命中 status: draft 的>, 自由输入]
- header: new_status
  question: 新 status
  options: [reviewed, approved, deprecated, needs-revision]
- header: reviewer
  question: 评审人
  options: [<git config user.name>, <历史 reviewer...>, 自由输入]
- header: decision
  question: 评审决议
  options: [approved, approved-with-changes, rejected]
- header: date
  question: 评审日期
  options: [<今天>, <昨天>, 自由输入]
```

### 3.5 H6 发布范围（一次问 1–3 项）

应用场景：`H6-ReleaseNoteWriter` 的第一步。

检测命令：

```bash
git tag --sort=-creatordate | head -5
git log --oneline -20
cat VERSION 2>/dev/null
```

```yaml
- header: range_type
  question: 发布范围类型
  options: [git tag 区间, commit 区间, 日期段]
- header: from
  question: 起点
  options: [<最近 5 个 tag>, <最近 20 个 commit short hash>, 自由输入]
- header: to
  question: 终点
  options: [HEAD, <tag 列表>, <commit 列表>, 自由输入]
```

### 3.6 H1-PrototypeReviewer 评审签字（一次问 5 项）

应用场景：`H1-PrototypeReviewer` 跑完 12 条机械化核对、起草完 `docs/02-prototype/prototype-review.md`（`status: draft`）后，**必须**通过 picker 收人工签字回写到第 5 节。

> **关键：决议字段无 default、无 recommended**——AI 不替人下决心。这是 [io-contracts.md §6.1](../../_shared/io-contracts.md#61-交互式输入约定pick-over-type) 反模式列表里"对评审决议 / 阶段门通过与否 / status 翻转等'人工兜底'字段预填默认值或设 `recommended`"那条的实例。

检测命令：

```bash
git config user.name                                   # 主审人候选
git log --format='%an' | sort -u | head -10            # 历史 reviewer
date +%Y-%m-%d                                          # 今天
date -v-1d +%Y-%m-%d                                    # 昨天（macOS）
```

`ask.user` 调用结构（一次调用，5 个 questions）：

```yaml
- header: decision
  question: H1 原型评审决议
  # 关键：无 default、无 recommended，AI 不替人下决心
  options:
    - Approved                # 全 PASS（仅第 12 条因评审记录刚生成处于 UNKNOWN）
    - Approved with Changes   # 有可在 §5 修改项里跟进的小问题
    - Rejected                # 有 FAIL，需返工后重评
    - Pending                 # 有 UNKNOWN（不止第 12 条），需补信息后重评
- header: chair
  question: 主审人
  options: [<git config user.name>, <历史 reviewer...>, 自由输入]
- header: date
  question: 评审日期
  options: [<今天>, <昨天>, 自由输入]
- header: overrides
  question: 要 override 的 Agent 结论（人工对 §2 表格中 Agent 给出 PASS 的项有不同意见时勾选）
  multiSelect: true
  options:
    - 不 override
    - <第 2 节 PASS 项 1>
    - <第 2 节 PASS 项 2>
    - <...>
- header: modifications
  question: 修改项 / 后续动作（自由 prose；无修改写"无"）
  # 自由文本，不用 options
```

回写到 `docs/02-prototype/prototype-review.md` 第 5 节时，把 `> **[ 待填 ]**：...` 占位行**整行替换**为人工选定的内容；frontmatter `status` **保持 `draft`**——`draft → reviewed` 由人工在签字确认后另行翻转，参见 [io-contracts.md 第 7 节](../../_shared/io-contracts.md#7-人工输入位约定human-input)。

## 4. 合并规则（避免对话疲劳）

属于同一逻辑动作的多个字段，**必须**并到一次 `ask.user` 调用：

| 逻辑动作 | 字段数 | 反模式（拒绝） |
| --- | --- | --- |
| 评审记录抬头 | ≤ 7 | 起 7 次 ask.user 把项目 / 阶段 / 对象 / 时间 / 地点 / 主持人 / 记录人各问一次 |
| OQ 关闭 | 3 | 先问"答案是什么"、答完再问"决策日期"、再问"决策人" |
| Commit metadata | 6 | 一字段一次 picker，连续 6 个对话框 |
| Status 翻转 | 5 | 拆成"先翻 status / 再加 reviewer / 再加 date" |

> 单次 `ask.user` 总问题数受 [io-contracts.md §9](../../_shared/io-contracts.md#9-循环与迭代上限) "单次问答 5 个问题" 约束。超过时拆成两次调用，但**禁止**把同一逻辑动作的强相关字段拆开（如 OQ 的"答案 + 日期 + 决策人"必须并）。

## 5. 检测命令清单（默认值的来源）

| 字段 | 检测命令 |
| --- | --- |
| 当前用户 | `git config user.name` |
| 历史评审人 | `git log --format='%an' \| sort -u \| head -10` |
| 今天 / 昨天 | `date +%Y-%m-%d` / `date -v-1d +%Y-%m-%d`（macOS） |
| 仓库 tag | `git tag --sort=-creatordate \| head -5` |
| 最近 commit | `git log --oneline -20` |
| 项目版本 | `cat VERSION 2>/dev/null` |
| 文件路径 | `file_search` 或 `read.search.text` |
| frontmatter id 候选 | `grep -rh '^id:' docs/0X-*/*.md \| sort -u` |
| 任务下一个序号 | `ls docs/06-tasks/TASK-$(date +%Y-%m-%d)-*.md 2>/dev/null \| wc -l` 然后 +1 |

## 6. 反例（出现即拒绝）

```text
❌ "请回复 A 或 B 或 C，告诉我你的选择。"
   → 改成 ask.user options=[A, B, C]

❌ "请输入 status（draft / reviewed / approved）："
   → 改成 ask.user options=[reviewed, approved, deprecated]，且 Agent 自己不应改 status，按 §7 规则交人工

❌ ask.user × 7 串行（项目 / 阶段 / 对象 / 时间 / 地点 / 主持人 / 记录人）
   → 合并成一次 ask.user，questions[] 7 项

❌ "请告诉我评审日期、评审人、评审决议（每行一个）"
   → 改成 ask.user × 1，questions[] 3 项，每项 options=[检测值, 自由输入]

❌ ask.user 让用户复述"业务核心诉求"
   → prose 字段，保持 chat 文本反问；picker 装不下长说明
```

## 7. 与其它 Skill / Agent 的边界

- 想**校验**一个 Agent 的反问范式合不合规：本 Skill 给检查表
- 想**起草任务卡**：用 `ai-task-brief-writer`（它会按本 Skill 的范本把"任务编号 / 上游路径 / 设计引用"等改成 picker）
- 想**写一条提交信息**：用 `commit-message-formatter`（它会按 §3.3 的 6 字段范本一次问完）
- 想**关闭 OQ**：对应阶段的 Agent（H1 → RequirementsInterviewer / UISpecAuthor，H2 → ArchitectAdvisor）按 §3.2 范本调用本 Skill
