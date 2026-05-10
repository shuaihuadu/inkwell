---
title: H1 — 需求、UI 与交互原型阶段
parent: ./README.md
peer:
  - ./h2-architecture.md
stage: H1
---

# H1：需求、UI 与交互原型阶段

跨阶段全景见 [`./README.md`](./README.md)。失败回退路径见 [`./README.md` §2](./README.md#2-跨阶段流程失败回退路径)。

## 1. 阶段定位与目标

通过 AI 交互形成可评审的**需求说明、UI 说明和可交互 UI 源码**。本阶段把"业务想法"转换成下游 H2 选型、H3 设计、H4 用例可以稳定引用的事实源。

## 2. 输入

- 业务想法
- 用户角色
- 用户场景
- 业务流程
- 竞品或参考系统
- 现有系统约束
- 合规、安全、权限要求

## 3. 输出物

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

## 4. 需求说明必须包含

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

## 5. UI 说明必须包含

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

## 6. 评审门禁

进入下一阶段前，必须确认：

- 需求边界清楚
- UI 流程可理解
- 核心场景无明显遗漏
- 验收标准可验证
- 原型能够表达主要交互
- 未确认内容已作为风险或待定项记录
