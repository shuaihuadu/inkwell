---
id: H5-<NNN>
title: <功能域名称> · 实施范围
stage: H5
document_type: scope
status: draft
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
downstream: []
---

<!-- markdownlint-disable MD025 -->

# H5-NNN · 功能域名称范围

> 本文件严格按照 `docs/_templates/implementation-scope.template.md` 编写，用于把功能域拆成可独立验收的工程单元；它不是可直接交给 `h5-coding-executor` 的任务简报。
>
> `status` / `reviewers` 由 Owner 人工维护，Agent 不代签。

## 1. 目标

<描述该功能域的用户价值和实施终态。>

## 2. 上游依据

- <REQ/NFR/AC/UI/UF/ADR/HD 路径与编号>。

## 3. 当前基线

- **已有**：<真实代码或端点>。
- **缺失**：<尚未实现的能力>。
- **偏差**：<当前实现与上游设计的冲突>。

## 4. 范围

- <该功能域包含的能力>。

## 5. 不做范围

- <明确排除项及其归属的 H5 编号>。

## 6. 建议工程单元

| 子任务 | 单一交付目标 | 前置依赖 | 主要验证 |
| --- | --- | --- | --- |
| H5-NNN-A | | | |

每个子任务执行前必须根据 `implementation-task-brief.template.md` 创建独立 `ai-task-brief.md`。

## 7. 契约与设计缺口

- <缺失 API、DTO、状态机、错误语义或 H3 设计>。

## 8. 风险与待确认项

- <风险、控制方式或真实待决分歧>。

## 9. 功能域完成定义

- <所有子任务完成后可验证的终态>。
