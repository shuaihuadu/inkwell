---
title: Harness Engineering 各阶段细则
parent: ../README.md
---

# 各阶段细则（H1–H6）

本文件是 Harness Engineering 规范的阶段细则部分，与 [`../README.md`](../README.md) 主体并列。全文采用第 4–9 节的编号体系，其他 Agent 与文档可按 `第 4.4 节`、`第 6.5 节`、`第 9.6 节` 等编号稳定引用。

> 阶段编号 `H1`–`H6` 中的 `H` 取自 *Harness*，`Hn` 即 **Harness Stage n** 的缩写：H1 需求 / H2 架构 / H3 详细设计 / H4 测试用例 / H5 AI 编码与自验证 / H6 运行验证与文档回写。完整定义见 [`../README.md` 第 3 节](../README.md#3-标准流程)。

阅读路径建议：

- 先看 [`../README.md`](../README.md) 第 1–3 节了解三层 Harness 与 H1–H6 总流程
- 再回到本文件按需查阅具体阶段
- 评审与质量门禁的横切要求见 [`../README.md`](../README.md) 第 6–11 节

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

下面是一个C#类的示例：

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

轻量变更（单文件、不跨设计项）可仅记录在 `coding-tasks.md`；跨多个设计项、多个提交、或需要多轮迭代逼近的复杂变更，必须在 `exec-plans/active/` 下创建独立的计划文件，完成后迁移至 `completed/`。

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
