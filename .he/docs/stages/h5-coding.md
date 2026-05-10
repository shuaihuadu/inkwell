---
title: H5 — AI 编码与自验证阶段
parent: ./README.md
peer:
  - ./h4-test-design.md
  - ./h6-release.md
stage: H5
---

# H5：AI 编码与自验证阶段

跨阶段全景见 [`./README.md`](./README.md)。失败回退路径见 [`./README.md` §2](./README.md#2-跨阶段流程失败回退路径)。

## 1. 阶段定位与目标

指挥 AI 按照已评审的设计和测试用例**逐文件**编码，包括业务代码、测试代码、配置和必要脚本。本阶段不接受"自由发挥"——所有改动必须能映射回上游设计与测试编号。

## 2. 输入

- 已评审详细设计
- 已评审测试用例
- 代码规范
- 项目脚手架
- 当前代码库状态

## 3. 输出物

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

## 4. 执行规范

每次编码必须遵守：

1. 只给 AI 一个明确编码单元。
2. 输入中必须包含对应需求、设计、测试用例和文件路径。
3. AI 必须同时生成或更新测试代码。
4. AI 必须给出验收命令并触发执行；若所用 AI 工具无法直接执行 shell，则由开发者在 AI 在场的对话中同步运行，并把真实输出回贴给 AI 用于自我修复。
5. 测试失败时，AI 必须分析原因并修复。
6. 测试全部通过后，才能进入提交。
7. 成功一个编码单元，提交一个编码单元。

## 5. AI 编码任务格式

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

## 6. 提交要求

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

## 7. 评审门禁

代码合并前必须确认：

- 测试通过
- 代码符合设计
- 未引入未评审功能
- 未绕过测试
- 未破坏既有接口
- 日志、配置、错误处理符合详细设计
- 提交粒度清晰
