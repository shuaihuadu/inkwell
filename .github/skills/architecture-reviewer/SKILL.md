---
name: architecture-reviewer
description: 对 H2 已产出的架构说明、技术选型、风险分析、ADR 做证据级核查。当用户说"H2 评审"、"看看架构写得行不行"、"选型理由够不够"、"准备从 H2 进 H3"、"ArchitectAdvisor 写完了帮我审"时主动调用。它与 phase-gate-runner 的差异是粒度：phase-gate-runner 是"清单是否齐"，本 Skill 是"清单项的证据是否在文件里、是否有效、是否被 Agent 用空话填了"——重点机械检查每条选型的"选什么/为什么/替代方案/放弃理由/维护影响/成本性能安全交付影响"六字段。
when_to_use: |
  - H2 文档（architecture.md / tech-selection.md / risk-analysis.md / adr/*）已落盘，准备进 H3
  - ArchitectAdvisor 自审通过后的二次复核（避免"AI 自我满足"）
  - H3 DesignReviewer 反问中出现"上游架构选型理由不充分"时回炉
when_not_to_use: |
  - H2 文档还在 draft——先让 ArchitectAdvisor 补到 reviewed 再来
  - 用户在做"是否要从 X 换到 Y"的选型本身——那是 ArchitectAdvisor 的工作
  - 用户在做架构改动的影响分析——用 RepoImpactMapper / DesignReviewer
---

# Skill: 架构证据评审器（Architecture Reviewer）

## 1. 目的与原理

H2 是"少量决定下游大量代价"的阶段。一条选型的"为什么"如果写得糊弄，下游 H3/H5 撞到边界时无法回溯——只能再开一次会重选。`ArchitectAdvisor` 已经按 [`docs/stages/h2-architecture.md` §5](../../../docs/stages/h2-architecture.md)的六字段输出选型，但它**自己写自己审**会自动开绿灯。本 Skill 提供的是一份独立的、机械化的证据核查表。

> 与现有 Reviewer Agent（PrototypeReviewer / DesignReviewer / CommitAuditor）的关系：本 Skill 故意做成轻量 SOP 而非 Agent，因为 H2 评审的硬要求高度机械化（六字段齐 / 替代方案有比较 / 风险有缓解），不需要独立角色身份。复杂的 H2 内容质量评审仍应由人主持。

> 与 phase-gate-runner 的关系：phase-gate-runner 答 12 条勾选清单的"是否打勾"；本 Skill 答"打勾的依据是不是真的在文件里、引用是否对得上、有没有空话"。

## 2. 输入

必需：

- `docs/03-architecture/architecture.md`（`status` ≥ `reviewed`）
- `docs/03-architecture/tech-selection.md`
- `docs/03-architecture/risk-analysis.md`
- `docs/03-architecture/adr/`（全部 ADR 文件）
- `docs/01-requirements/requirements.md`（`status: approved`）
- `docs/01-requirements/repo-impact-map.md`
- `templates/phase-gate-checklist.md` 的 H2 段
- `AGENTS.md`（模块边界、技术栈约束）

可选：

- 上一版本 H2 文档（用于增量评审）
- `prototypes/<feature>/`（用于核对前端架构是否能承载原型交互）

## 3. 步骤

### 3.1 列出受审条目

逐项扫描 `tech-selection.md`，把每一条选型登记到一张表里：

| 选型条目 | 文件 + 行号 |
| --- | --- |
| 数据库 = PostgreSQL 16 | tech-selection.md#L12 |
| 缓存 = Redis 7 | tech-selection.md#L34 |
| 消息队列 = NATS JetStream | tech-selection.md#L56 |
| ... | ... |

### 3.2 对每条选型核查六字段

按 [`docs/stages/h2-architecture.md` §5](../../../docs/stages/h2-architecture.md)六字段逐项打分。**不允许"看上去差不多"通过**——必须能在文件里指出对应文字：

| 字段 | 通过标准 | 常见放水形态（→ FAIL） |
| --- | --- | --- |
| 选择什么 | 给出具体技术 / 版本号 / SKU | "数据库"（缺版本）→ FAIL |
| 为什么选择 | ≥2 条业务/工程依据，能映射到 REQ 或 NFR | "团队熟悉" 单一理由 → PARTIAL |
| 替代方案是什么 | ≥1 条具体技术（不是"其它方案"） | "调研了多种方案" → FAIL |
| 放弃替代方案的原因 | 与"为什么选择"对偶；不能是同一句话翻译 | "性能不够"（无数字）→ PARTIAL |
| 对团队维护能力的影响 | 显式提到学习曲线 / 招聘 / 现有人员熟悉度 | 缺失 → FAIL |
| 对成本 / 性能 / 安全 / 交付的影响 | 至少明确说"哪一项最受影响" | 笼统说"无明显影响"→ PARTIAL |

每条选型得到一个三态结果：`PASS` / `PARTIAL` / `FAIL`。

### 3.3 一致性核查

机械化交叉比对：

| 检查项 | 怎么查 |
| --- | --- |
| `architecture.md` 提到的所有组件 ↔ `tech-selection.md` 都有选型条目 | 取组件名集合 A，取选型条目集合 B，差集 A−B 必须为空 |
| `tech-selection.md` 引用的 ADR ↔ `adr/` 实际存在 | grep `ADR-NNN` 编号在 `adr/` 下都有对应文件 |
| `risk-analysis.md` 中"已知风险" ↔ "缓解方案" | 一一对应，缺缓解的风险标 `unmitigated` |
| ADR 的 `status` ↔ `tech-selection.md` 引用方式 | 引用 `deprecated` ADR 必须显式说明"已弃用，迁移到 ADR-XXX" |
| 模块边界 ↔ `AGENTS.md` 第 4 节 | 架构图里的模块边界与 `AGENTS.md` 禁区不能矛盾 |

### 3.4 风险与缓解清单核查

对每条风险，必须包含：

- **触发条件**（什么情况下会发生）
- **影响范围**（数据 / 用户 / 服务）
- **缓解方案**（事前预防 / 事中检测 / 事后回滚 任选其一以上）
- **责任归属**（哪个角色 / 阶段负责落实）

四项任缺一项即 `PARTIAL`。整段风险无对应缓解 → `FAIL`。

### 3.5 输出评审报告

报告**不写盘**，输出到对话框，由用户决定归档（建议归到 `docs/_review-records/H2-arch-<YYYY-MM-DD>.md`，不污染 `docs/03-architecture/` 本身）：

```markdown
# H2 Architecture Review · <scope> · <YYYY-MM-DD>

- 受审产物：docs/03-architecture/* @ commit <sha>
- 评审依据：docs/stages/ §5.5 + templates/phase-gate-checklist.md H2

## 选型六字段核查

| 选型 | 选什么 | 为什么 | 替代 | 放弃理由 | 维护影响 | 成本/性能/安全/交付 | 总评 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 数据库 = PostgreSQL 16 | PASS | PASS | PASS | PASS | PASS | PASS | **PASS** |
| 缓存 = Redis 7 | PASS | PARTIAL | PASS | FAIL | PASS | PASS | **FAIL** |
| 消息队列 = NATS | FAIL（缺版本） | PASS | PASS | PASS | PARTIAL | PASS | **FAIL** |

## 一致性核查

- ✓ architecture.md 8 个组件全部在 tech-selection.md 有对应条目
- ✗ tech-selection.md#L78 引用 ADR-005，但 docs/03-architecture/adr/ 下不存在该文件
- ✓ risk-analysis.md 5 条风险全部有缓解方案

## 风险清单核查

- 风险 R-002（缓存雪崩）：缓解只写"加超时"，未指定阈值与监控指标 → PARTIAL
- ...

## 阻塞清单（必须修复后才能进 H3）

1. **NATS 选型缺版本号**：`tech-selection.md#L56`，请补 `NATS JetStream 2.10.x`。
2. **ADR-005 引用失效**：`tech-selection.md#L78`，请创建 ADR-005 或修正引用编号。
3. **Redis 缓存放弃替代方案理由不足**：`tech-selection.md#L40`，"性能不够"过于笼统，请引用具体基准数据或场景。

## 待人工判断（UNKNOWN）

- 风险 R-004（多租户数据隔离）的缓解方案"由后续 ADR 决定"——这是合理的延后还是赌概率？需架构师当面决议。

## 放行结论

- ✗ 不可放行（3 条阻塞 + 1 条待人工判断）
- 建议：先修阻塞清单，UNKNOWN 项作为评审会议程
```

### 3.6 落盘建议

报告内容贴出后，由用户决定：

- **未通过**：把阻塞清单登记到 [`docs/_open-questions/open-questions.md`](../../../templates/open-questions.md) 或项目的 task-board，由 ArchitectAdvisor 修订后再跑一次本 Skill。
- **通过**：把报告归档到 `docs/_review-records/H2-arch-<YYYY-MM-DD>.md`，作为 H2 → H3 切换的放行凭证。

## 4. 失败模式与回退

- **architecture.md 缺章节**（[`docs/stages/h2-architecture.md` §4](../../../docs/stages/h2-architecture.md) 列的章节有缺）：直接报阻塞，不要"自己脑补一下"。
- **被要求评内容质量**（"这个选型对不对"）：拒绝。本 Skill 只查"理由是否齐"，不评"理由是否对"——评对错是架构师的工作，由人在评审会决定。
- **跨阶段证据混入**（H3 详细设计已经开始落地，user 说"反正 HD 都写完了，H2 就别太较真"）：拒绝。H2 的事必须在 H2 关掉，否则下游 H3/H5 撞墙时无回溯证据。
- **同一选型有多版本 ADR 共存**：报告 `ADR conflict`，要求用户先把废弃 ADR 显式标 `deprecated`。

## 5. 示例

**输入**：

> 我们 H2 写完了，准备进 H3，先让你审一下。

**操作**：读 `docs/03-architecture/*` + `templates/phase-gate-checklist.md`，按 3.2-3.4 逐条核查。

**输出（节选）**：见 3.5 节模板。

## 6. 与其它 Skill / Agent 的边界

- 想**改进选型本身**：让用户回到 `ArchitectAdvisor` Agent。
- 想**核对清单是否齐**：用 `phase-gate-runner`，本 Skill 是它的下一层（证据级）。
- 想**追溯 REQ ↔ ADR**：用 `traceability-linker`。
- 想**评 H3 详细设计**：用 `DesignReviewer` Agent。
