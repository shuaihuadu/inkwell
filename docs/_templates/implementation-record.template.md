---
id: H5-<NNN>
title: <功能域名称> · 实施记录
stage: H5
document_type: implementation-record
status: draft
implementation_state: <implemented|partial|unverified>
authors:
  - name: <姓名或 Agent 名>
    role: <owner|agent>
reviewers: []
created: <YYYY-MM-DD>
updated: <YYYY-MM-DD>
upstream:
  - REQ-<NNN>
  - NFR-<NNN>
  - ADR-<NNN>
tests:
  - AC-<NNN>
downstream: []
---

<!-- markdownlint-disable MD025 -->

# H5-NNN · 功能域名称实施记录

> 本文件严格按照 `docs/_templates/implementation-record.template.md` 编写，只记录仓库中可核实的当前实现和验证证据，不把计划或推测写成已完成事实。
>
> `implementation_state` 只描述可由代码核实的实施事实，不等于所有验收项已通过；`status` / `reviewers` 由 Owner 人工维护，Agent 不代签。

## 1. 实施状态

- **结论**：<已实现 / 部分实现 / 已实现但待补验证>。
- **代码基线**：<commit，可未知>。
- **记录日期**：YYYY-MM-DD。

## 2. 上游依据

- <REQ/NFR/AC/UI/UF/ADR/HD 路径与编号>。

## 3. 已实现内容

| 路径 / 符号 | 当前职责 | 对应需求 |
| --- | --- | --- |
| | | |

## 4. 已验证证据

| 验证项 | 命令或测试 | 结果 | 日期 |
| --- | --- | --- | --- |
| | | | |

没有实际执行记录的项目不得写为通过，应移入 §5。

## 5. 待补验证与实现缺口

| 缺口 | 关联 AC / 风险 | 后续任务 |
| --- | --- | --- |
| | | |

## 6. 已知偏差

- <当前实现与上游设计不一致之处；没有则写“无”>。

## 7. 后续任务

- <H5-NNN-A：单一目标>。

## 8. 维护规则

- 新验证完成后追加 §4 并更新 §5，不写编年史式叙事。
- 行为发生变化时直接更新当前状态；历史由 git 和评审记录保留。
- 不在本文件代签 `status` / `reviewers`。
