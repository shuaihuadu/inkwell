# Harness Engineering · 操作手册（HANDBOOK）

**10 分钟读完即可上手。** 涵盖：每个目录放了什么、什么时候切哪个 Agent、什么时候打哪条 `/` 命令、模板怎么改、要卸载或升级怎么办。

---

## 目录

1. [装完后该干啥](#1-装完后该干啥按项目状态分流)
2. [全流程一览：H1 → H6 + Hx](#2-全流程一览h1--h6--hx)
3. [`.github/` 里都装了什么](#3-github-里都装了什么)
4. [`.he/` 里都装了什么](#4-he-里都装了什么)
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

> **前置步骤 · 项目身份初始化**：在仓库根创建一份最小 `AGENTS.md`（约 60 行的目录式骨架，最小可工作版本见 [Q8](#q8-我只用-github-copilot仓库根的-agentsmd-还要不要写)）。这是 `H1-RepoImpactMapper` 后续运行的硬性输入（缺它会被报 GAP-001 硬阻塞）。空仓下 H1 走完前不必写满，但“项目身份”那一句必须由项目负责人亲手签下——`AGENTS.md` 是项目对所有 AI 工具的对外声明，AI 代笔 = 闭环漏洞，与 `requirements.md` 的 `status` 签字同源。
>
> **完成后下一步**：
>
> 1. 第 1 节"项目身份"亲手签字（≤ 2 行讲清目标用户 + 核心价值）；第 4 节"模块边界 / 禁区"在 H1 阶段保留 TODO 即可，H2 完成后回来补。
> 2. 进下一节"1. H1 上半段 · 需求文本"切 `h1-requirements-interviewer`。
> 3. 在 H1 影响图（第 4 步）跑完后，如果 `repo-impact-map.md` 报了 `GAP-001 仓库根 AGENTS.md 不存在` —— 回到本前置步骤补完，把那条 GAP 标记为 `已关闭`。

1. **H1 上半段 · 需求文本**：Copilot Chat 输入框下方的 Agent 下拉切到 `h1-requirements-interviewer`，用一段大白话描述目标用户、核心场景、必做与可选——它会反问、追问、把回答落成 `docs/01-requirements/requirements.md` 草稿，分配 `REQ-001`、`REQ-002`…，没答清的进 `open-questions.md`，**不会自动用 `<TBD>` 占位**。

   _示例输入_（直接说目标，把模糊点交给它反问出来）：

   ```text
   我想做一个给自己写技术博客用的 AI 内容工厂：
   输入一个题目，AI 多轮迭代帮我磨稿子，最后吐出能直接发的 markdown。
   要求本地能跑，多家 LLM 厂商可以切——OpenAI / Azure OpenAI / 通义千问都得支持。
   一键发到微信公众号是加分项，不强求。
   先别急着写 requirements.md，先把你觉得我没说清的地方一条条问我。
   ```

2. **H1 下半段 · UI 说明 + 原型 + 评审 + 留档**：H1 不是只写 `requirements.md` 就结束了。完整 H1 还包含「UI 说明、可交互原型、评审、留档」四件事，按顺序切两个专属 Agent + 一个外部工具走完：
   - **UI 说明**：切到 `h1-ui-spec-author`，给它 `requirements.md` + 你手头的截图或参考页面，它会按 [stages/h1-requirements-and-prototype.md §5](../../../docs/stages/h1-requirements-and-prototype.md) 那 10 项反问一轮，然后产出 `docs/01-requirements/ui-spec.md` / `user-flow.md` / `acceptance-criteria.md`，没答清的**追加**到同一份 `open-questions.md`。

     _示例输入_（明确上游凭证 + 手头素材 + 推不清的就丢回 open-questions）：

     ```text
     上游需求看 docs/01-requirements/requirements.md 里 REQ-001 到 REQ-005，
     我手上还有三张草图在 prototypes/ai-content-factory/screenshots/ 下。
     按 stages/h1-requirements-and-prototype.md §5 那 10 个维度挨个问我，
     答得清的写进 ui-spec.md / user-flow.md / acceptance-criteria.md，
     答不清的全部追加到 open-questions.md——别给我用 <TBD> 占位，我会忘记回来补。
     ```

   - **可交互原型**：切到 `h1-prototype-author`，给它上一步产出的 `ui-spec.md` / `user-flow.md` / `acceptance-criteria.md` + 你项目的技术栈（或者让它从 `AGENTS.md` 第 4 节“技术栈约束”读），它会严格按 ui-spec 一一对应生成 `prototypes/<feature>/` 下的可点原型源码、起本地 dev server、自截屏，最后产出 `coverage.md` 记录 “UI-NNN → 原型文件 / 截图” 映射。**它绝不发明 ui-spec 之外的页面/状态/字段**；发现 spec 缺漏会反问你或走 open-questions。不想让 Agent 写代码的，则跳过此 Agent，用 v0.dev / Cursor / 手写都行，但你必须亲手补上 `coverage.md`。

     _示例输入_（明确技术栈与产出路径，由 Agent 最后提交原型 + coverage 映射）：

     ```text
     产出参考这几份：
     - docs/01-requirements/ui-spec.md
     - docs/01-requirements/user-flow.md
     - docs/01-requirements/acceptance-criteria.md
     本项目技术栈看 AGENTS.md 第 4 节（React 18 + Vite + Tailwind）。
     请按 ui-spec 里的 UI-001…UI-007 逐个生成页面，起 dev server、自己截屏、最后输出 coverage.md。
     ui-spec 没写过的东西一律不要加，有这种冲动就反问我。
     ```

   - **原型评审**：切到 `h1-prototype-reviewer`，它会读 `ui-spec.md` + `prototypes/<feature>/`（含上一步产出的 `coverage.md` 与截图）+ `phase-gate-checklist.md`，按 H1 那 12 条逼出 `PASS / FAIL / UNKNOWN`，**起草** `docs/02-prototype/prototype-review.md`（`status: draft`），最后弹出 picker 一次性收人工签字（评审决议 / 主审人 / 日期 / override / 修改项）回写到第 5 节。两道闸守住「AI 不给自己开绿灯」：① picker 的 `decision` 字段无 default、无 recommended，必须人工显式选；② `status: draft → reviewed` 翻转始终留给人。

     _示例输入_（说清原型在哪、UI 文档在哪、按哪份清单打分，三件齐就行）：

     ```text
     可交互原型在 prototypes/ai-content-factory/ 下，
     UI 三件套（ui-spec / user-flow / acceptance-criteria）都在 docs/01-requirements/ 里。
     按 .github/templates/phase-gate-checklist.md 里 H1 那 12 条挨个打分，
     起草 docs/02-prototype/prototype-review.md（status: draft），
     最后弹 picker 让我选评审决议——decision 不要预填默认值。
     ```

   - **纪要留档**：拿上一步的 PASS/FAIL 报告作为评审纪要起点，补充你的调整后请人评审一轮，走 `/log-review` 落到 `docs/07-reviews/YYYY-MM-DD-h1-review.md`，同时把评审结论摘要回写到 `docs/02-prototype/prototype-review.md`（这份是 H2 架构选型的输入凭证之一，不能省）。

     _示例输入_（叫出 `/log-review`，主题 + 参与者 + 结论摘要三件齐）：

     ```text
     /log-review 主题是今天的 H1 原型评审，
     参与的有产品、设计、后端、前端各一人。
     结论：12 条门禁过了 9 条，
     剩下 3 条（页面状态、错误提示、权限差异）这周内由我补齐，
     补完重跑一次 /run-gate H1 再进 H2。
     ```

   - **签字位回写**：`/log-review` 只会产出 `docs/07-reviews/<纪要>.md`，**不会**改任何上游产物的 frontmatter。评审纪要落档后，你需要亲手把上游三份文档（`requirements.md` / `ui-spec.md` / `acceptance-criteria.md`，以及有 `user-flow.md` 的话也算一份）的 frontmatter `status: draft` 改成 `reviewed`（纪要 `approved` / `approved-with-changes` 都可以进 `reviewed`；`rejected` / `pending` 保留 `draft`），同时在 `reviewers:` 里加一行记录评审人、决议、日期。这一步是设计上的人工签字位，是 H2 `H1-RepoImpactMapper` / `H2-ArchitectAdvisor` 能不能开始工作的硬门槛。后面 Q7 说明为什么任何 Agent 都不会替你动 `status` 这个字段。

     _示例变更_（以 `requirements.md` 为例，`ui-spec.md` / `user-flow.md` / `acceptance-criteria.md` 同样处理）：

     ```yaml
     ---
     id: REQ-001
     stage: H1
     status: reviewed              # 从 draft 进
     reviewers:                    # 原本为空，人工补一行
       - name: <你自己或评审人>
         decision: approved-with-changes   # 跟纪要保持一致
         date: 2026-05-07
     ---
     ```

   > **完成后下一步**：上面三件（H1 上半段、UI / 原型 / 评审、签字位回写）走完之后，跑下一节"3. 跑一次 `/run-gate H1`"做机械复核。`/run-gate` 失败回头补对应文档，**不要硬切 H2**。

3. **跑一次 `/run-gate H1`**：在 Copilot Chat 输入 `/run-gate`，它会按上面那 12 条机械核对，给出 PASS / FAIL / UNKNOWN。**只有全 PASS 才能进 H2**——这是设计上的硬卡口，绕过去后面的 commit 审计会让你在 H5 阶段重新偿还。

   _示例输入_（`/` 后面接阶段号，不需要额外参数）：

   ```text
   /run-gate H1
   ```

   > **完成后下一步**：
   >
   > - **全 PASS**：进下一步"4. H1 影响图"切 `H1-RepoImpactMapper`。
   > - **任一 FAIL / UNKNOWN**：把缺项登记到 `docs/06-tasks/task-board.md` 第 2 节"等待人工决策"，按提示回去补对应文档，再重跑 `/run-gate H1`。**不要硬切 H2**——下游 Agent 会在 frontmatter `status` 上拒收，到时候返工成本只会更高。

4. **H1 影响图**：切 `h1-repo-impact-mapper`，落 `docs/01-requirements/repo-impact-map.md`。

   **它是干啥的**：一份“这次要做的需求，落到这个仓库里会牼动哪些东西”的对账单。它**不**选技术栈、**不**设计 API、**不**写代码，只回答“现在长啥样、谁会被改、谁会被破坏、有没有禁区”。横在 H1 与 H2 之间的一道对账闸门，避免 H2/H3/H5 在沙地上盖楼。

   **不是可选**：`H2-ArchitectAdvisor` 与 `H3-DesignReviewer` 的输入契约都把它列为“必需”（参见 [`agents/architect-advisor/AGENT.md`](../../agents/architect-advisor/AGENT.md) 第 3 节、[`agents/design-reviewer/AGENT.md`](../../agents/design-reviewer/AGENT.md) 第 3 节），缺它两个 Agent 会同时阻塞。区别只在于产出形态不同：

   - **老仓改造**：扫真实代码，列受影响模块 / 文件 / 接口 / 测试，每条给置信度（high / medium / low）。**不在图上的文件，`H5-CodingExecutor` 不会改**——这是约束层的核心机制。
   - **全新空仓**：影响面表的“已存在”列全为“无”是常态，但付要做——它会用占位符（`<frontend>` / `<backend>` / `<dal>`）锁定**功能簇**作为 H2 ADR 的输入信号，并用“缺失发现 GAP-NNN”列出 H2 启动会议必须当面回答的硬依赖（登录方案 / 模型网关 / 第三方服务商等）。

   **下游怎么用它**：

   - H2 `h2-architect-advisor` 读它识别“必须复用”与“可替换”的既有组件
   - H3 `h3-design-reviewer` 拿设计中引用的路径与它反向交叉验证，疑似凭空编造的全部抦下
   - H5 `/new-task` 起任务卡时，从这里的“已存在”列拽出“允许修改的文件”列表。**AI 改不改一个文件，不取决于它觉得该不该改，取决于这份图列没列**。

   _示例输入_（老仓改造场景，强调查不到就标 UNKNOWN，不凭命名臆造）：

   ```text
   把 docs/01-requirements/requirements.md 里 REQ-001 到 REQ-005，
   映射到我现有的 src/ 目录上：
   哪些项目 / 文件 / 接口 / 测试会被动到，每条按高 / 中 / 低标置信度。
   仓库里 grep 不到的别瞎猜，直接标 UNKNOWN；
   结果落到 docs/01-requirements/repo-impact-map.md。
   ```

   _示例输入_（全新空仓场景，明说用占位符锁功能簇、用 GAP 列硬依赖）：

   ```text
   仓库是 greenfield，还没任何产品代码。
   把 docs/01-requirements/requirements.md 里本期 MVP 的 REQ 全部扫一遍。
   “已存在”列为空是常态，在“预计新增模块”列
   用 <frontend> / <backend> / <dal> 这种占位符写功能簇，
   具体路径交给 H2 ADR 决定。
   另外把你扫出来、但需求不负责的硬依赖（登录方案 / 模型网关 /
   搜索服务商等）逐条列到“缺失发现”节，给 GAP-NNN 编号，
   H2 启动会议会一条条过。
   ```

   > **完成后下一步**：
   >
   > 1. 人工评审 `docs/01-requirements/repo-impact-map.md`，把 frontmatter `status: draft → reviewed`、`reviewers:` 加一行（与 `requirements.md` 的签字位逻辑相同）。
   > 2. 把里面登记的 `GAP-NNN` 逐条搬到 `docs/06-tasks/task-board.md` 第 2 节"等待人工决策"——这是规范层的人工出口，不要让 GAP 只停在 repo-impact-map 里。
   > 3. 切到下一步"5. H2 架构 / ADR"`H2-ArchitectAdvisor`，它会读这份图识别"必须复用"与"可替换"的既有组件。

5. **H2 架构 / ADR**：切 `h2-architect-advisor`。它基于上一步的 requirements + ui-spec 给一份初版架构（项目划分、技术栈、依赖关系）+ 关键 `ADR-NNN`（每条含"选择 / 为什么 / 替代 / 放弃理由 / 维护成本 / 性能-安全-交付影响"六字段）。这一步决定源码树长什么样、用什么栈。

   _示例输入_（先说硬约束，再说想要的交付物，让 ADR 有边界）：

   ```text
   看 docs/01-requirements/ 下的 requirements.md 和 ui-spec.md。
   约束先说清：用 .NET 8、单仓多项目（src/core/* 放领域层，src/app/* 放宿主），
   本地能跑就行不强依赖云，Docker 是可选项。
   给我两份东西：
   一份初版架构（项目怎么划、用啥技术栈、谁依赖谁），
   再挑 3~5 条最关键的决策写成 ADR——
   每条都得讲清楚：选了啥、为啥选、有啥替代、为啥不要、维护贵不贵、对性能 / 安全 / 交付有啥影响。
   ```

   > **完成后下一步**：
   >
   > 1. 评审 `docs/03-architecture/architecture.md` / `tech-selection.md` / `risk-analysis.md` + `adr/` 下每条 ADR，把 `status: draft → reviewed`，`reviewers:` 加一行。
   > 2. **回填根目录 `AGENTS.md` 第 4 节"模块边界 / 禁区"**——把 H2 决定的跨模块允许 / 禁止规则写清楚，这是 H3 / H5 的边界依据。空仓时这一节是 TODO，H2 完成后必须落地。
   > 3. 跑一次 `/run-gate H2` 做机械复核；全 PASS 后挑一个或多个最小 feature 切到 H3 起草详细设计。

6. **H3 详细设计**：人手起草 `docs/04-detailed-design/<feature>/HD-NNN.md`（接口、数据模型、错误码、并发与失败语义）。写完切 `h3-design-reviewer` 让它逐项核对完备性，挡住"设计还没写清"流入下一阶段。

   _示例输入_（只评审不修改，给评审口径 + 给期望交付）：

   ```text
   帮我看一下 docs/04-detailed-design/ai-content-factory/HD-001.md。
   按 stages/h3-detailed-design.md 那份章节列表对——
   接口、数据模型、错误码、并发与失败语义、可观测性、发布回滚，每一项都看看写没写清。
   缺啥列出来告诉我下一步该补啥；这轮只评审，别动我的文档。
   ```

   > **完成后下一步**：
   >
   > 1. 按评审反馈补完详细设计后，把 `HD-NNN.md` / `database-design.md` / `api-design.md` 的 `status: draft → reviewed`，`reviewers:` 加一行。
   > 2. 跑 `/run-gate H3` 做机械复核（其中"在现有仓库结构下的落点已确认"对应 RepoImpactMapper 输出，全 PASS 才放行）。
   > 3. 切到 `H4-TestCaseAuthor` 反推测试用例。

7. **H4 测试用例**：切 `h4-test-case-author`。它从 REQ + HD 反推 `docs/05-test-design/test-cases.md`（每条 `TC-NNN`），保证每个 `REQ-NNN` 都有至少一条机械可判断的覆盖。

   _示例输入_（说清覆盖下限和分组要求，让用例不至于模糊）：

   ```text
   对着 REQ-001 到 REQ-005 和 HD-001 到 HD-003，
   反推一份测试用例矩阵到 docs/05-test-design/test-cases.md。
   两条硬要求：
   每条需求至少有一条 TC 兜底；
   每条 TC 都得是机器能判定的——给具体命令、期望输出、什么算失败。
   分三组写：契约测试、集成测试、E2E 关键流。
   ```

   > **完成后下一步**：
   >
   > 1. 评审 `docs/05-test-design/test-cases.md` / `test-matrix.md`，`status: draft → reviewed`。
   > 2. 跑 `/run-gate H4` 做机械复核。
   > 3. 切到下一节"8. H5 起任务 → 编码 → 审提交"，用 `/new-task` 起第一张任务卡。

8. **H5 起任务 → 编码 → 审提交**：上游凭证齐全后，就可以走 [1.2 节](#12-已有项目从-h5-起跳) 那四步把每条任务跑完。
9. **H6 发版说明**：版本切出来时切 `h6-release-note-writer`，从 commit 抽取生成 `docs/07-release/release-notes.md`，回写追溯矩阵。

   _示例输入_（版本号 + commit 范围 + 破坏性变更要单独成章）：

   ```text
   准备发 v0.2.0，commit 范围从上一个 tag v0.1.0 到 HEAD。
   给我写一份 docs/08-releases/v0.2.0.md：
   特性 / 修复 / 文档 / 重构 分四类列；
   破坏性变更单独开一节，每条都得告诉用户怎么迁移；
   最后顺手把追溯矩阵（REQ ↔ HD ↔ TC ↔ Task ↔ Commit）回写一下。
   ```

   > **完成后下一步**：
   >
   > 1. 评审 `docs/08-releases/v<X.Y.Z>.md` 与 `traceability-matrix.md`，`status: draft → reviewed`，发版后 `status: approved`（这是签字位的特例）。
   > 2. 跑一次 `Hx-DocGardener`（横切，不阻塞）扫一遍 `docs/`，对已腐化的文档加 `status: deprecated`，**不要物理删除**。
   > 3. 把已完成的任务从 `docs/06-tasks/task-board.md` 第 1 节迁到第 3 节"已交付任务"，回填 `发布说明` / `追溯矩阵` 两列。

> 第 1+2 步产出的 `requirements.md` / `ui-spec.md` / `acceptance-criteria.md` 是后面所有阶段的"上游凭证"——commit message 里的 `Design: REQ-001` / `Tests: TC-NNN` / `Task: TASK-NNN` 都是顺着它们往下挂的。**没有这两步，提交格式校验会一路把你打回来**。

### 1.2 已有项目从 H5 起跳

仓库已经有 `docs/01-requirements/requirements.md`（或等价的需求凭证），这次只想加一个具体的小功能，跟着这四步把最小闭环跑一遍：

1. **起一个最小任务**：在 Copilot Chat 输入 `/new-task` 加你想做的小事；首次运行它会按模板自动建 `docs/06-tasks/task-board.md`，并起草 `docs/06-tasks/T-001-xxx.md`、同时登记一行到看板。

   _示例输入_（一句话说清要做什么 + 上游凭证号）：

   ```text
   /new-task 想给 ChatHistoryService 加一个 SQL Server 持久化实现，
   对应 REQ-007 + HD-012。
   先出任务卡草稿、登记到看板，代码先别碰，等我审完再动。
   ```

   > **完成后下一步**：把任务卡 `docs/06-tasks/T-NNN-xxx.md` 打开人工审一遍，进下一节"2. 人工审任务说明"——`/new-task` 只起草，不替你确认范围。

2. **人工审任务说明**：核对 `允许修改的文件` 与 `Verify 命令` 是否合理；OK 之后把 `docs/06-tasks/task-board.md` 里这一行的 `status` 改成 `ready`。

   > **完成后下一步**：状态改成 `ready` 之后切下一节"3. 切到 `H5-CodingExecutor`"。`status` 没改 `ready` 时 Executor 不会真正执行，会要求你先确认范围。

3. **切到 `H5-CodingExecutor`**：在 Copilot Chat 输入框下方的 Agent 下拉里选它，让它按任务说明执行。

   _示例输入_（指定任务卡 + 不越界 + 每改必跑 verify）：

   ```text
   按 docs/06-tasks/T-007-sqlserver-chat-history.md 这张任务卡干。
   范围别越界——卡里“允许修改的文件”以外的别碰；
   每改完一处都跑一下 dotnet test 看跑不跑得过；
   范围不够或者测试挂了，停下来告诉我，别自己想办法绕过去。
   ```

   > **完成后下一步**：
   >
   > - **Verify 通过**：把 `docs/06-tasks/task-board.md` 这一行的 `status` 改成 `coded`，进下一节"4. 提交前切到 `H5-CommitAuditor`"。
   > - **阻塞返回（`status: blocked`）**：把 `suggested_next_action` 直接搬进 `docs/06-tasks/task-board.md` 第 2 节"等待人工决策"，按提示补完上游再回到本节重跑——**不要复用旧的 chat 上下文**（参见 `agents/_shared/io-contracts.md` 第 6 节）。

4. **提交前切到 `H5-CommitAuditor`**：让它逐字段校验 commit message（Design / Tests / Verify / Docs / Risk / Task）。

   _示例输入_（说清本次 commit 覆盖什么 + 缺字段就否决，别帮我编号）：

   ```text
   我刚把 T-007（SQL Server 聊天历史）改完准备提交。
   帮我看一下 commit message 那六字段够不够格——
   Design / Tests / Verify / Docs / Risk / Task。
   缺啥告诉我，别帮我编号——编号必须是我能从仓库里查到的真东西。
   ```

   > **完成后下一步**：
   >
   > - **审核通过**：执行 `git commit`，把 `docs/06-tasks/task-board.md` 对应行从第 1 节"在跑任务"迁到第 3 节"已交付任务"，回填 `发布说明 / 追溯矩阵` 两列（暂无 release 时填 `pending`）。
   > - **审核拒绝**：CommitAuditor 报哪个字段缺，回上游补对应凭证（`Design` 缺找 H3，`Tests` 缺找 H4），**不要自己编号**——编号必须能在仓库内被查到。

> 中小变更允许跳过 H1–H4 直接从 H5 起跳，但底线是：**每个 commit 至少要能映射到一条 `REQ-NNN`**。如果这次改动连 REQ 都对不上，先回 1.1 节第 1 步把 requirements 补齐再来——`H5-CommitAuditor` 不会替你豁免这条。

---

## 2. 全流程一览：H1 → H6 + Hx

```
┌─────────────────────── 一个特性 / 一次发版的生命周期 ───────────────────────┐
│                                                                              │
│  H1 需求文本      → H1-RequirementsInterviewer  → docs/01-requirements/      │
│  H1 UI 说明       → H1-UISpecAuthor             → docs/01-requirements/      │
│  H1 原型评审      → H1-PrototypeReviewer        → docs/02-prototype/         │
│                                                   prototype-review.md        │
│                                                   (Agent 起草 draft +        │
│                                                    picker 收人工签字)       │
│  H1 原型实践      → 你自选原型工具              → prototypes/<feature>/      │
│  H1 影响图        → H1-RepoImpactMapper         → docs/01-requirements/      │
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

> H1 是双段：**上半段**（需求文本）由 `H1-RequirementsInterviewer` 主导；**下半段**拆为三个环节：UI 说明由 `H1-UISpecAuthor` 反问产出，中间你用外部工具做 `prototypes/<feature>/` 原型，`H1-PrototypeReviewer` 读原型 + UI 文档给 PASS/FAIL，**起草** `prototype-review.md`（`status: draft`），用 picker 收人工评审签字（避免 AI 自我满足），参见 [1.1 节第 2 步](#11-全新项目从-h1-起步)。`/run-gate H1` 会把两段一起核对，只过上半段不算 H1 完成。

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
├── agents/                          ← 12 个 Custom Agent，下拉菜单可选
│   ├── h1-repo-impact-mapper.agent.md
│   ├── h1-requirements-interviewer.agent.md
│   ├── h1-ui-spec-author.agent.md
│   ├── h1-prototype-author.agent.md
│   ├── h1-prototype-reviewer.agent.md
│   ├── h2-architect-advisor.agent.md
│   ├── h3-design-reviewer.agent.md
│   ├── h4-test-case-author.agent.md
│   ├── h5-coding-executor.agent.md
│   ├── h5-commit-auditor.agent.md
│   ├── h6-release-note-writer.agent.md
│   └── hx-doc-gardener.agent.md
├── skills/                          ← 10 个 Skill，Copilot 按 description 自动调
│   ├── ai-task-brief-writer/SKILL.md
│   ├── commit-message-formatter/SKILL.md
│   ├── phase-gate-runner/SKILL.md
│   ├── traceability-linker/SKILL.md
│   ├── interactive-form-builder/SKILL.md
│   ├── architecture-reviewer/SKILL.md
│   ├── test-plan-reviewer/SKILL.md
│   ├── release-reviewer/SKILL.md
│   ├── effort-estimator/SKILL.md
│   └── prd-exporter/SKILL.md
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

## 4. `.he/` 里都装了什么

```
.he/
├── HANDBOOK.md       ← 你正在读的这份手册
├── README.md         ← 解释这个目录的角色 + .gitignore 建议
├── docs/             ← 设计文档（stages/ 阶段细则 / repo-layout.md / tech-debt-gc.md）
├── manifest.json     ← 安装清单，uninstall 用
├── install.log       ← 每次 install/uninstall 追加一行
└── uninstall.ps1     ← 一键反向清理
```

这个目录承担两件事：**随时能查规范文档**（HANDBOOK + docs/），以及**能干净卸载**（manifest + uninstall.ps1）。安装完成后它不需要你再去改——所有自定义都应该发生在 `.github/` 里。

如果觉得它和项目本身无关、不想入版本库，**推荐把它加进 `.gitignore`**：

```gitignore
.he/
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
| `interactive-form-builder` | Agent 即将向用户拿封闭枚举 / 半结构化字段（status / 评审人 / 日期 / 候选答 / 发布范围 等），把"打字反问"统一改成 picker |

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
| `H1-UISpecAuthor`            | H1    | 反问把 UI 细节逼出，按 stages/h1-requirements-and-prototype.md §5 10 项产出 ui-spec / user-flow / acceptance-criteria |
| `H1-PrototypeReviewer`       | H1    | 只读评审：读原型 + UI 文档，按 phase-gate H1 12 条 PASS/FAIL，不写文件                      |
| `H1-RepoImpactMapper`        | H1↔H3 | 产出“需求 ↔ 真实代码”对账单；H2 / H3 必需输入；H5 阶段用作 AI “允许修改文件”的边界              |
| `H2-ArchitectAdvisor`        | H2    | 起草架构选型 + ADR，每条选型留六字段                                                        |
| `H3-DesignReviewer`          | H3    | 评审详细设计是否可进 H4                                                                     |
| `H4-TestCaseAuthor`          | H4    | 从需求与设计反推测试用例矩阵                                                                |
| `H5-CodingExecutor`          | H5    | 严格按 ai-task-brief 执行编码 + Verify                                                      |
| `H5-CommitAuditor`           | H5    | 校验 commit 六字段，不合格拒合并                                                            |
| `H6-ReleaseNoteWriter`       | H6    | 从 commit-records 抽变更生成 release notes                                                  |
| `Hx-DocGardener`             | Hx    | 周期巡检 docs/ 与代码偏离                                                                   |

### 6.1 H1 下半段的两个专属 Agent：UISpecAuthor + PrototypeReviewer

H1 完整定义见 [stages/h1-requirements-and-prototype.md](../../../docs/stages/h1-requirements-and-prototype.md)，包含五件事：**需求文本 / UI 说明 / 用户流 / 可交互原型 / 评审留档**。最初版本只把"需求文本"做成了专属 Agent，下半段统一交给默认 Agent + 外部工具。**这一决策在采用方第一次跑 `/run-gate H1` 时被推翻了**：12 条门禁里下半段那 6 条经常 FAIL，原因是"默认 Agent 不会按 stages/h1-requirements-and-prototype.md §5 那 10 项主动反问"——同一组反问纪律已在上半段的 `H1-RequirementsInterviewer` 上证明有效，下半段当然也吃这套。从 v0.0.2 起，H1 下半段拆为两个专属 Agent：

| Agent                  | 性质                       | 干什么                                                                                   |
| ---------------------- | -------------------------- | ---------------------------------------------------------------------------------------- |
| `H1-UISpecAuthor`      | 反问写文档                 | 平移 RequirementsInterviewer 的纪律到 UI 维度，按 stages/h1-requirements-and-prototype.md §5 10 项产出三份文档     |
| `H1-PrototypeReviewer` | 受限评审员（draft + picker） | 读原型 + UI 文档，按 phase-gate H1 12 条 PASS/FAIL/UNKNOWN，**起草** `prototype-review.md`（`status: draft`），用 picker 收人工签字回写第 5 节 |

设计取舍：

- **PrototypeReviewer 为什么不能让 Agent 自己宣布评审通过**：评审 Agent 容易自我满足。v1 的招数是「完全不写文件」，但带来糟糕体验——用户得手动建文件、复制 chat 报告。v2 改用**两道闸**保住同一条原则：① 只能写 `docs/02-prototype/prototype-review.md` 一个文件，且永远 `status: draft`；② 第 5 节「评审决议」必须 picker 收人工选择，picker 无 default、无 recommended，AI 不替人下决心。`status: draft → reviewed` 翻转留给人。
- **可交互原型本身仍由你自选工具实现**：HTML/CSS、Figma、V0、Lovable、手绘扫描都行。`H1-UISpecAuthor` 写 ui-spec markdown，`H1-PrototypeReviewer` 读原型目录里的 markdown / 截图，原型工具的选择被严格隔离在两个 Agent 之外。
- **v1 边界**：`H1-PrototypeReviewer` 仍然只读 markdown 描述与本地截图（上游 `H1-PrototypeAuthor` 产出的）。评审员不起 dev server / 不点击 / 不重新截图是有意设计——这里是质量门禁，不该跟作者走同一个工具栈；要让评审 Agent 亲自点页面、做交互误差比对，是 v2 的事。

实操上，H1 下半段的工作流是：

```
H1-UISpecAuthor (反问 + 写 ui-spec.md / user-flow.md / acceptance-criteria.md)
    ↓
外部工具 (做 prototypes/<feature>/ 可交互原型，关键截图归档到 screenshots/)
    ↓
H1-PrototypeReviewer (12 条 PASS/FAIL/UNKNOWN +
                      起草 docs/02-prototype/prototype-review.md (status: draft) +
                      picker 收人工签字回写第 5 节)
    ↓
人工检查 §1–§4 证据 + 通过 picker 选评审决议 + 把 status: draft → reviewed
    ↓
/log-review (可选：把会议纪要细节落到 docs/07-reviews/)
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

本仓库自带的 12 个 Custom Agent + 4 个 Prompt 中，**除了 `/run-gate` 与 `h1-prototype-reviewer` 之外的 14 个文件**默认把整套 49 个工具都放进白名单。原因是 H1–H6 阶段虽然角色分明，但每个角色都可能临时需要：起草文档（`edit/*`）、看代码上下文（`search/*` + `read/*`）、查官方 docs（`web/fetch`）、跑构建命令验证（`execute/runInTerminal`）、对前端改动做截图核对（`browser/*`）。预留满集合可以省掉用户每加一种工作就回头改 frontmatter 的麻烦。

**真正的角色边界由 system prompt 文字（即 `agents/<role>/AGENT.md` 的指令章节）来约束**——比如 `H1-RequirementsInterviewer` 的指令明确写着"主动反问、不臆测、待澄清问题进 open-questions"，AI 不会因为有 `execute/runInTerminal` 就突然跑去执行 `dotnet test`，因为它的角色脚本没让它做这件事。换言之：**`tools` 是物理边界，prompt 是行为边界，两道闸门各司其职**。

### 7.3 两个收紧白名单的评审员：`/run-gate` 与 `h1-prototype-reviewer`

这两个文件的角色都是**机械化评审员**——看代码、看文档、看构建产物。差别在写权限：

- `/run-gate` 是**纯只读** —— 只 `search/*` + `read/*`，写权限完全没开。它给阶段门核对结果，结果以 chat markdown 输出，绝不动任何文件。
- `h1-prototype-reviewer` 是**受限可写** —— 多了 `vscode/askQuestions` + `edit/createDirectory` + `edit/createFile` + `edit/editFiles`，仅用来起草 `docs/02-prototype/prototype-review.md`（`status: draft`）并通过 picker 收回写人工签字。**没有 `execute/*` / `web/*` / `browser/*`**，写权限的真正约束在 `AGENT.md` 第 5/6 节用 prompt 措辞兜底（只能写一个文件、不能改 status、不能给评审决议预填默认值）。

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

`h1-prototype-reviewer` 在上述基础上多一个 `read/viewImage`（读 `prototypes/<feature>/screenshots/` 下的截图）+ `vscode/askQuestions` + `edit/{createDirectory,createFile,editFiles}`：

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
    vscode/askQuestions,
    edit/createDirectory,
    edit/createFile,
    edit/editFiles,
  ]
```

两个都**没有 `execute/*` / `web/*` / `browser/*`**。`h1-prototype-reviewer` 比 `/run-gate` 多 4 个工具，是为了实现「Agent 起草 draft + picker 收人工签字」这套体验：v1 让用户手动建文件 + 复制粘贴 chat 报告太糟，v2 把生成 prototype-review.md 这步也交给 Agent，靠两道闸守住「AI 不给自己开绿灯」——闸 1：决议 picker 无 default、无 recommended；闸 2：写出来的文件永远 `status: draft`，翻成 `reviewed` 仍归人工。`browser/*` 不开是有意设计：原型的渲染与截图交给 `H1-PrototypeAuthor` 负责，评审员不重新点页面、不重新截图，让两个 Agent 工具栈不重叠是 v2 才放开的事。

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

只有传 `-Force` 才会全部静默覆盖。想长期 own 某个文件，每次升级时按 `K` 即可；想彻底脱钩、连询问都不要，从 `.he/manifest.json` 里删掉对应那一行。

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
pwsh -File .\.he\uninstall.ps1
```

按 `manifest.json` 反向移除全部装过的文件。本地改过的文件默认**保留**并打 `keep` 标记，加 `-Force` 才会一并删除；不在 manifest 里的文件全程不动。

### Q5: 我想看完整安装日志

```powershell
Get-Content .\.he\install.log
```

每次 install / uninstall 追加一行：时间戳 / harness commit / 目标列表 / 文件计数。可作为变更审计来源。

### Q6: 模板更新后，已经写好的产物文档（比如现有的 `docs/06-tasks/task-board.md`）会被覆盖吗？

不会。**模板只是新文档的起点**。已存在的产物文档完全归你管，install 与模板更新都不会动它们；想用上新模板的字段，需要自己手动 backport。

### Q7: 为什么跑完 `/log-review` / 走完 H1，`requirements.md` 的 `status` 还是 `draft`？

这不是 bug，是设计。整套体系里的 `status` 字段（`draft` → `reviewed` → `approved` → `deprecated`）**被刻意保留为人工签字位**：

- 起草类 Agent（如 `H1-RequirementsInterviewer` / `H1-UISpecAuthor`）只产出 `draft`；
- 只读评审员（如 `H1-PrototypeReviewer`）的工具集里**没有 `edit/*`**，物理上就写不了任何文件；
- `/log-review` 只往 `docs/07-reviews/` 落评审纪要，不反向动上游产物的 frontmatter；
- 下游消费者（如 `H2-ArchitectAdvisor`）只检查 `status >= reviewed`，从不生产 `status`。

为什么这么设计：如果 Agent 能自动把自己起草的东西改成 `approved`，就出现"AI 起草 → AI 评审 → AI 签字"的闭环，两道闸门全部失效。类比 CI 跑过了仍需人点 merge，`status` 就是文档维度的 merge 按钮。

怎么处理：评审纪要落档后（`/log-review` 运行完），人工去上游三份文档里把 `status: draft` 改成 `reviewed`、在 `reviewers:` 添一行，参考 [1.1 节第 2 步 · 签字位回写](#11-全新项目从-h1-起步) 的 YAML 示例。走完这一步之后 `H1-RepoImpactMapper` / `H2-ArchitectAdvisor` 才会放你过。

> **改完 status 后下一步**：直接回到 [1.1 节第 3 步](#11-全新项目从-h1-起步) 跑 `/run-gate H1`；通过则切到第 4 步 `H1-RepoImpactMapper`，没通过按 gate 报告补缺的字段——不需要再问 Agent 接下来做啥。

### Q8: 我只用 GitHub Copilot，仓库根的 `AGENTS.md` 还要不要写？

要写，但不需要写得像百科全书。

先把三件事重新划清边界：

- 仓库根 `AGENTS.md`：**项目对所有 AI 工具的对外声明**。跨工具单一事实源（参见 [`docs/repo-layout.md` 第 10.1 节](../../docs/repo-layout.md#101-agentsmd-的使用约定)），负责项目身份、模块边界、文档目录。是项目负责人的**签字位**，不是工具链产物。顶层控制在 100 行以内，只写索引、不复述细节。
- `.github/copilot-instructions.md`：**Copilot 实施细节**。硬约束、指令集路径、专用 Agent 速查。是 `install.ps1` 装的标准件，manifest 跟踪、升级会检测本地修改。
- 两者互相 reference，**不重复内容**：`AGENTS.md` 一句话指向 `copilot-instructions.md` 讲硬约束，`copilot-instructions.md` 顶部一句话指向 `AGENTS.md` 讲项目身份。

为什么 GitHub Copilot 原生不读 `AGENTS.md`也还是得写：

- `H1-RepoImpactMapper` 的输入契约硬性要求读 `AGENTS.md` 识别“模块边界禁区”——这是规范层的约束，与底层调用哪个工具无关。
- `AGENTS.md` 是跨工具开放约定（OpenAI / Cursor / Factory 等 2025-08 联合提出）：你今天只用 Copilot，明天切 Codex / Claude Code / Cursor 时它们会直接读，这份文件保证不用重复维护项目身份。
- `install.ps1` 不装它，是因为签字位不能由工具代签（跟 Q7 的 `status` 同源）：AI 写一份“我自己暂时无限制”的声明与 AI 给自己改 `status: reviewed` 是同一种漏洞。

最小可工作版本（约 60 行）：

```markdown
# <项目名>

> AI 协作单一事实源——遵循 [AGENTS.md 跨工具开放约定](https://agents.md/)。
> 本仓库采用 [Harness Engineering 规范](.he/HANDBOOK.md) 作为工程骨架。

## 1. 项目身份

<!--
项目负责人签字位。AI 不能替写。
一句话讲清：目标用户是谁、核心价值是什么。最多 2 行。

模板示例：
> Inkwell 是给个人技术博客作者用的 AI 内容工厂：
> 输入题目 → AI 多轮迭代磨稿 → 输出可直接发布的 Markdown。

完成签字后下一步：
1. 改 docs/01-requirements/repo-impact-map.md 第 3 节，把 GAP-001 标为"已关闭（写日期）"，
   同时把 0.1 节"AGENTS.md"行从"无"改"有"。
2. 提交一条 commit 记录这次签字，参考 .github/instructions/commit-format.instructions.md。
3. 第 4 节"模块边界 / 禁区"维持 TODO 不动——H2 选型完成后再回来填。
-->
> **TODO（项目负责人签字位）**：1 句话项目定位待补。

**当前阶段**：H1 / H2 / ...

## 2. AI 工具与入口

本项目当前唯一启用 GitHub Copilot Chat。

- 工具入口：`.github/copilot-instructions.md`（Copilot 自动加载）
- Agent / Skill / Prompt 速查与白名单改法：`.he/HANDBOOK.md` 第 6 节、第 7 节

> 切到 Codex / Claude Code / Cursor 时本文件作为跨工具事实源不变。

## 3. 硬约束

详见 `.github/copilot-instructions.md` 第 1 节。本文件不复述。

## 4. 模块边界 / 禁区

<!--
H2 架构选型完成后由项目负责人补充：
  - 哪些目录是其他模块的私有领域，跨模块调用的允许 / 禁止清单
  - 哪些目录禁止 AI 自动修改（须人工评审）
  - 与外部依赖（登录 / 模型网关 / 搜索服务商等）耦合的边界

H1-RepoImpactMapper、H3-DesignReviewer、H5-CodingExecutor 都依赖本节做边界判断。

完成本节签字后下一步：
1. 跑一次 /run-gate H2 做机械复核。
2. 评审 docs/03-architecture/ 下产出，把 status: draft → reviewed。
3. 切到 H3 起草 docs/04-detailed-design/<feature>/HD-NNN.md。
-->
> **TODO（H2 选型完成后由项目负责人补充）**：空仓 / greenfield 阶段暂无既有禁区。

## 5. 文档入口

| 内容 | 位置 |
| --- | --- |
| 操作手册 | `.he/HANDBOOK.md` |
| 当前需求 | `docs/01-requirements/requirements.md` |
| 仓库影响图 | `docs/01-requirements/repo-impact-map.md` |
| H1 评审纪要 | `docs/07-reviews/` |
| 任务看板 | `docs/06-tasks/task-board.md` |

## 6. 提交规范

详见 `.github/instructions/commit-format.instructions.md`。每条 commit 含 `Design / Tests / Verify / Docs / Risk / Task` 六字段。
```

在这份最小版本中，只有第 1 节（项目身份）与第 4 节（模块边界）是你需要亲手签的签字位。其他都是指针，不需要你另外维护。**两个签字位的 HTML 注释里都内嵌了"完成签字后下一步"**——签完不知道接下来做什么时直接回头看注释，不需要再问 Agent。

为什么这里不能让 AI 代笔：这两节付与项目负责人的同一套逻辑——谁都不能替项目说“我是谁”“我该受什么限制”。`H1-RepoImpactMapper` / `H3-DesignReviewer` / `Hx-DocGardener` 都是这两节的下游消费者；AI 代笔 = 让下游评审在架空凭证上跑。
### Q9: Agent 起草的文档里有"待填"位置，我应该在哪里改？怎么改？

简短答：**搜 `[ 待填 ]`**。

整套规范统一约定：所有需要人工填答 / 决策 / 签字的位置都用 markdown blockquote + 粗体方括号标记：

```markdown
> **[ 待填 ]**：<提示语>
```

这是 `agents/_shared/io-contracts.md` 第 7 节（[源仓](https://github.com/shuaihuadu/harness-engineering/blob/main/agents/_shared/io-contracts.md)）的硬约束。在 VS Code 里直接 `Ctrl+Shift+F` 搜 `[ 待填 ]` 就能列出整个仓库还有哪些位置等你动手。

最常见的 5 类落点：

| 落点                                         | 怎么填                                                                                                                  |
| -------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| `open-questions*.md` 每条 OQ 的 **回答 / 决策日期 / 决策人** 行 | 整行替换 `> **[ 待填 ]**：...`，写下你的选择（A / B / C / 自定义）+ 1 句理由。模板见 [`templates/open-questions.md`](../../.github/templates/open-questions.md) |
| 文档 frontmatter 的 `reviewers: []`          | `/log-review` 后人工追加一行（`name / role / decision: approved / date`），格式见 `agents/_shared/io-contracts.md` 第 2 节（[源仓](https://github.com/shuaihuadu/harness-engineering/blob/main/agents/_shared/io-contracts.md)）            |
| 文档 frontmatter 的 `status: draft`          | 评审通过后人工改 `draft → reviewed`（参见 [Q7](#q7-为什么跑完-log-review--走完-h1requirementsmd-的-status-还是-draft)）                      |
| `phase-gate-checklist.md` 表格"结论"列       | `/run-gate` 跑完汇总后，人工把 `[ ]` 勾成 `[x]`，再切下一阶段                                                                     |
| `AGENTS.md` 第 1 节项目身份 / 第 4 节模块边界 | 项目负责人亲手签字，HTML 注释里内嵌了"完成签字后下一步"指引（参见 [Q8](#q8-我只用-github-copilot仓库根的-agentsmd-还要不要写)）           |

约束：

- **整行替换**：把 `> **[ 待填 ]**：...` 整行替换成你的内容，不要在原行后追加（追加 = grep 还会把这行匹配出来，看起来还没填）。
- **不要让 Agent 代填**：任何标了 `[ 待填 ]` 的位置，Agent 没有权限填——它只会在 `suggested_next_action` 里指出"哪份文件的哪行需要人工填"（参见第 5 节阻塞返回）。如果 Agent 试图替你填，那是它越权，请在 PR 评审时退回。
- **新建产物时**：如果你自己起草一份新的 OQ / review-record，按 `templates/` 下对应模板复制一份；模板自带 `[ 待填 ]` placeholder，复制后只需要替换。
---

对手册本身有疑问或建议，去 [Harness Engineering 源仓库](https://github.com/shuaihuadu/harness-engineering) 提 Issue。
