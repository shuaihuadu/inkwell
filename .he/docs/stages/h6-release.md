---
title: H6 — 运行验证与文档回写阶段
parent: ./README.md
peer:
  - ./h5-coding.md
stage: H6
---

# H6：运行验证与文档回写阶段

跨阶段全景见 [`./README.md`](./README.md)。失败回退路径见 [`./README.md` §2](./README.md#2-跨阶段流程失败回退路径)。

## 1. 阶段定位与目标

系统真实运行后，根据**实际实现和测试结果**修订所有工程文档，形成可交付的软件资料包。本阶段是 Harness 的"回写闭环"——把实际行为反向写回 docs，让追溯链最终可验证。

## 2. 输入

- 已运行系统
- 测试结果
- 部署记录
- 运行日志
- 代码提交历史
- 评审记录

## 3. 输出物

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

## 4. 必须回写的内容

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

## 5. Agent 可读的运维入口

H6 输出的运维文档不仅面向人，也面向后续代码仓库中的 AI Agent。`ops-manual.md` 和运行阶段的可观测性设计必须提供：

- 可复制粘贴的日志查询语句（如 LogQL、Kusto、SQL）
- 可调用的指标查询 / Dashboard 链接
- 常见故障场景到查询语句的映射表
- 本地复现指令（如 docker compose up、`dotnet run`、seed 脚本）

这样后续让 AI 修复线上问题时，它能从仓库本身读到"怎么看、怎么证、怎么复现"，而不是仅依赖人工口传。

## 6. 完成标准

项目完成必须满足：

- 系统可运行
- 测试通过
- 部署可复现
- 监控可观察
- 故障可定位
- 文档已回写
- 需求、设计、代码和测试一致
