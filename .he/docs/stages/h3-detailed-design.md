---
title: H3 — 详细设计阶段
parent: ./README.md
peer:
  - ./h2-architecture.md
  - ./h4-test-design.md
stage: H3
---

# H3：详细设计阶段

跨阶段全景见 [`./README.md`](./README.md)。失败回退路径见 [`./README.md` §2](./README.md#2-跨阶段流程失败回退路径)。

## 1. 阶段定位与目标

将需求、UI 和架构文档交给 AI，通过多轮交互生成完整详细设计。**详细设计必须达到可以指导 AI 和人工工程师逐文件编码的程度**——这是 H4 测试用例与 H5 编码任务卡能否成立的前提。

## 2. 输入

- `requirements.md`
- `ui-spec.md`
- `architecture.md`
- `tech-selection.md`
- `risk-analysis.md`
- `adr/`

## 3. 输出物

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

## 4. 详细设计必须覆盖

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

## 5. 文件级设计要求

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

下面是一个 C# 类的示例：

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

## 6. 评审门禁

进入下一阶段前，必须确认：

- 设计覆盖全部需求
- 数据结构清晰
- 接口契约清晰
- 文件职责清晰
- 配置、日志、监控、部署都有设计
- 性能和安全边界明确
- 没有把关键问题留到编码阶段
