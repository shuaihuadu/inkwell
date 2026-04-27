# Harness Engineering 规范

版本：v0.1  
状态：试行（条款会在 v1.0 前持续收敛）  
适用范围：使用 AI 参与需求、设计、编码、测试、交付和文档维护的软件工程团队

## 1. 总览

### 1.1 概念背景

"Harness Engineering（套具工程）" 这一术语在 2026 年 2 月由 Mitchell Hashimoto 在其个人博客中首次提出，并由 OpenAI（Ryan Lopopolo）在 2026 年 2 月 11 日发布的文章中给出正式定义。LangChain 把它精炼为一个公式：**Agent = Model + Harness**，OpenAI 给出的标语是 **"Humans steer. Agents execute."（人类掌舵，Agent 执行）**。

业界对 Harness Engineering 的共识定义是：

> 设计环境、约束和反馈回路，让 AI 编码 Agent 在规模化场景下保持可靠的工程学科。

它强调三层结构：

- **约束层（Constraint Harness）**：规则文件、Lint、类型系统、AGENTS.md / copilot-instructions.md 等前馈控制，缩小 Agent 的解空间。
- **反馈层（Feedback Loop）**：把测试、Lint、构建错误等结构化信号回灌给 Agent，让其自我修复。
- **门禁层（Quality Gate）**：在 CI、评审、合并环节进行硬性拦截，阻止不合规产物进入主干。

主流工具区厊对这三层的落地可作为对照：

| 层次 | OpenAI Codex 体系 | Anthropic Claude Code 体系 |
| --- | --- | --- |
| 约束层 | `AGENTS.md`（多级，作为"目录"而非百科全书）、自定义 Lint、结构测试、分层架构不变式 | `CLAUDE.md`（及子目录继承）、Skills、Subagents |
| 反馈层 | 测试 / 构建 / Chrome DevTools MCP / 本地可观测栈（LogQL、PromQL）、Ralph Wiggum 循环（Agent 自审自改） | Plan Mode、测试与截图验证、Subagents 隔离调查 |
| 门禁层 | 自定义 Lint 硬失败、结构测试、后台 doc-gardening Agent、提交者路径限制 | Hooks（确定性拦截）、Permission allowlist、Sandbox、Auto Mode |

本规范作为上述体系中 **"文档化 + SDLC"** 这一层的通用骨架，与底层工具不冲突。

### 1.2 本规范的定位

本规范是上述思想在 **"团队 SDLC + AI 协作"** 场景下的一份具体落地方案，重点放在**约束层和门禁层的人工与文档化部分**：用文档、评审、测试和提交记录构成一条 Agent 在工作时必须遵守的硬轨道。它**不试图覆盖**所有运行时层面的 Harness（如 Lint 钩子、CI 拦截、运行时工具调度），这些应由具体项目的 CI/CD 流水线、`AGENTS.md` / `copilot-instructions.md` 等配套机制实现。

一句话定位：

> 本规范是 Harness Engineering 在 SDLC 维度的人工版骨架——用文档、评审、测试、提交和运行回写，把 AI 的创造力约束成可交付、可追溯、可维护的软件工程能力。

## 2. 核心原则

### 2.1 先说明，后实现

任何编码工作开始前，必须先完成需求说明、UI 说明、架构说明、详细设计和测试用例设计。

如果某项内容无法清晰说明，就不应进入编码阶段。

### 2.2 AI 生成，人类审核

AI 可以参与以下工作：

- 梳理需求
- 设计 UI 和用户流程
- 生成可交互 UI 原型
- 推演技术架构
- 生成详细设计
- 设计测试用例
- 编写代码和测试代码
- 运行测试并自我修复
- 修订文档

但每个关键阶段必须由团队审核。AI 的输出不能直接替代评审结论。

### 2.3 文档即约束

通过评审的 Markdown 文档是后续阶段的工作依据。

AI 编码时，必须严格引用已评审文档中的需求、设计、接口、数据结构、测试用例和验收标准。

### 2.4 测试先于代码

编码前必须先定义测试用例。测试用例应覆盖正常路径、异常路径、边界条件、权限边界、数据一致性和关键性能约束。

### 2.5 小步编码，小步提交

每次只让 AI 完成一个明确的工程单元。每个工程单元必须完成代码、测试、运行验证和提交记录。

推荐粒度：

- 一个程序文件及其测试
- 一个 API 端点及其测试
- 一个数据库迁移及其验证
- 一个配置项及其验证
- 一个后台任务及其测试
- 一个前端组件及其交互测试

### 2.6 运行结果回写文档

系统真实运行后，必须根据实际实现、测试结果、部署记录和运行日志修订需求、设计、测试、运维和用户说明文档。

## 3. 标准流程

本规范把 SDLC 切分为六个 Harness 阶段：

```text
H1 需求、UI 与交互原型
H2 技术架构选型
H3 详细设计
H4 测试用例设计
H5 AI 编码与自验证
H6 运行验证与文档回写
```

阶段编号 H1–H6 与文档目录 `01–07` 的映射关系如下（H1 同时产出需求和原型两类目录，因此目录数比阶段数多 1）：

| 阶段 | 文档目录 |
| --- | --- |
| H1 | `docs/01-requirements/` + `docs/02-prototype/` + `prototypes/`（原型源码） |
| H2 | `docs/03-architecture/` |
| H3 | `docs/04-detailed-design/` |
| H4 | `docs/05-test-design/` |
| H5 | `docs/06-implementation/`（任务清单与提交记录） |
| H6 | `docs/07-release/` |

每个阶段都必须满足：

- 输入清晰
- 输出完整
- 假设明确
- 风险记录
- 评审通过
- 可支撑下一阶段

## 4. H1：需求、UI 与交互原型阶段

### 4.1 目标

通过 AI 交互形成可评审的需求说明、UI 说明和可交互 UI 源码。

### 4.2 输入

- 业务想法
- 用户角色
- 用户场景
- 业务流程
- 竞品或参考系统
- 现有系统约束
- 合规、安全、权限要求

### 4.3 输出物

建议输出到：

```text
docs/01-requirements/
  requirements.md
  user-flow.md
  ui-spec.md
  acceptance-criteria.md

docs/02-prototype/
  prototype-review.md

prototypes/
  <feature-name>/
    ...                # 可交互 UI 原型源码（与 docs/ 平行的产物目录）
```

### 4.4 需求说明必须包含

- 项目背景
- 目标用户
- 用户角色
- 核心场景
- 功能范围
- 非功能需求
- 权限边界
- 数据边界
- 异常场景
- 验收标准
- 不做什么

### 4.5 UI 说明必须包含

- 页面清单
- 页面布局
- 页面状态
- 表单字段
- 操作按钮
- 错误提示
- 空状态
- 加载状态
- 权限差异
- 关键交互流程

### 4.6 评审门禁

进入下一阶段前，必须确认：

- 需求边界清楚
- UI 流程可理解
- 核心场景无明显遗漏
- 验收标准可验证
- 原型能够表达主要交互
- 未确认内容已作为风险或待定项记录

## 5. H2：技术架构选型阶段

### 5.1 目标

基于已评审的需求和 UI，使用 AI 辅助完成技术架构选型，形成架构说明 Markdown。

### 5.2 输入

- `requirements.md`
- `ui-spec.md`
- `acceptance-criteria.md`
- `prototypes/`
- 团队技术栈约束
- 部署环境约束
- 安全与合规要求

### 5.3 输出物

建议输出到：

```text
docs/03-architecture/
  architecture.md
  tech-selection.md
  risk-analysis.md
  adr/
```

### 5.4 架构说明必须包含

- 总体架构图或架构描述
- 前端架构
- 后端架构
- 数据库选型
- 缓存策略
- 消息机制
- 鉴权与权限模型
- 文件存储方案
- 部署方式
- 可观测性方案
- 性能目标
- 扩展性设计
- 安全设计
- 主要技术风险
- 替代方案比较

以下为可选项，若涉及付费云资源、商业 license 或大规模采购则必须给出：

- 成本估算

### 5.5 技术选型必须说明

每个关键技术选择都应说明：

- 选择什么
- 为什么选择
- 替代方案是什么
- 放弃替代方案的原因
- 对团队维护能力的影响
- 对成本、性能、安全、交付周期的影响

### 5.6 评审门禁

进入下一阶段前，必须确认：

- 技术路线可落地
- 团队具备维护能力
- 成本可接受
- 关键风险有缓解方案
- 架构能覆盖需求和 UI
- 不存在绕过需求的隐含功能

## 6. H3：详细设计阶段

### 6.1 目标

将需求、UI 和架构文档交给 AI，通过多轮交互生成完整详细设计。详细设计必须达到可以指导 AI 和人工工程师逐文件编码的程度。

### 6.2 输入

- `requirements.md`
- `ui-spec.md`
- `architecture.md`
- `tech-selection.md`
- `risk-analysis.md`
- `adr/`

### 6.3 输出物

建议输出到：

```text
docs/04-detailed-design/
  detailed-design.md
  database-design.md
  api-design.md
  process-design.md
  file-structure.md
  config-design.md
  log-design.md
  monitoring-design.md
  deployment-design.md
  performance-boundary.md
```

### 6.4 详细设计必须覆盖

- 数据库、表、字段、索引、约束
- API 请求、响应、错误码
- 服务、进程、后台任务、定时任务
- 每个目录的职责
- 每个程序文件的职责
- 每个配置文件的字段和默认值
- 每个日志文件的用途和格式
- 服务间通信方式
- 监控指标
- 告警策略
- 部署步骤
- 回滚策略
- 数据备份与恢复
- 性能边界
- 安全边界
- 已知限制

### 6.5 文件级设计要求

每个程序文件都应在设计中明确：

- 文件路径
- 文件职责
- 对外接口
- 内部主要函数或类
- 输入数据
- 输出数据
- 依赖模块
- 错误处理
- 日志要求
- 测试要求

示例：

```text
文件：src/Orders/CreateOrderHandler.cs
职责：处理 CreateOrderCommand，创建订单并写入订单状态变更日志
类型：internal sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand, OrderDto>
输入：CreateOrderCommand
输出：OrderDto
依赖：IOrderRepository、IInventoryService、IPaymentGateway、ILogger<CreateOrderHandler>
错误：库存不足（InsufficientStockException）、支付初始化失败（PaymentInitializationException）、重复提交（DuplicateOrderException）
日志：记录订单创建开始、成功、失败原因，使用结构化日志字段 OrderId、CustomerId、TraceId
测试：正常创建、库存不足、支付失败、重复提交、并发幂等
```

> 如果是非 .NET 技术栈，按所属语言的目录与命名约定填写文件路径与类型签名即可，字段含义不变。

### 6.6 评审门禁

进入下一阶段前，必须确认：

- 设计覆盖全部需求
- 数据结构清晰
- 接口契约清晰
- 文件职责清晰
- 配置、日志、监控、部署都有设计
- 性能和安全边界明确
- 没有把关键问题留到编码阶段

## 7. H4：测试用例设计阶段

### 7.1 目标

针对目录结构下每个程序文件构建测试用例，审核通过后再进入编码。

### 7.2 输入

- `file-structure.md`
- `detailed-design.md`
- `api-design.md`
- `database-design.md`
- `performance-boundary.md`

### 7.3 输出物

建议输出到：

```text
docs/05-test-design/
  test-plan.md
  test-matrix.md
  test-cases/
```

### 7.4 每个程序文件必须定义

- 文件职责
- 被测函数、类或模块
- 正常路径测试
- 异常路径测试
- 边界条件测试
- 权限测试
- 数据一致性测试
- 并发或重试测试
- Mock 和测试数据要求
- 测试通过标准

### 7.5 测试矩阵

测试矩阵应维护如下追溯关系：

```text
需求编号 -> 设计编号 -> 文件路径 -> 测试用例编号 -> 测试文件 -> 提交记录
```

### 7.6 评审门禁

进入下一阶段前，必须确认：

- 每个关键文件都有测试设计
- 每个核心需求都有测试覆盖
- 异常路径不是空白
- 权限和数据一致性已有测试方案
- Mock 边界清楚
- 测试结果可自动验证

## 8. H5：AI 编码与自验证阶段

### 8.1 目标

指挥 AI 按照已评审的设计和测试用例逐文件编码，包括业务代码、测试代码、配置和必要脚本。

### 8.2 输入

- 已评审详细设计
- 已评审测试用例
- 代码规范
- 项目脚手架
- 当前代码库状态

### 8.3 输出物

建议在 `docs/06-implementation/` 下沉淀，采用执行计划（exec plan）三档组织法：

```text
docs/06-implementation/
  coding-tasks.md         # H5 任务总索引：任务编号、状态、对应需求/设计/测试编号
  commit-records.md       # 提交 → 设计项 → 测试用例 的对应关系
  exec-plans/
    active/               # 进行中的复杂任务计划，带进度与决策日志
    completed/            # 已完成任务的存档
    tech-debt-tracker.md  # 已知技术债务跟踪，供后续 GC 使用
```

轻量变更（单文件、不跨设计项）可仅记录在 `coding-tasks.md`；跨多个设计项、多个提交、或需要多轮迫使 AI 迫近的复杂变更，必须在 `exec-plans/active/` 下创建独立的计划文件，完成后迁移至 `completed/`。

### 8.4 执行规范

每次编码必须遵守：

1. 只给 AI 一个明确编码单元。
2. 输入中必须包含对应需求、设计、测试用例和文件路径。
3. AI 必须同时生成或更新测试代码。
4. AI 必须给出验收命令并触发执行；若所用 AI 工具无法直接执行 shell，则由开发者在 AI 在场的对话中同步运行，并把真实输出回贴给 AI 用于自我修复。
5. 测试失败时，AI 必须分析原因并修复。
6. 测试全部通过后，才能进入提交。
7. 成功一个编码单元，提交一个编码单元。

### 8.5 AI 编码任务格式

建议使用 `templates/ai-task-brief.md`。

每次任务至少包含：

- 当前阶段
- 任务目标
- 允许修改的文件
- 禁止修改的文件
- 上游文档
- 设计引用
- 测试引用
- 验收命令
- 提交要求

### 8.6 提交要求

每次提交必须说明：

- 实现了哪个设计项
- 覆盖了哪些测试用例
- 运行了哪些测试
- 是否修改了文档
- 是否存在遗留风险

提交信息建议格式（字段名与 `ai-task-brief.md` 保持一致）：

```text
<type>(<scope>): <summary>

Design: HD-xxx
Tests: TC-xxx, TC-yyy
Verify: dotnet test --filter ...
Docs: updated / not needed
Risk: none / see known issue
```

### 8.7 评审门禁

代码合并前必须确认：

- 测试通过
- 代码符合设计
- 未引入未评审功能
- 未绕过测试
- 未破坏既有接口
- 日志、配置、错误处理符合详细设计
- 提交粒度清晰

## 9. H6：运行验证与文档回写阶段

### 9.1 目标

系统真实运行后，根据实际实现和测试结果修订所有工程文档，形成可交付的软件资料包。

### 9.2 输入

- 已运行系统
- 测试结果
- 部署记录
- 运行日志
- 代码提交历史
- 评审记录

### 9.3 输出物

建议输出到：

```text
docs/07-release/
  software-manual.md       # 用户/集成方使用说明
  requirements-final.md
  design-final.md
  ops-manual.md            # 运行起来后的运维：监控、告警、应急、备份恢复
  deployment-guide.md      # 如何把代码部署到环境：环境准备、部署步骤、回滚
  test-report.md
  release-notes.md
  known-issues.md
  traceability-matrix.md   # 需求 → 设计 → 文件 → 测试 → 提交 的最终追溯矩阵
```

> `deployment-guide.md` 与 `ops-manual.md` 边界：前者覆盖"如何把系统部署起来"，后者覆盖"系统跑起来之后怎么维护"。出现交叉时以"是否需要重新部署"为分界。

### 9.4 必须回写的内容

- 实际实现与原设计不一致之处
- 最终 API 行为
- 最终数据库结构
- 最终配置项
- 最终部署流程
- 实际性能测试结果
- 实际监控指标
- 测试覆盖情况
- 遗留问题
- 后续优化建议

### 9.5 Agent 可读的运维入口

H6 输出的运维文档不仅面向人，也面向后续代码仓库中的 AI Agent。`ops-manual.md` 和运行阶段的可观测性设计必须提供：

- 可复制粘贴的日志查询语句（如 LogQL、Kusto、SQL）
- 可调用的指标查询 / Dashboard 链接
- 常见故障场景到查询语句的映射表
- 本地复现指令（如 docker compose up、`dotnet run`、seed 脚本）

这样后续让 AI 修复线上问题时，它能从仓库本身读到"怎么看、怎么证、怎么复现"，而不是仅依赖人工口传。

### 9.6 完成标准

项目完成必须满足：

- 系统可运行
- 测试通过
- 部署可复现
- 监控可观察
- 故障可定位
- 文档已回写
- 需求、设计、代码和测试一致

## 10. 目录规范

推荐项目目录：

```text
AGENTS.md                  # 顶层：Agent 规则的"目录"，推荐 100 行以内
CLAUDE.md                  # 可选：针对 Claude Code 的别名/补充，可以仅 import AGENTS.md
.github/copilot-instructions.md  # 可选：针对 GitHub Copilot 的别名/补充

docs/
  01-requirements/         # H1
    requirements.md
    user-flow.md
    ui-spec.md
    acceptance-criteria.md
  02-prototype/            # H1
    prototype-review.md
  03-architecture/         # H2
    architecture.md
    tech-selection.md
    risk-analysis.md
    adr/
  04-detailed-design/      # H3
    detailed-design.md
    database-design.md
    api-design.md
    process-design.md
    file-structure.md
    config-design.md
    log-design.md
    monitoring-design.md
    deployment-design.md
    performance-boundary.md
  05-test-design/          # H4
    test-plan.md
    test-matrix.md
    test-cases/
  06-implementation/       # H5
    coding-tasks.md
    commit-records.md
    exec-plans/
      active/
      completed/
      tech-debt-tracker.md
  07-release/              # H6
    software-manual.md
    requirements-final.md
    design-final.md
    ops-manual.md
    deployment-guide.md
    test-report.md
    release-notes.md
    known-issues.md
    traceability-matrix.md

prototypes/                # H1，与 docs/ 平行，存放可交互 UI 原型源码
  <feature-name>/

templates/
  ai-task-brief.md
  phase-gate-checklist.md
  review-record.md

agents/                    # 配套 Agent 套件（与业务无关，详见 §10.2）
  README.md
  _shared/                 # 共享术语与 I/O 契约
  _integrations/           # 落到具体工具的轻量包装模板
  <agent-name>/
    AGENT.md
    prompt.md
```

### 10.1 AGENTS.md 的使用约定

`AGENTS.md` 是 2025-08 由 OpenAI 、Cursor 、Factory 等联合提出的跨工具开放约定，已被主流 AI 编码工具公共识别。本规范采纳其作为 Agent 规则的唯一权威文件，其他工具专用文件（`CLAUDE.md`、`.github/copilot-instructions.md` 等）可以用 import / 软链的方式指向它，避免多处维护。

根据 OpenAI 的实践经验，`AGENTS.md` **必须是一份"目录"，而不是百科全书**：

- 顶层 `AGENTS.md` 控制在 100 行以内，只写项目身份、关键约束、常用命令、文档入口
- 领域知识、详细架构、测试策略由 `AGENTS.md` **指向** `docs/` 下的权威文档，不重复书写
- 子目录（如 `src/core/AGENTS.md`、`src/app/webapp/AGENTS.md`）可增量补充**仅在子范围适用**的约束，由工具自动按路径层级拼接
- 如果一条规则可以用 Lint / Hooks / CI 强制执行，就不要只在 `AGENTS.md` 里说它——文档是 advisory，工具才是 deterministic

判断一条规则到底该不该写进 `AGENTS.md` 的常用问法：**"如果删了这一行，Agent 还会照做吗？"** 如果是，删。

### 10.2 配套 Agent 套件

本规范附带一组与业务无关、模型与工具中立的 Agent 规格，落在 [`agents/`](./agents/README.md) 目录。这些 Agent 是规范的**配套基础设施**，并非项目业务实现：

- `agents/_shared/`：共享的术语表与 I/O 契约（frontmatter、提交格式、错误结构），所有 Agent 文件通过相对链接引用，避免漂移
- `agents/<agent-name>/AGENT.md`：定位、触发时机、输入/输出契约、工具集、行为约束、验收标准、协作关系、已知边界
- `agents/<agent-name>/prompt.md`：可直接喂给任何能跑系统提示的 LLM 的纯文本工作流

首批覆盖三层 Harness 的关键岗位：

| Agent | 阶段 | Harness 层 |
| --- | --- | --- |
| RequirementsInterviewer | H1 | 反馈层 |
| RepoImpactMapper | H1↔H3 | 约束层 |
| DesignReviewer | H3 | 门禁层 |
| TestCaseAuthor | H4 | 反馈层 |
| CodingExecutor | H5 | 反馈层 |
| CommitAuditor | H5/H6 | 门禁层 |
| ReleaseNoteWriter | H6 | 反馈层 |
| DocGardener | 跨阶段 | 门禁层 + 反馈层 |

采用方可以选择性接入，无需一次性引入全部 Agent。落到具体工具时使用 [`agents/_integrations/`](./agents/_integrations/README.md) 提供的模板（覆盖 Claude Code、GitHub Copilot Chat、OpenAI Codex、自研 Runtime 四类），保持 `AGENT.md` / `prompt.md` 自身工具中立。

## 11. AI 使用规范

### 11.1 AI 输入必须明确

每次让 AI 工作时，必须提供：

- 当前阶段
- 目标产物
- 已通过评审的上游文档
- 不允许修改的范围
- 输出格式
- 验收标准
- 需要重点检查的风险

### 11.2 AI 输出必须可审计

AI 输出必须满足：

- 可审阅
- 可追溯
- 可执行
- 可测试
- 可回滚
- 不引入未确认需求
- 不隐藏假设

### 11.3 AI 禁止直接编码的情况

以下情况不得要求 AI 直接编码：

- 需求未评审
- UI 未评审
- 架构未评审
- 详细设计未完成
- 测试用例未定义
- 文件职责不清
- 接口契约不清
- 数据结构不清
- 验收标准不清

### 11.4 AI 使用反例

以下反模式取自 Anthropic 官方 Claude Code 最佳实践的常见失效模式，在本规范下同样适用。

- **杂烩会话（kitchen sink）**：在同一会话里串联多个不相关任务，上下文被无关内容填满。修正：不同任务开新会话，使用 `/clear` 或重启。
- **反复迫使（correction loop）**：同一问题超过两次仍未调对，说明上下文已被失败尝试污染。修正：重开会话，把学到的信息写进初始提示中。
- **过量规则文件（over-specified `AGENTS.md` / `CLAUDE.md`）**：规则文件越长，Agent 越会忽略重点。修正：定期修剪，能转成 Hooks / Lint 的规则就不要留在文档里。
- **估望代检验（trust-then-verify gap）**：AI 交付看似合理的实现，却未覆盖边界条件。修正：始终给出可运行的验收手段（测试、脚本、截图），无法验证的成果不予合并。
- **无界探索（infinite exploration）**：让 AI 调研"这个库怎么回事"不设边界，他会仅依赖读取大量文件填满上下文。修正：限定调研范围，或派发子 Agent / Subagent 以隔离上下文进行。

## 12. 评审规范

每个阶段评审必须保留记录，建议使用 `templates/review-record.md`。

评审记录应包括：

- 评审时间
- 参与人员
- 评审对象
- 通过项
- 修改项
- 风险项
- 结论
- 下一步动作

评审结论分为：

- `Approved`：通过，可进入下一阶段
- `Approved with Changes`：小修改后可进入下一阶段
- `Rejected`：不通过，必须返工
- `Pending`：信息不足，暂缓决策

## 13. 追溯关系

本规范要求所有交付物之间建立完整的追溯关系：

```text
需求 -> UI/原型 -> 架构 -> 详细设计 -> 测试用例 -> 代码/配置 -> 提交 -> 测试报告 -> 部署/运维文档
```

这条链最终沉淀为 `docs/07-release/traceability-matrix.md`，由 H5 阶段维护的 `commit-records.md` 汇总而成。

每个代码文件应能回答：

- 它来自哪个需求？
- 它实现哪个设计项？
- 它对应哪些测试用例？
- 它由哪个提交引入？
- 它是否已在最终文档中体现？

## 14. 质量标准

一个阶段只有在满足以下条件时，才视为完成：

- 输出物完整
- 关键假设明确
- 风险已记录
- 评审已通过
- 修改意见已处理
- 与上游文档一致
- 可支撑下一阶段工作

项目级完成标准见 §9.6。

## 15. 熵与技术债务 GC

> 该节来自 OpenAI Codex 团队在"Harness engineering"文章中提出的实践经验。在 H6 交付后，AI 代码仓库会随时间产生"状态熵"，需要持续清理。

### 15.1 黄金原则（Golden Principles）

团队应提炼出一组可机械化检查的"黄金原则"，描述项目期望代码库保持的形状：

- 优先使用共享工具包，避免手写重复逻辑
- 在边界始终使用类型验证（parse, don't validate），不凭猜测推送数据形状
- 结构化日志、命名约定、文件大小上限等"品味不变式（taste invariants）"需以 Lint 硬拦截
- 跨层依赖只能沿架构图预设方向，逾越者报错

这些原则需写进 `docs/` 下的权威文档（如 `quality-grade.md`）并同步编码为可执行检查。

### 15.2 定期 GC 任务

建议在仓库中配置定期运行的后台 AI 任务，完成以下事项：

- 扫描代码库与黄金原则的偏离，开启重构 PR
- 扫描 `docs/` 下与代码实际行为不一致的过期文档（doc-gardening），开启修复 PR
- 更新 `docs/06-implementation/exec-plans/tech-debt-tracker.md` 中的未完成项
- 合并选项：质量评级 / quality grade 表可在项目初期仅补充到 `docs/04-detailed-design/` 或 `docs/07-release/` 中

### 15.3 使用原则

- **持续偿还 > 集中重构**：技术债务像高利息贷款，每日少量偿还远优于积压后被迫集中返工。
- **人的品味一次捕获，机器永久执行**：评审心得、重构经验、线上故障复盘，要么转化为 `AGENTS.md` / Skill 里的指导，要么转化为 Lint / Hooks / CI 检查。
- **允许小幅 PR 自动合并**：GC 产出的 PR 如果可以在一分钟内评审完毕，应设置成可自动合并。

## 16. 附录：阶段门禁摘要

| 阶段 | 核心产物 | 通过标准 |
| --- | --- | --- |
| H1 | 需求、UI、原型 | 需求清晰，UI 可评审，验收标准明确 |
| H2 | 架构说明、技术选型 | 技术路线可落地，风险有缓解方案 |
| H3 | 详细设计 | 数据、接口、文件、配置、日志、部署和监控均明确 |
| H4 | 测试计划、测试用例 | 每个关键文件和核心需求都有测试覆盖 |
| H5 | 代码、测试、提交、执行计划 | 小步实现，测试通过，提交可追溯 |
| H6 | 最终文档、测试报告、运维资料 | 实现与文档一致，系统可运行可维护 |

## 17. 参考资料

本规范在设计中参考了以下公开资料：

- OpenAI · [*Harness engineering: leveraging Codex in an agent-first world*](https://openai.com/index/harness-engineering/)（Ryan Lopopolo，2026-02-11）
- OpenAI Cookbook · [*Codex Execution Plans*](https://cookbook.openai.com/articles/codex_exec_plans)
- Anthropic · [*Best Practices for Claude Code*](https://code.claude.com/docs/en/best-practices)
- Anthropic · [*Claude Code Memory（CLAUDE.md）*](https://code.claude.com/docs/en/memory)
- Anthropic · [*Claude Code Skills*](https://code.claude.com/docs/en/skills)
- [AGENTS.md 跨工具开放约定](https://agents.md/)（2025-08）
- LangChain Blog · [*The Anatomy of an Agent Harness*](https://blog.langchain.com/the-anatomy-of-an-agent-harness/)
- Mitchell Hashimoto · [*My AI Adoption Journey*](https://mitchellh.com/writing/my-ai-adoption-journey)
- Red Hat Developers · [*Harness engineering: Structured workflows for AI-assisted development*](https://developers.redhat.com/articles/2026/04/07/harness-engineering-structured-workflows-ai-assisted-development)（Marco Rizzi，2026-04-07）
- Augment Code · [*Harness Engineering for AI Coding Agents: Constraints That Ship Reliable Code*](https://www.augmentcode.com/guides/harness-engineering-ai-coding-agents)（2026-04-16）
