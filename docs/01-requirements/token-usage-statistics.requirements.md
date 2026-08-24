---
id: REQ-token-usage-statistics
stage: H1
status: reviewed
authors:
  - name: GitHub Copilot
    role: agent
reviewers: [Inkwell]
created: 2026-08-25
updated: 2026-08-25
upstream:
  - REQ-010
  - REQ-014
  - NFR-005
downstream:
  - HD-022
resolved-questions: []
---

<!-- markdownlint-disable MD060 -->

# Token 用量统计 · 需求增量（v1）

> 本文件是现有 Inkwell v1 需求的增量提案，后续将合入 [requirements.md](./requirements.md)、[ui-spec.md](./ui-spec.md) 与 [acceptance-criteria.md](./acceptance-criteria.md)；`status` / `reviewers` 由 Owner 人工维护。

## 1. 背景

原型已在 Agent 试运行与正式聊天的 assistant 回复下方展示 Token 用量，但正式产品当前只消费文本与 Tool/Skill 活动，未消费或持久化模型 Provider 返回的 usage。现有 AC-052 要求调试 Trace 展示每次模型调用的 Token 用量，未覆盖 UI-004 / UI-005 的回复级汇总展示。

本增量把回复级 Token 用量纳入 v1：用户可在完成的 assistant 回复下方看到该 Run 内全部模型调用的 Provider 实报用量，并在重新打开会话或跨设备访问时看到同一结果。

## 2. 功能范围

| ID      | 功能项            | 说明                                                                                                                                                      |
| ------- | ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| REQ-020 | 回复级 Token 用量 | Agent 试运行与正式聊天在完成的 assistant 回复下方展示该 Run 的输入、输出和总 Token；存在自动工具调用或其他内部模型重试时，数值覆盖该 Run 内全部模型调用。 |

## 3. 产品行为

1. Token 用量位于 assistant 回复正文下方、复制 / 重新生成 / 反馈操作上方，不单独占用消息气泡。
2. 输入、输出与总 Token 以 Provider 返回的数据为事实源；客户端和后端均不得基于文本长度、分词器或模型上限估算实际用量。
3. 一个 Run 内发生多次模型调用时，按相同计数类别求和。后端持久化保留 Provider 的 nullable 语义；AG-UI 通过 Inkwell 自定义 usage 事件原样传递 nullable 计数。
4. 流式响应可把中间 usage 聚合到运行快照用于主进程恢复，但聊天 UI 在 Run 正常完成前不得展示部分统计；Run 正常完成后发布最终聚合值。
5. Provider 未返回 usage、请求在最终 usage 到达前停止、流中断或失败时，不展示可能不完整的统计，也不补算。
6. 正式 Conversation 的完整用量随 assistant 消息持久化；刷新、重新打开会话和跨设备读取必须保持一致。
7. cached input、reasoning、audio/text 等细分类别允许后端保留，但 v1 聊天气泡只展示输入、输出和总 Token。
8. 数值遵循当前桌面端 locale 格式化；固定标签必须同时提供 `zh-CN` / `en-US` 资源。

## 4. 权限与数据边界

- Token 用量继承所属 Conversation 与消息的读取、删除、清空和级联删除权限，不新增独立资源或管理端点。
- 用量只表示模型调用计数，不等同于费用、账单或配额。
- 调试 Trace 的逐次模型调用 usage 仍归 REQ-014；本增量只定义用户可见的回复级聚合值。

## 5. 不做范围

- 不做价格换算、成本报表、预算、配额、告警或账单对账。
- 不做按用户、Agent、模型或时间范围的统计分析页。
- 不展示 Provider 未报告的推测值。
- 不为停止、断流或失败 Run 展示部分用量。
- 不新增独立 `agent_runs` 或 usage 分析事实表；后续分析需求应结合 Traces 单独设计。

## 6. 异常场景

| 编号   | 场景                           | 期望行为                                                                                                                             |
| ------ | ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------ |
| EX-020 | Provider 不返回 usage          | 回复正常展示，不显示 Token 用量区域。                                                                                                |
| EX-021 | Tool loop 中部分调用缺少 usage | 后端与桌面端只累加 Provider 已报告类别；正式 Conversation 重新读取历史后以后端持久化值为准。 |
| EX-022 | 停止、断流或模型调用失败       | 不持久化或展示可能不完整的 Token 用量；消息与既有错误 / 停止语义保持不变。                                                           |
| EX-023 | 用量持久化失败                 | 不覆盖已经成功生成的回复；服务端记录结构化 warning，历史中该回复不显示用量。                                                         |

## 7. 验收标准

| REQ / EX | 验收标准                                                                                         |
| -------- | ------------------------------------------------------------------------------------------------ |
| REQ-020  | 单次无工具调用的完成回复显示 Provider 实报的输入、输出和总 Token，位置在正文下方、消息操作上方。 |
| REQ-020  | 一个包含至少两次模型调用的 Tool loop 回复显示各调用 usage 的分类求和，而不是最后一次调用的值。   |
| REQ-020  | 刷新应用、重新打开 Conversation 或从另一客户端读取历史后，用量与 Run 完成时一致。                |
| REQ-020  | `zh-CN` / `en-US` 下标签与数字格式正确，缺失 usage 时不渲染空容器。                              |
| EX-020   | Provider 不返回 usage 时，回复仍完成且不显示 `0 / 0 / 0`。                                       |
| EX-022   | 在最终 usage 到达前停止或断开流时，不显示或持久化部分统计。                                      |
| EX-023   | 模拟 usage 更新失败时，回复内容仍可读取，服务端产生不含消息正文的 warning。                      |

## 8. 设计边界

- `Microsoft.Extensions.AI.UsageDetails` 的捕获、聚合和持久化结构由 HD-022 定义。
- 双 Provider JSON 列、EF Core Migration、REST Response、Electron IPC 与 UI 组件均由 HD-022 定义。
- Trace 逐调用明细不在 HD-022 范围内，等待 `Inkwell.Core.Traces` 详细设计。
