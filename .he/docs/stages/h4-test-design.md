---
title: H4 — 测试用例设计阶段
parent: ./README.md
peer:
  - ./h3-detailed-design.md
  - ./h5-coding.md
stage: H4
---

# H4：测试用例设计阶段

跨阶段全景见 [`./README.md`](./README.md)。失败回退路径见 [`./README.md` §2](./README.md#2-跨阶段流程失败回退路径)。

## 1. 阶段定位与目标

针对目录结构下每个程序文件构建测试用例，**审核通过后再进入编码**。本阶段是 Harness 反馈层的核心：测试用例不齐 / 不可执行，下游 H5 的"AI 自我修复"就失去对照基准。

## 2. 输入

- `file-structure.md`
- `detailed-design.md`
- `api-design.md`
- `database-design.md`
- `performance-boundary.md`

## 3. 输出物

建议输出到：

```text
docs/05-test-design/
  test-plan.md
  test-matrix.md
  test-cases/
```

## 4. 每个程序文件必须定义

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

## 5. 测试矩阵

测试矩阵应维护如下追溯关系：

```text
需求编号 -> 设计编号 -> 文件路径 -> 测试用例编号 -> 测试文件 -> 提交记录
```

## 6. 评审门禁

进入下一阶段前，必须确认：

- 每个关键文件都有测试设计
- 每个核心需求都有测试覆盖
- 异常路径不是空白
- 权限和数据一致性已有测试方案
- Mock 边界清楚
- 测试结果可自动验证
