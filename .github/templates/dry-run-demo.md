---
template: dry-run-demo
purpose: 用一个虚拟最小需求把 H1–H6 全流程走一遍，验证 Harness 在本仓库内跑得通
copy-to: docs/00-dry-run/dry-run-demo.md
delete-after: 第一个真实需求落地后可删除
---

# Dry Run · 空跑演练

> **为什么要 dry run**：Harness 的规则、Agent、Slash 命令都得在你自己的项目环境里"跑过一次"才能确认没问题。文章 1 的作者在拿真实需求前用一个虚拟需求空跑，发现了 4 个会让真实需求严重返工的缺陷（CI 门禁只看状态码、评审报告未生成、摘要文件被追加重复行、部署参数被臆测）。这次空跑就是为了避免你也踩同一类坑。
>
> **预期投入**：单人 30–60 分钟。比第一次真实需求踩坑后回退便宜得多。
>
> **运行后**：本目录可整体删除，或保留作为团队 onboarding 教材。

## 1. 选一个虚拟需求

> **[ 待填 ]**：替换成一个**和你项目技术栈匹配的最小需求**。要求：① 单文件、最多两个文件能改完；② 不依赖外部服务；③ 有可验证的产出。

参考候选（任选其一或自拟）：

- Web 后端项目：在主程序加一个 `/healthz` 端点，返回 `{"status":"ok","time":"..."}`，写一个单测验证字段
- CLI 工具项目：加一个 `--version` 参数，输出当前版本号，单测验证输出格式
- 文档站项目：在首页加一个"最后更新时间"的角标，单测/快照验证渲染产物
- 库 / SDK 项目：暴露一个 `getLibraryVersion()` 函数，覆盖单测

## 2. 各阶段验证清单

每个阶段只有一个目的：**走通流程**，不在乎产物质量。所有产物可以在演练后整体删除。

### H1 · 需求

- [ ] 在 Copilot Chat 输入 `/new-task` 或调用 `h1-requirements-interviewer`，让 Agent 反问澄清
- [ ] Agent 是否反问而不是直接编造？反问问题数 **不超过 5 个**？（io-contracts 第 9 节）
- [ ] 产出 `docs/01-requirements/requirements.md`，frontmatter 含 `id / stage / status: draft`
- [ ] 把所有 `> **[ 待填 ]**：...` 整行替换成实际答案；签字位填自己的名字 + 当天日期
- [ ] 把 `status: draft` 改 `reviewed`

**预期发现的问题**（参考）：Agent 是否会跳过反问直接补全？AGENTS.md 第 1/4 节有没有签字位？签字位是不是 `[ 待填 ]` 占位？

### H2 · 架构

- [ ] 调用 `h2-architect-advisor`
- [ ] 即使是 `/healthz` 这种小需求，也要走完"选择 / 为什么 / 替代方案 / 放弃理由 / 维护影响 / 成本性能安全交付影响"六字段
- [ ] 产出 `docs/03-architecture/architecture.md` 与 `tech-selection.md`
- [ ] `risk-analysis.md` 的"残余风险接受"列必须有人工签字（无人签 → 不进 H3）

**预期发现的问题**：Agent 是否会因为需求小就跳过六字段？签字位有没有？

### H3 · 详细设计

- [ ] 调用 `h3-detailed-design-reviewer`
- [ ] 对要改的 1–2 个文件做"文件级设计"（路径 / 职责 / 输入 / 输出 / 错误 / 日志 / 测试）
- [ ] 产出 `docs/04-detailed-design/HD-001.md`

### H4 · 测试用例

- [ ] 调用 `h4-test-case-author`
- [ ] 给虚拟需求至少写 1 条 `TC-001`，覆盖正常路径
- [ ] 产出 `docs/05-test-design/test-cases/TC-001.md`，frontmatter 引用 REQ + HD

### H5 · 编码与提交

- [ ] 用 `/new-task` 起任务卡，确认 `允许修改的文件` / `禁止修改的文件` / `验收命令` 都齐
- [ ] 调用 `h5-coding-executor` 完成实现，跑通 `Verify` 命令
- [ ] 调用 `h5-commit-auditor` 校验六字段（Design / Tests / Verify / Docs / Risk / Task）
- [ ] 提交一次 commit，验证 commit message 不被自动审计拒绝

**预期发现的问题**：CI / 测试命令在你的项目里是不是真能跑？六字段是不是真能填齐？

### H6 · 回写

- [ ] 调用 `h6-release-note-writer`
- [ ] 产出 `docs/07-release/release-notes.md` 与 `traceability-matrix.md`
- [ ] 验证从 `REQ-001` 能反向走到 `commit hash`

## 3. 收集发现

把空跑过程中遇到的"骨架问题"记录下来，按文章 1 作者的做法**立刻 patch 回 Harness 本身**，而不是绕过去。

> **[ 待填 ]**：列举本次 dry run 发现的问题及处理方式。每条建议格式：`<现象> → <根因，写在哪个文件第几节> → <修复或回写到哪里>`。

示例：

- CI 门禁只检查 `status == SUCCESS` 但忽略 `total_tests > 0`，导致空测试套件也能通过 → 已在 `docs/05-test-design/test-plan.md` 增加"测试用例数 > 0"门禁条件 → 同步到 `task-board.md`
- 评审 Agent 在简单需求下不生成评审记录文件 → 在 `agents/h3-detailed-design-reviewer/AGENT.md` 第 X 节加硬约束"无论需求大小都生成 review-record.md" → 提了一个 issue 给 harness-engineering 上游

## 4. 完成

- [ ] 上述清单全部勾完
- [ ] 第 3 节问题已 patch 回相应 Agent / 文档
- [ ] 决定本目录的去留（删除 / 保留作 onboarding 教材）

完成后，可以放心拿真实需求开第一条 H1 任务了。
