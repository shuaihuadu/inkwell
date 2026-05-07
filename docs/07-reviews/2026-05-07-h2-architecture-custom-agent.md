---
date: 2026-05-07
topic: H2 架构评审 · 自定义 Agent 功能
type: self-review
status: approved
participants:
  - 作者本人（一人三角色：架构师视角 / 后端视角 / Agent 作者代表）
upstream:
  - architecture-custom-agent
  - tech-selection-custom-agent
  - risk-analysis-custom-agent
  - open-questions-arch-custom-agent
  - ADR-001-microsoft-agent-framework
  - ADR-002-agui-as-chat-protocol
  - ADR-003-multi-provider-m1
  - ADR-004-platform-login-dev-mock
  - ADR-005-web-search-mock-first
  - ADR-006-deployment-split
  - ADR-008-object-storage-azure-blob-only
downstream: []
---

# H2 架构评审 · 自定义 Agent 功能

## 1. 背景

H2-ArchitectAdvisor 在 2026-05-07 当日完成第 6 步交付：11 份架构产出（[architecture.md](../03-architecture/architecture.md) / [tech-selection.md](../03-architecture/tech-selection.md) / [risk-analysis.md](../03-architecture/risk-analysis.md) / [open-questions-arch.md](../03-architecture/open-questions-arch.md) / 7 份 ADR）落盘并通过 OQ-A-003 关闭轮升级到 v0.2。`/run-gate H2` 第一次跑出 11 PASS / 1 FAIL，FAIL 仅集中在第 12 项「评审记录已保存」。本次评审是作者一人对该批架构产出的自评（self-review），目的是把 [phase-gate-checklist H2](../../.github/templates/phase-gate-checklist.md) 第 12 项 FAIL 关闭，让 H2 能切到 H3。

## 2. 出席与角色

| 角色                 | 姓名     | 备注                                            |
| -------------------- | -------- | ----------------------------------------------- |
| 主持人 / 作者 / 评审 | 作者本人 | 一人三角色（架构师 / 后端 / Agent 作者代表）    |

> 说明：本次属 **self-review**，不替代未来真实多角色评审。H1 评审记录中 [Q-001](2026-05-07-h1-prototype-custom-agent.md#8-待澄清) 已建立"多方评审延后"决议，本次延续该立场，不重复登记同一争议（见第 8 节 [Q-001](#8-待澄清)）。

## 3. 关键发言（按时间序）

> 原话保留，仅对错别字 / 标点做最小修正；脱敏不涉及（无对外信息）。

**作者（评审视角）**：

> 架构评审通过，self-review 2026-05-07

> 唯一一句反馈。无其他发言、无修改项、无要求重写的章节。等价于 stages.md 第 5.6 节 6 条评审门禁全部接受：
> 技术路线可落地 / 团队具备维护能力 / 成本可接受 / 关键风险有缓解方案 / 架构能覆盖需求和 UI / 不存在绕过需求的隐含功能。

## 4. 决议

| 编号 | 决议 | 来源 / 关联 |
| --- | --- | --- |
| D-1 | **接受 [tech-selection.md](../03-architecture/tech-selection.md) 全部 14 条选型**（5 high / 8 medium / 1 low）。1 条 `low`（T-1 联网搜索服务商）已显式标"待 OQ-A-002 关闭后定"，不视作选型缺陷。 | tech-selection.md 第 0 节置信度分布 + 第 1~14 节每条六字段 |
| D-2 | **接受 [risk-analysis.md](../03-architecture/risk-analysis.md) 残余风险表 8 条**（RISK-001 ~ RISK-008，RISK-009 已并入 001）：自实现 SSE fallback 不同步 schema / MAF 1.x→2.x major 升级风险 / M1 漏抽 / OIDC 选定前生产不能上线 / 真实搜索服务商 SLA / 客户用其他 Ingress / 私有部署 NFS / 单副本 InMemory 误用。每条均接受。 | risk-analysis.md "残余风险接受" 表 |
| D-3 | **接受 7 份 ADR 全部 `accepted` 状态**：ADR-001 MAF / ADR-002 AG-UI 协议 / ADR-003 multi-provider M1 / ADR-004 dev mock + 生产 OIDC / ADR-005 联网搜索 Mock / ADR-006 部署形态分裂 / ADR-008 仅 Azure Blob + Local。无反对意见。 | docs/03-architecture/adr/ |
| D-4 | **OQ-A-004（OQ-030 试运行下半屏 A/B/C）选 C：本期 A 同页 / vNext 视平台聊天页面成熟度切 B**。延续 [H1 评审 D-1 暂定立场](2026-05-07-h1-prototype-custom-agent.md#4-决议)，不重新决策。 | OQ-A-004 / OQ-030 / H1 评审 D-1 / TASK-2026-05-07-001 |
| D-5 | **本次结论 = Approved**：12/12 phase-gate H2 项可全 PASS。配套 4 条行动项见第 6 节，全部为"形式合规"工作（frontmatter 状态升级 / OQ-A-004 写回 task-board / AGENTS.md 第 4 节 / 复跑 run-gate），不阻塞 H2 → H3 切换。 | phase-gate-checklist H2 第 12 项 |

## 5. 争议未决

| 编号 | 争议点 | 暂定立场 | 待回看时机 |
| --- | --- | --- | --- |
| C-1 | [`AGENTS.md` 第 4 节"模块边界 / 禁区"](../../AGENTS.md) 当前签字为 "greenfield，暂无既有禁区"。H2 落定具体技术栈与项目结构后，是否要在第 4 节增补"跨模块允许 / 禁止规则"（如 `Inkwell.Domain` 禁止依赖 EF Core / `Inkwell.Web` 禁止直引 Provider 实现等）？self-review 不替项目负责人对模块边界做硬约束。 | 本期保留 greenfield 立场不动；具体边界规则随 H3 详细设计的目录落点同步补；如 H3 阶段未补，由项目负责人单独签字。 | H3 详细设计完稿时回看 |

## 6. 行动项

| 编号 | 行动内容 | owner | due | status |
| --- | --- | --- | --- | --- |
| T-001 | 把 [architecture.md](../03-architecture/architecture.md) / [tech-selection.md](../03-architecture/tech-selection.md) / [risk-analysis.md](../03-architecture/risk-analysis.md) frontmatter `status: draft → reviewed`，[open-questions-arch.md](../03-architecture/open-questions-arch.md) `status: pending → reviewed`；4 份均在 `reviewers:` 加 `{name: self-review, decision: approved, date: 2026-05-07}` 一行 | 作者 | 2026-05-07 | open |
| T-002 | 把 OQ-A-004 = C 的决议写回 [task-board.md TASK-2026-05-07-001](../06-tasks/task-board.md)（OQ-030 最终选项），状态从 `等待人工决策` → `已决议`；同步把 [open-questions.md OQ-030](../01-requirements/open-questions.md) 状态 `non-blocking → closed 2026-05-07` | 作者 | 2026-05-07 | open |
| T-003 | [`AGENTS.md` 第 4 节"模块边界 / 禁区"](../../AGENTS.md) 是否扩写跨模块规则——本期保留 greenfield 立场不动；H3 详细设计完稿后由项目负责人决定是否补充 | 项目负责人 | H3 详细设计完稿后 | open |
| T-004 | 跑 `/run-gate H2` 复核 12/12 PASS，确认本评审记录关闭第 12 项 FAIL；若仍有 FAIL 回到对应行动项重做 | 作者 | 2026-05-07 | open |

> 4 条行动项中 T-001 / T-002 / T-004 是 self-review 落档当日可跑完的形式合规工作；T-003 留给项目负责人在 H3 完稿时决策，不阻塞 H2 → H3 切换。

## 7. 追溯

| 关联类型 | 编号 / 路径 |
| --- | --- |
| 评审对象（架构产出） | [docs/03-architecture/](../03-architecture/)（4 份核心 + 7 份 ADR + 1 ADR 占位号 ADR-007） |
| REQ | REQ-001 ~ REQ-005 / REQ-010 ~ REQ-012（[requirements.md](../01-requirements/requirements.md) 全 MVP REQ） |
| ND / R | ND-007 / ND-008 / ND-009 / ND-013 / R3 / R7 / R8（已在 H2 上游约束概要中显式锁定） |
| ADR | ADR-001 / ADR-002 / ADR-003 / ADR-004 / ADR-005 / ADR-006 / ADR-008（本次 self-review 全部 `accepted`） |
| RISK | RISK-001 ~ RISK-008（RISK-009 并入 001） |
| OQ-A | OQ-A-001 ~ OQ-A-008（OQ-A-003 已 resolved 2026-05-07；OQ-A-004 = C 由本评审 D-4 决议；其余 6 条 non-blocking） |
| OQ（H1） | OQ-030 由本评审 D-4 关闭（沿用 H1 D-1 暂定立场） |
| 上游评审 | [2026-05-07-h1-prototype-custom-agent.md](2026-05-07-h1-prototype-custom-agent.md)（D-1 / Q-001 立场延续） |
| Task | [TASK-2026-05-07-001](../06-tasks/task-board.md)（OQ-030 / OQ-A-004 决议写回，T-002）、[TASK-2026-05-07-002](../06-tasks/task-board.md)（多方评审，已暂缓，复活条件 H2 / H3 阶段非技术用户路径可用性盲点） |
| PR / Commit | `<无>`（本期未走 PR；H5 编码任务启动后才进入 commit） |

## 8. 待澄清

| 编号 | 问题 | 缺谁的输入 | 何时补 | 决议 |
| --- | --- | --- | --- | --- |
| Q-001 | 本次为 self-review，缺少真实终端用户 / 产品 / 前端 / 架构师四方代表的独立评审。是否需要在 H3 启动前补一轮多角色架构评审？ | 项目负责人决策 | H3 启动前 | **2026-05-07 决议**：延续 [H1 Q-001 立场](2026-05-07-h1-prototype-custom-agent.md#8-待澄清)，本期不补；复用 [TASK-2026-05-07-002 已暂缓](../06-tasks/task-board.md)，复活条件保持 "H2 / H3 发现非技术用户路径可用性盲点"，本评审不新增独立任务 |
| Q-002 | T-003（[`AGENTS.md` 第 4 节"模块边界 / 禁区"](../../AGENTS.md) 是否扩写）owner = 项目负责人，due = H3 详细设计完稿后；self-review 无权代项目负责人决策。如项目负责人坚持 H3 完稿前必须补，请改 due 与 owner | 项目负责人决策 | H3 详细设计完稿后 | **2026-05-07 决议**：保留默认 owner / due 不动；H3 详细设计阶段如发现需要硬性边界规则（如某 Provider 实现包不允许被 Web 层直引），由 H3-DesignReviewer 在评审时反向回传 |

---

> 归档完成。本评审纪要满足 phase-gate-checklist H2 第 12 项（评审记录已保存）的字面要求；属 self-review，**不**充分代表多方评审。Q-001 决议本期不补，复用 [task-board.md 第 4 节 TASK-002](../06-tasks/task-board.md) 暂缓项备复活条件追跟；Q-002 由 H3 阶段需要时再回看。后续步骤：
>
> 1. 跑 T-001 / T-002 / T-004，把 4 份核心文档与 [open-questions.md OQ-030](../01-requirements/open-questions.md) 的状态升级落地，跑 `/run-gate H2` 复核 12/12 PASS。
> 2. 全 PASS 后切 `H3-DesignReviewer` 起草 `docs/04-detailed-design/<feature>/HD-NNN.md`。
> 3. T-003（[`AGENTS.md` 第 4 节](../../AGENTS.md)）等 H3 详细设计完稿后由项目负责人回看；不阻塞 H2 → H3 切换。
