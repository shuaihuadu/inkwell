# Harness Engineering · 操作手册（HANDBOOK）

**10 分钟读完即可上手。** 涵盖：每个目录放了什么、什么时候切哪个 Agent、什么时候打哪条 `/` 命令、模板怎么改、要卸载或升级怎么办。

---

## 目录

1. [装完后该干啥](#1-装完后该干啥按项目状态分流)
2. [全流程一览：H1 → H6 + Hx](#2-全流程一览h1--h6--hx)
3. [`.github/` 里都装了什么](#3-github-里都装了什么)
4. [`.harness-engineering/` 里都装了什么](#4-harness-engineering-里都装了什么)
5. [Templates 怎么用、怎么改](#5-templates-怎么用怎么改)
6. [Skills / Prompts / Agents 速查](#6-skills--prompts--agents-速查)
7. [给 Agent / Prompt 配置工具白名单](#7-给-agent--prompt-配置工具白名单)
8. [常见问题 / 排查 / 升级 / 卸载](#8-常见问题--排查--升级--卸载)

---

## 1. 装完后该干啥

先看你属于哪种情况，再决定从哪进门：

| 项目状态                                                      | 入口                                                    |
| ------------------------------------------------------------- | ------------------------------------------------------- |
| **全新空仓 / 项目还没启动 / 还没有任何 `REQ-NNN`**            | 走 [1.1 节：全新项目从 H1 起步](#11-全新项目从-h1-起步) |
| **老仓改造 / 给已有项目加一个具体小功能（已有 REQ/HD 凭证）** | 走 [1.2 节：已有项目从 H5 起跳](#12-已有项目从-h5-起跳) |

> 关键概念：`/new-task` 是 **H5（编码）的入口，不是项目的入口**。它把"已有 REQ/HD"切成可执行的代码改动；空仓没有 REQ 可指，它会反问 / 阻塞返回。**别用 `/new-task` 起新项目**——切 Agent 才是。

### 1.1 全新项目从 H1 起步

依次切 Agent 跑下去，每一步的产出会成为下一步的输入；不要跳级，跳级会让追溯链断在你身上。

1. **H1 上半段 · 需求文本**：Copilot Chat 输入框下方的 Agent 下拉切到 `h1-requirements-interviewer`，用一段大白话描述目标用户、核心场景、必做与可选——它会反问、追问、把回答落成 `docs/01-requirements/requirements.md` 草稿，分配 `REQ-001`、`REQ-002`…，没答清的进 `open-questions.md`，**不会自动用 `<TBD>` 占位**。

   _示例输入_（粗暴说目标，把模糊点交给它反问）：

   ```text
   做一个 AI 内容工厂：
   - 目标用户：个人技术博客作者
   - 核心场景：输入题目 → AI 多轮迭代 → 输出可发布的 markdown
   - 必做：本地运行 + 多 LLM 厂商可切换
   - 可选：直接发布到微信公众号
   先把模糊点反问出来，再起草 requirements.md。
   ```

2. **H1 下半段 · UI 说明 + 原型 + 评审 + 留档**：H1 不是只写 `requirements.md` 就结束了。完整 H1 还包含「UI 说明、可交互原型、评审、留档」四件事，按顺序切两个专属 Agent + 一个外部工具走完：
   - **UI 说明**：切到 `h1-ui-spec-author`，给它 `requirements.md` + 你手头的截图或参考页面，它会按 [stages.md 第 4.5 节](../../docs/stages.md#45-ui-说明必须包含) 那 10 项反问一轮，然后产出 `docs/01-requirements/ui-spec.md` / `user-flow.md` / `acceptance-criteria.md`，没答清的**追加**到同一份 `open-questions.md`。

     _示例输入_（明确上游凭证 + 手头素材 + 推不清走 open-questions）：

     ```text
     上游：docs/01-requirements/requirements.md 里 REQ-001~REQ-005。
     手头素材：prototypes/ai-content-factory/screenshots/draft-{1,2,3}.png
     任务：按 stages.md 4.5 节 10 项反问一轮，
     产出 ui-spec.md / user-flow.md / acceptance-criteria.md；
     你不能推出的项追加到 open-questions.md，不要用 <TBD> 占位。
     ```

   - **可交互原型**：你自己挑工具做（HTML/CSS 静态页面、Figma 导出、V0、Lovable、手绘扫描都行），落到 `prototypes/<feature>/` 目录，关键屏幕被截图在 `prototypes/<feature>/screenshots/` 下。
   - **原型评审**：切到 `h1-prototype-reviewer`，它会只读 `ui-spec.md` + `prototypes/<feature>/` + `phase-gate-checklist.md`，按 H1 那 12 条逼出 `PASS / FAIL / UNKNOWN`与补救动作；**它只读不写，不会替你产出 `prototype-review.md`**（评审纪要由人写，避免 AI 给自己开绿灯）。

     _示例输入_（上游证据、评审目标、打分口径，三件说清就行）：

     ```text
     评审目标：prototypes/ai-content-factory/
     上游证据：
     - docs/01-requirements/ui-spec.md
     - docs/01-requirements/user-flow.md
     - docs/01-requirements/acceptance-criteria.md
     打分口径：.github/templates/phase-gate-checklist.md 里 H1 那 12 条。
     逐项给 PASS / FAIL / UNKNOWN + 补救动作；不写任何文件，输出在聊天里。
     ```

   - **纪要留档**：拿上一步的 PASS/FAIL 报告作为评审纪要起点，补充你的调整后请人评审一轮，走 `/log-review` 落到 `docs/07-reviews/YYYY-MM-DD-h1-review.md`，同时把评审结论摘要回写到 `docs/02-prototype/prototype-review.md`（这份是 H2 架构选型的输入凭证之一，不能省）。

     _示例输入_（叫出 `/log-review`，主题 + 参与者 + 结论摘要三者齐）：

     ```text
     /log-review h1-review-2026-05-06
     参与者：产品 / 设计 / 后端 / 前端
     结论摘要：12 条门禁通过 9 条，
     待补 3 条（页面状态 / 错误提示 / 权限差异），
     由 X 负责本周内补齐后重跑 /run-gate H1。
     ```

3. **跑一次 `/run-gate H1`**：在 Copilot Chat 输入 `/run-gate`，它会按上面那 12 条机械核对，给出 PASS / FAIL / UNKNOWN。**只有全 PASS 才能进 H2**——这是设计上的硬卡口，绕过去后面的 commit 审计会让你在 H5 阶段重新偿还。

   _示例输入_（`/` 后面接阶段号，不需要额外参数）：

   ```text
   /run-gate H1
   ```

4. **（可选）H1 影响图**：切 `h1-repo-impact-mapper`。全新空仓基本是全部新建，可跳过；老仓改造时它会列出受影响的模块 / 文件 / 接口 / 测试。

   _示例输入_（老仓改造场景，强调不凭命名臆造）：

   ```text
   上游：docs/01-requirements/requirements.md REQ-001~REQ-005。
   列出受影响的模块 / 文件 / 接口 / 测试，
   每条标置信度（高 / 中 / 低）；
   查不到的不要凭命名臆造，直接标 UNKNOWN，
   结果落到 docs/01-requirements/repo-impact-map.md。
   ```

5. **H2 架构 / ADR**：切 `h2-architect-advisor`。它基于上一步的 requirements + ui-spec 给一份初版架构（项目划分、技术栈、依赖关系）+ 关键 `ADR-NNN`（每条含"选择 / 为什么 / 替代 / 放弃理由 / 维护成本 / 性能-安全-交付影响"六字段）。这一步决定源码树长什么样、用什么栈。

   _示例输入_（上游凭证 + 硬约束，让 ADR 有边界）：

   ```text
   上游：docs/01-requirements/{requirements.md, ui-spec.md}。
   硬约束：.NET 8、单仓多项目（src/core/* + src/app/*）、
   本地优先（不强依赖云）、可选 Docker。
   交付：
   - 初版架构说明（项目划分 / 技术栈 / 依赖关系）
   - 3~5 条关键 ADR-NNN，每条六字段：
     选择 / 为什么 / 替代 / 放弃理由 / 维护成本 / 影响
   ```

6. **H3 详细设计**：人手起草 `docs/04-detailed-design/<feature>/HD-NNN.md`（接口、数据模型、错误码、并发与失败语义）。写完切 `h3-design-reviewer` 让它逐项核对完备性，挡住"设计还没写清"流入下一阶段。

   _示例输入_（只评审不修改，明确口径 + 期望交付）：

   ```text
   评审 docs/04-detailed-design/ai-content-factory/HD-001.md。
   口径：stages.md 第 6 节那份章节列表（接口 / 数据模型 / 错误码 /
   并发与失败语义 / 可观测性 / 发布与回滚）。
   逐项给 PASS/FAIL，缺项列出 + 下一步行动；本次只评审，不修改文档。
   ```

7. **H4 测试用例**：切 `h4-test-case-author`。它从 REQ + HD 反推 `docs/05-test-design/test-cases.md`（每条 `TC-NNN`），保证每个 `REQ-NNN` 都有至少一条机械可判断的覆盖。

   _示例输入_（上游 + 覆盖下限 + 分组约束）：

   ```text
   上游：REQ-001~REQ-005 + HD-001~HD-003。
   反推到 docs/05-test-design/test-cases.md：
   - 每条 REQ 至少一条 TC-NNN 覆盖
   - 每条 TC 必须可机械判断（命令 / 期望输出 / 失败标准）
   分组：契约测试 / 集成测试 / E2E 关键流。
   ```

8. **H5 起任务 → 编码 → 审提交**：上游凭证齐全后，就可以走 [1.2 节](#12-已有项目从-h5-起跳) 那四步把每条任务跑完。
9. **H6 发版说明**：版本切出来时切 `h6-release-note-writer`，从 commit 抽取生成 `docs/07-release/release-notes.md`，回写追溯矩阵。

   _示例输入_（版本 + commit 范围 + 破坏性变更隔离要求）：

   ```text
   版本：v0.2.0
   commit 范围：afa72c7..HEAD
   产出 docs/08-releases/v0.2.0.md：
   - 特性 / 修复 / 文档 / 重构 分类
   - 破坏性变更单独章节，每条给迁移指引
   - 同步回写追溯矩阵（REQ ↔ HD ↔ TC ↔ Task ↔ Commit）
   ```

> 第 1+2 步产出的 `requirements.md` / `ui-spec.md` / `acceptance-criteria.md` 是后面所有阶段的"上游凭证"——commit message 里的 `Design: REQ-001` / `Tests: TC-NNN` / `Task: TASK-NNN` 都是顺着它们往下挂的。**没有这两步，提交格式校验会一路把你打回来**。

### 1.2 已有项目从 H5 起跳

仓库已经有 `docs/01-requirements/requirements.md`（或等价的需求凭证），这次只想加一个具体的小功能，跟着这四步把最小闭环跑一遍：

1. **起一个最小任务**：在 Copilot Chat 输入 `/new-task` 加你想做的小事；首次运行它会按模板自动建 `docs/06-tasks/task-board.md`，并起草 `docs/06-tasks/T-001-xxx.md`、同时登记一行到看板。

   _示例输入_（一句话说清要做什么 + 上游凭证号）：

   ```text
   /new-task 给 ChatHistoryService 增加 SQL Server 持久化实现，
   对应 REQ-007 + HD-012；先给我任务草稿 + 板上登记，暂不动代码。
   ```

2. **人工审任务说明**：核对 `允许修改的文件` 与 `Verify 命令` 是否合理；OK 之后把 `docs/06-tasks/task-board.md` 里这一行的 `status` 改成 `ready`。
3. **切到 `H5-CodingExecutor`**：在 Copilot Chat 输入框下方的 Agent 下拉里选它，让它按任务说明执行。

   _示例输入_（明确任务卡 + 不越界 + 每改必验）：

   ```text
   按 docs/06-tasks/T-007-sqlserver-chat-history.md 执行：
   - 只改任务卡里“允许修改的文件”列出的范围；
   - 每改完一处跑 Verify：dotnet test；
   - 超出范围或 Verify 失败时阻塞返回，不要自作主张扩大改动。
   ```

4. **提交前切到 `H5-CommitAuditor`**：让它逐字段校验 commit message（Design / Tests / Verify / Docs / Risk / Task）。

   _示例输入_（说清本次 commit 范围 + 期望被否决的下限）：

   ```text
   准备提交：HEAD 这次改动覆盖 T-007（SQL Server 聊天历史）。
   校验 commit message 的六字段：
   Design / Tests / Verify / Docs / Risk / Task。
   不合格直接拒绝，告诉我缺哪条、怎么补；不要代笔编造编号。
   ```

> 中小变更允许跳过 H1–H4 直接从 H5 起跳，但底线是：**每个 commit 至少要能映射到一条 `REQ-NNN`**。如果这次改动连 REQ 都对不上，先回 1.1 节第 1 步把 requirements 补齐再来——`H5-CommitAuditor` 不会替你豁免这条。

---

## 2. 全流程一览：H1 → H6 + Hx

```
┌─────────────────────── 一个特性 / 一次发版的生命周期 ───────────────────────┐
│                                                                              │
│  H1 需求文本      → H1-RequirementsInterviewer  → docs/01-requirements/      │
│  H1 UI 说明       → H1-UISpecAuthor             → docs/01-requirements/      │
│  H1 原型评审      → H1-PrototypeReviewer        → 只读 PASS/FAIL（不写文件） │
│                                                   人手回写 docs/02-prototype/│
│                                                   prototype-review.md        │
│  H1 原型实践      → 你自选原型工具              → prototypes/<feature>/      │
│  H1 影响图（可选）→ H1-RepoImpactMapper         → docs/01-requirements/      │
│  H2 架构 / ADR    → H2-ArchitectAdvisor         → docs/03-architecture/      │
│  H3 详细设计评审  → H3-DesignReviewer           → docs/04-detailed-design/   │
│  H4 测试用例      → H4-TestCaseAuthor           → docs/05-test-design/       │
│  H5 起任务        → /new-task                   → docs/06-tasks/             │
│  H5 编码          → H5-CodingExecutor           → 改源码 + Verify            │
│  H5 审提交        → H5-CommitAuditor            → 拒不合格的 commit          │
│  H6 发版说明      → H6-ReleaseNoteWriter        → docs/08-releases/          │
│  Hx 文档腐化巡检  → Hx-DocGardener              → 标 deprecated / 待清理     │
│                                                                              │
│  阶段切换前 → /run-gate 跑 phase-gate-checklist                              │
│  评审落档   → /log-review 把会议纪要归到 docs/07-reviews/                    │
│  对账       → /sync-board 把板和实际 commit 对一遍                           │
└──────────────────────────────────────────────────────────────────────────────┘
```

> H1 是双段：**上半段**（需求文本）由 `H1-RequirementsInterviewer` 主导；**下半段**拆为三个环节：UI 说明由 `H1-UISpecAuthor` 反问产出，中间你用外部工具做 `prototypes/<feature>/` 原型，`H1-PrototypeReviewer` 只读原型 + UI 文档给 PASS/FAIL（评审纪要由人写，避免 AI 自我满足），参见 [1.1 节第 2 步](#11-全新项目从-h1-起步)。`/run-gate H1` 会把两段一起核对，只过上半段不算 H1 完成。

并不强制把 H1 → H6 全走完才能动手——分流规则见 [第 1 节](#1-装完后该干啥按项目状态分流)：全新项目按 1.1 节老老实实从 H1 起步；老仓加小功能按 1.2 节从 H5 起跳，事后补 `requirements.md` 链路。

---

## 3. `.github/` 里都装了什么

```
.github/
├── copilot-instructions.md          ← 仓库总指令；何时切哪个 Agent / 用哪个 Prompt
├── instructions/                    ← 文件类型相关的"规则集"，按 applyTo 自动加载
│   ├── coding-style.instructions.md
│   ├── commit-format.instructions.md
│   └── docs-style.instructions.md
├── agents/                          ← 11 个 Custom Agent，下拉菜单可选
│   ├── h1-repo-impact-mapper.agent.md
│   ├── h1-requirements-interviewer.agent.md
│   ├── h1-ui-spec-author.agent.md
│   ├── h1-prototype-reviewer.agent.md
│   ├── h2-architect-advisor.agent.md
│   ├── h3-design-reviewer.agent.md
│   ├── h4-test-case-author.agent.md
│   ├── h5-coding-executor.agent.md
│   ├── h5-commit-auditor.agent.md
│   ├── h6-release-note-writer.agent.md
│   └── hx-doc-gardener.agent.md
├── skills/                          ← 4 个 Skill，Copilot 按 description 自动调
│   ├── ai-task-brief-writer/SKILL.md
│   ├── commit-message-formatter/SKILL.md
│   ├── phase-gate-runner/SKILL.md
│   └── traceability-linker/SKILL.md
├── prompts/                         ← 4 个 Slash Command
│   ├── new-task.prompt.md
│   ├── run-gate.prompt.md
│   ├── log-review.prompt.md
│   └── sync-board.prompt.md
└── templates/                       ← 4 个产物模板，AI 与人手共用
    ├── ai-task-brief.md
    ├── phase-gate-checklist.md
    ├── review-record.md
    └── task-board.md
```

**这 27 个文件全部开箱即用，不用再做任何配置：进 Copilot Chat，直接选 Agent / 输 `/` 即可。**

---

## 4. `.harness-engineering/` 里都装了什么

```
.harness-engineering/
├── HANDBOOK.md       ← 你正在读的这份手册
├── README.md         ← 解释这个目录的角色 + .gitignore 建议
├── docs/             ← 设计文档（stages.md / repo-layout.md / tech-debt-gc.md）
├── manifest.json     ← 安装清单，uninstall 用
├── install.log       ← 每次 install/uninstall 追加一行
└── uninstall.ps1     ← 一键反向清理
```

这个目录承担两件事：**随时能查规范文档**（HANDBOOK + docs/），以及**能干净卸载**（manifest + uninstall.ps1）。安装完成后它不需要你再去改——所有自定义都应该发生在 `.github/` 里。

如果觉得它和项目本身无关、不想入版本库，**推荐把它加进 `.gitignore`**：

```gitignore
.harness-engineering/
```

代价：团队其他人 `git pull` 后看不到这份手册，需要自己再跑一次 `install.ps1`。如果想让所有人都能直接读，就保留入版本库。

---

## 5. Templates 怎么用、怎么改

`.github/templates/` 里 4 个文件是**产物的初始骨架**，分两种用法：

### 5.1 直接复制使用

需要在仓库里建一个新文档（比如手工起草一份任务简报）：

```powershell
# 例：从模板初始化任务简报
New-Item -ItemType Directory -Path docs\06-tasks -Force | Out-Null
Copy-Item .github\templates\ai-task-brief.md docs\06-tasks\T-001-<slug>.md
```

然后按里面的注释自己填。

> 任务看板 `task-board.md` 不需要手工复制——`/new-task` 首次运行会自动建到 `docs/06-tasks/task-board.md`。

### 5.2 让 Agent / Prompt 引用

四条 `/` 命令背后都会读模板：

| Slash 命令    | 读取的模板                                                                                  |
| ------------- | ------------------------------------------------------------------------------------------- |
| `/new-task`   | `ai-task-brief.md`；首次运行同时按 `task-board.md` 模板自动建 `docs/06-tasks/task-board.md` |
| `/run-gate`   | `phase-gate-checklist.md`                                                                   |
| `/log-review` | `review-record.md`                                                                          |

所以你改完 `.github/templates/*.md` 之后：

- 直接复制使用的人，下一次复制就拿到新版
- AI 执行 Prompt 时也会读到新版
- **不需要重启 VS Code，不需要重跑 `install.ps1`**

### 5.3 推荐的修改方向

| 模板                      | 你应当根据自家情况调整的字段                              |
| ------------------------- | --------------------------------------------------------- |
| `ai-task-brief.md`        | `Verify 命令` 一行：换成你仓库真实的构建/测试命令         |
| `phase-gate-checklist.md` | 各 H 阶段的清单：删掉与你团队无关的项，加上你团队额外要求 |
| `review-record.md`        | `参与者角色`、`脱敏要求`：按公司合规要求改                |
| `task-board.md`           | 列字段：增减你想要的列（如 `priority`、`epic`）           |

### 5.4 不推荐的修改方向

- ❌ 删除 frontmatter 字段——Skills / Prompts 在解析时依赖它们
- ❌ 在 `phase-gate-checklist.md` 里写"AI 必须自动通过"——那 gate 就废了
- ❌ 把模板改成具体某个任务的内容——模板要保持通用

---

## 6. Skills / Prompts / Agents 速查

### Skills（按 description 自动触发）

| Skill                      | 何时被触发                                  |
| -------------------------- | ------------------------------------------- |
| `ai-task-brief-writer`     | 用户说"起一个任务" / "写一份 AI 任务说明"   |
| `commit-message-formatter` | 准备提交 / 校验 commit message 时           |
| `phase-gate-runner`        | 跑阶段门检查时                              |
| `traceability-linker`      | 需要回填 REQ ↔ ADR ↔ Task ↔ Commit 追溯链时 |

### Prompts（用户主动 `/` 触发）

| 命令          | 干什么                                                 |
| ------------- | ------------------------------------------------------ |
| `/new-task`   | 起一个 H5 任务：草稿 + 板上登记，不动代码              |
| `/run-gate`   | 按 phase-gate-checklist 核对当前阶段是否能进下一阶段   |
| `/log-review` | 把会议 / PR 评审誊到 `docs/07-reviews/YYYY-MM-DD-*.md` |
| `/sync-board` | 审计 task-board 与代码 / commit 的对齐，列失同步       |

### Agents（在 Copilot Chat 输入框下方的 Agent 下拉手动切）

| Agent                        | 阶段  | 用途                                                                                        |
| ---------------------------- | ----- | ------------------------------------------------------------------------------------------- |
| `H1-RequirementsInterviewer` | H1    | 反问把模糊需求转成可评审 `requirements.md`                                                  |
| `H1-UISpecAuthor`            | H1    | 反问把 UI 细节逼出，按 stages.md 4.5 节 10 项产出 ui-spec / user-flow / acceptance-criteria |
| `H1-PrototypeReviewer`       | H1    | 只读评审：读原型 + UI 文档，按 phase-gate H1 12 条 PASS/FAIL，不写文件                      |
| `H1-RepoImpactMapper`        | H1↔H3 | 把已 reviewed 需求映射到真实仓库代码                                                        |
| `H2-ArchitectAdvisor`        | H2    | 起草架构选型 + ADR，每条选型留六字段                                                        |
| `H3-DesignReviewer`          | H3    | 评审详细设计是否可进 H4                                                                     |
| `H4-TestCaseAuthor`          | H4    | 从需求与设计反推测试用例矩阵                                                                |
| `H5-CodingExecutor`          | H5    | 严格按 ai-task-brief 执行编码 + Verify                                                      |
| `H5-CommitAuditor`           | H5    | 校验 commit 六字段，不合格拒合并                                                            |
| `H6-ReleaseNoteWriter`       | H6    | 从 commit-records 抽变更生成 release notes                                                  |
| `Hx-DocGardener`             | Hx    | 周期巡检 docs/ 与代码偏离                                                                   |

### 6.1 H1 下半段的两个专属 Agent：UISpecAuthor + PrototypeReviewer

H1 完整定义见 [stages.md 第 4 节](../../docs/stages.md#4-h1需求ui-与交互原型阶段)，包含五件事：**需求文本 / UI 说明 / 用户流 / 可交互原型 / 评审留档**。最初版本只把"需求文本"做成了专属 Agent，下半段统一交给默认 Agent + 外部工具。**这一决策在采用方第一次跑 `/run-gate H1` 时被推翻了**：12 条门禁里下半段那 6 条经常 FAIL，原因是"默认 Agent 不会按 stages.md 4.5 节那 10 项主动反问"——同一组反问纪律已在上半段的 `H1-RequirementsInterviewer` 上证明有效，下半段当然也吃这套。从 v0.0.2 起，H1 下半段拆为两个专属 Agent：

| Agent                  | 性质       | 干什么                                                                                   |
| ---------------------- | ---------- | ---------------------------------------------------------------------------------------- |
| `H1-UISpecAuthor`      | 反问写文档 | 平移 RequirementsInterviewer 的纪律到 UI 维度，按 stages.md 4.5 节 10 项产出三份文档     |
| `H1-PrototypeReviewer` | 只读评审员 | 读原型 + UI 文档，按 phase-gate H1 12 条 PASS/FAIL/UNKNOWN，**不写文件**——评审纪要由人写 |

设计取舍：

- **PrototypeReviewer 为什么不能让 Agent 写评审纪要**：评审 Agent 容易自我满足（参见 [run-gate 设计](#73-两个只读例外run-gate-与-h1-prototype-reviewer)）。把它限制成"只读 + 不能写 prototype-review.md"，复用 run-gate 的同一招——用工具集物理隔离取代行为约束，让 AI 评审与人评审之间留出独立空间。
- **可交互原型本身仍由你自选工具实现**：HTML/CSS、Figma、V0、Lovable、手绘扫描都行。`H1-UISpecAuthor` 写 ui-spec markdown，`H1-PrototypeReviewer` 读原型目录里的 markdown / 截图，原型工具的选择被严格隔离在两个 Agent 之外。
- **v1 边界**：`H1-PrototypeReviewer` 当前只读 markdown 描述与本地截图。要让 Agent 真的去渲染 React / 点击按钮 / 截图比对，是 v2 的事——届时给它开 `browser/*`。

实操上，H1 下半段的工作流是：

```
H1-UISpecAuthor (反问 + 写 ui-spec.md / user-flow.md / acceptance-criteria.md)
    ↓
外部工具 (做 prototypes/<feature>/ 可交互原型，关键截图归档到 screenshots/)
    ↓
H1-PrototypeReviewer (只读评审：12 条 PASS/FAIL/UNKNOWN，不写文件)
    ↓
人工评审纪要 + /log-review (把纪要落到 docs/07-reviews/)
    ↓
回写 docs/02-prototype/prototype-review.md (由人写)
    ↓
/run-gate H1 (机械核对 12 条做最终复核)
```

---

## 7. 给 Agent / Prompt 配置工具白名单

每个 Custom Agent 与 `/` Prompt 文件最顶上的 frontmatter 都有一行 `tools: [...]`——这就是它能调用的工具白名单。Copilot Chat 在加载这个 Agent 时，**只允许它使用列在这里的工具**；不在表里的工具即使在用户可见的工具下拉里出现，Agent 也调不到。

### 7.1 命名空间一览（49 个内置工具）

VS Code Copilot Chat 把所有内置工具按"用途"分到 9 个命名空间下，写 `tools` 字段时必须用 `<namespace>/<name>` 的全名。下表是当前完整清单：

| 命名空间    | 工具                                                                                                                                                               | 主要用途                                                  |
| ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ | --------------------------------------------------------- |
| `vscode/*`  | `extensions`, `getProjectSetupInfo`, `installExtension`, `memory`, `newWorkspace`, `resolveMemoryFileUri`, `runCommand`, `vscodeAPI`, `askQuestions`, `toolSearch` | 与 VS Code 自身交互（装扩展 / 查项目信息 / 触发命令面板） |
| `execute/*` | `runInTerminal`, `getTerminalOutput`, `sendToTerminal`, `killTerminal`, `createAndRunTask`, `runNotebookCell`                                                      | 跑命令、跑 Notebook 单元格、管理终端会话                  |
| `read/*`    | `readFile`, `viewImage`, `problems`, `terminalSelection`, `terminalLastCommand`, `getNotebookSummary`                                                              | 只读取上下文（文件 / 图片 / 报错 / 终端 / Notebook）      |
| `search/*`  | `codebase`, `textSearch`, `fileSearch`, `listDirectory`, `usages`, `changes`                                                                                       | 语义搜索 / 全文搜索 / 找引用 / 看 git diff                |
| `edit/*`    | `createFile`, `editFiles`, `createDirectory`, `rename`, `createJupyterNotebook`, `editNotebook`                                                                    | 写文件 / 改文件 / 新建目录 / 重命名                       |
| `web/*`     | `fetch`, `githubRepo`, `githubTextSearch`                                                                                                                          | 抓网页 / 拉 GitHub 仓库 / 搜 GitHub 代码                  |
| `browser/*` | `openBrowserPage`, `readPage`, `screenshotPage`, `navigatePage`, `clickElement`, `dragElement`, `hoverElement`, `typeInPage`, `runPlaywrightCode`, `handleDialog`  | Playwright 驱动浏览器（前端验证 / 抓页面）                |
| `agent/*`   | `runSubagent`                                                                                                                                                      | 让本 Agent 启动一个子 Agent 跑独立任务                    |
| `todo`      | （无前缀，独立工具）                                                                                                                                               | 维护 Copilot 内置的 todo list                             |

> 旧裸名（`codebase` / `fetch` / `editFiles` / `runCommands` 等）在新版 VS Code 里仍可用，但运行时会打 `Tool 'X' has been renamed, use 'Y' instead.` 警告。**新建 / 修改 Agent 一律写命名空间形式**。

### 7.2 默认配置：H1–H6 全套放开 49 个工具

本仓库自带的 11 个 Custom Agent + 4 个 Prompt 中，**除了 `/run-gate` 与 `h1-prototype-reviewer` 之外的 13 个文件**默认把整套 49 个工具都放进白名单。原因是 H1–H6 阶段虽然角色分明，但每个角色都可能临时需要：起草文档（`edit/*`）、看代码上下文（`search/*` + `read/*`）、查官方 docs（`web/fetch`）、跑构建命令验证（`execute/runInTerminal`）、对前端改动做截图核对（`browser/*`）。预留满集合可以省掉用户每加一种工作就回头改 frontmatter 的麻烦。

**真正的角色边界由 system prompt 文字（即 `agents/<role>/AGENT.md` 的指令章节）来约束**——比如 `H1-RequirementsInterviewer` 的指令明确写着"主动反问、不臆测、待澄清问题进 open-questions"，AI 不会因为有 `execute/runInTerminal` 就突然跑去执行 `dotnet test`，因为它的角色脚本没让它做这件事。换言之：**`tools` 是物理边界，prompt 是行为边界，两道闸门各司其职**。

### 7.3 两个只读例外：`/run-gate` 与 `h1-prototype-reviewer`

这两个文件的角色都是**机械化评审员**——看代码、看文档、看构建产物，**但不能写文件、不能改任务板、不能跑命令**。否则它们会自作主张去补缺项，让 gate / 评审形同虚设。

`/run-gate` 的白名单：

```yaml
tools:
  [
    search/codebase,
    search/textSearch,
    search/fileSearch,
    search/listDirectory,
    search/usages,
    search/changes,
    read/readFile,
    read/problems,
    read/getNotebookSummary,
  ]
```

`h1-prototype-reviewer` 在上述基础上多一个 `read/viewImage`（读 `prototypes/<feature>/screenshots/` 下的截图）：

```yaml
tools:
  [
    search/codebase,
    search/textSearch,
    search/fileSearch,
    search/listDirectory,
    search/usages,
    search/changes,
    read/readFile,
    read/problems,
    read/getNotebookSummary,
    read/viewImage,
  ]
```

两个都只有 `search/*` 与 `read/*`，**没有任何 `edit/*` / `execute/*` / `web/*` / `browser/*`**。`h1-prototype-reviewer` 不开 `browser/*` 是 v1 的有意设计：v1 只消费人手走过原型后留下的 markdown 与截图，让 Agent 真的去渲染 React / 点击按钮 / 截图比对是 v2 的事。

### 7.4 你想自定义时该怎么改

frontmatter 里的 `tools:` 是 YAML flow 风格列表，下面两种形式都合法：

```yaml
# 单行紧凑形式（工具少时用）
tools: [search/codebase, read/readFile, web/fetch]

# 多行展开形式（工具多时更易读）
tools:
  [
    search/codebase,
    read/readFile,
    web/fetch,
  ]
```

常见自定义场景与改法：

| 场景                                            | 改法                                                                                                                      |
| ----------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| 想把某个 Agent 限制为只读（防止它修改你的代码） | 把所有 `edit/*` 与 `execute/*` 从 `tools` 里删掉，参考 `/run-gate` 的子集                                                 |
| 想让某个 Agent 能 call subagent 拆活            | 加上 `agent/runSubagent`                                                                                                  |
| 不想让 Agent 跑浏览器（节省 token）             | 把 `browser/*` 全部从 `tools` 里删掉                                                                                      |
| 想加自定义 MCP 工具                             | MCP 工具不在 49 个内置工具里——它的命名空间由 MCP server 自己定，按 server 文档写就行                                      |
| 改完没生效                                      | Custom Agent 的 frontmatter 在 Copilot Chat **重启 / 切 Agent** 时才会重读；如果已是当前 Agent，先切到别的 Agent 再切回来 |

> 修改的是 `.github/agents/*.agent.md` / `.github/prompts/*.prompt.md`（采用方落地路径）。如果你装了 vendor 模式（默认），下一次 `install.ps1` 跑回来会按 manifest 校验你改过的文件——会触发 `[O]/[K]/[A]/[B]` 四选一询问，选 `K` 保留你的本地修改即可。

### 7.5 如何确认配置生效

切到目标 Agent 之后，在 Chat 输入框旁边的工具图标（🔧）下拉里能看到 Copilot **实际允许这个 Agent 用的工具列表**——下拉里出现的就是 `tools` 白名单的渲染结果。如果你删除了某个工具但下拉里还在，说明没生效（多半是没重启 / 没切 Agent）。

另一种确认方式：让 Agent 干一件你已经从白名单里删掉的事，它应当回复"我无权使用 `<tool>`"或类似拒绝信息，而不是真的去跑。

---

## 8. 常见问题 / 排查 / 升级 / 卸载

### Q1: Copilot 看不到我装的 Agent / Skill / Prompt

- 重启一次 VS Code（Copilot 只在启动时扫描 `.github/`）
- 确认文件确实在 `.github/agents/` / `.github/skills/<name>/SKILL.md` / `.github/prompts/`
- 检查 frontmatter 是否完整，`description` 字段必填
- 翻一下 Output Panel 的 "GitHub Copilot Chat"，看有没有解析报错

### Q2: 我改了 `.github/` 下的某个文件，下次 install 会被覆盖吗？

不会自动覆盖。`install.ps1` 拿你本地版本与 manifest 里登记的 `sha256` 比对，发现差异就**逐个弹**四选一：

```
[O]verwrite  /  [K]eep  /  [A]ll-overwrite  /  a[B]ort
```

- 选 `K`：保留本地改动，本次跳过
- 选 `O`：用新版覆盖
- 选 `A`：本次后续所有冲突一律覆盖
- 选 `B`：中断本次 install

只有传 `-Force` 才会全部静默覆盖。想长期 own 某个文件，每次升级时按 `K` 即可；想彻底脱钩、连询问都不要，从 `.harness-engineering/manifest.json` 里删掉对应那一行。

### Q3: 升级到新版本

```powershell
# 1. 拉取新版本
git -C <harness-source-repo> pull
# 2. 重新跑 install
pwsh -File <harness-source-repo>/install.ps1 -TargetRepo .
```

升级是**非破坏性**的：未改过的文件直接同步，已改过的文件按 Q2 的四选一逐个询问；新版本里删掉的文件会作为孤儿询问是否清理（不想清就加 `-NoDelete`）。

### Q4: 一键卸载

```powershell
pwsh -File .\.harness-engineering\uninstall.ps1
```

按 `manifest.json` 反向移除全部装过的文件。本地改过的文件默认**保留**并打 `keep` 标记，加 `-Force` 才会一并删除；不在 manifest 里的文件全程不动。

### Q5: 我想看完整安装日志

```powershell
Get-Content .\.harness-engineering\install.log
```

每次 install / uninstall 追加一行：时间戳 / harness commit / 目标列表 / 文件计数。可作为变更审计来源。

### Q6: 模板更新后，已经写好的产物文档（比如现有的 `docs/06-tasks/task-board.md`）会被覆盖吗？

不会。**模板只是新文档的起点**。已存在的产物文档完全归你管，install 与模板更新都不会动它们；想用上新模板的字段，需要自己手动 backport。

---

对手册本身有疑问或建议，去 [Harness Engineering 源仓库](https://github.com/shuaihuadu/harness-engineering) 提 Issue。
