---
title: H2 — 技术架构选型阶段
parent: ./README.md
peer:
  - ./h1-requirements-and-prototype.md
  - ./h3-detailed-design.md
stage: H2
---

# H2：技术架构选型阶段

跨阶段全景见 [`./README.md`](./README.md)。失败回退路径见 [`./README.md` §2](./README.md#2-跨阶段流程失败回退路径)。

## 1. 阶段定位与目标

基于已评审的需求和 UI，使用 AI 辅助完成技术架构选型，形成架构说明 Markdown。本阶段输出是"少量决定下游大量代价"的关键决策——每条选型必须留下可回溯的证据。

## 2. 输入

- `requirements.md`
- `ui-spec.md`
- `acceptance-criteria.md`
- `prototypes/`
- 团队技术栈约束
- 部署环境约束
- 安全与合规要求

## 3. 输出物

建议输出到：

```text
docs/03-architecture/
  architecture.md
  tech-selection.md
  risk-analysis.md
  adr/
```

## 4. 架构说明必须包含

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

## 5. 技术选型必须说明

每个关键技术选择都应说明：

- 选择什么
- 为什么选择
- 替代方案是什么
- 放弃替代方案的原因
- 对团队维护能力的影响
- 对成本、性能、安全、交付周期的影响

## 6. 评审门禁

进入下一阶段前，必须确认：

- 技术路线可落地
- 团队具备维护能力
- 成本可接受
- 关键风险有缓解方案
- 架构能覆盖需求和 UI
- 不存在绕过需求的隐含功能
