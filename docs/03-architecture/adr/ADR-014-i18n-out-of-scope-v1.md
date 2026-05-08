---
id: ADR-014-i18n-out-of-scope-v1
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [ Inkwell ]
created: 2026-05-08
updated: 2026-05-08
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
  - ADR-001
downstream: []
---

# ADR-014 国际化：v1 仅 zh-CN（声明边界）

## 上下文

[OQ-015 closed §A](../../01-requirements/open-questions.md) 已锁"v1 仅 zh-CN，不实现 i18n 框架"。客户端语言（[ADR-001](./ADR-001-client-runtime-electron-react.md)）所有 UI 文案、错误提示、邮件模板均使用简体中文。

[OQ-006 closed §A](../../01-requirements/open-questions.md) 范围风险已签字 → 不应在 v1 引入 i18n 框架（react-i18next / Format.JS / Resx）。

## 决策

**v1 仅支持 zh-CN；UI 文案直接以中文字面量出现在源码中（不抽到 messages.json）；接收和返回的内容均假设中文为默认语言。**

- 客户端：React 组件文案、错误 toast、表单 placeholder 直接中文字面量。
- 后端：错误消息（[RFC 7807 Problem Details](https://datatracker.ietf.org/doc/html/rfc7807) `detail`）直接中文。
- 模型 prompt 模板：中文 system prompt 为默认；用户也可在 Agent 配置中改写。
- 时间 / 数字 / 货币格式：默认 zh-CN locale（`Intl.DateTimeFormat('zh-CN')`）。
- Azure Speech ASR 语言：默认 zh-CN（[ADR-009](./ADR-009-multimodal-azure-speech.md)）。

## 备选项

### 备选 A（OQ-015 §B 引入 i18n + 双语）：v1 同时支持 zh-CN + en-US

- **放弃理由**：(1) 引入 i18n 框架（react-i18next / Format.JS）增加 H5 工作量约 15%-20%（每条文案需要 key + 两份翻译）；(2) 没有英文用户场景的真实需求 — v1 客户群在中文环境；(3) 翻译质量需要人工校对（"工具调用" / "技能" / "编排"等领域术语易翻错）。

### 备选 B（OQ-015 §C）：v1 仅 zh-CN，但用 i18n 框架以方便 v2

- **放弃理由**：(1) "为未来准备"是过度设计的典型 — v2 引入 i18n 时必须重新审视所有文案的 key 设计；(2) 留 i18n 框架但只填一种语言 = 给 v1 加摩擦但不带价值；(3) v2 真要做时，重构成本与现在抽 key 的成本相当 — 现在抽是预算前置。

### 备选 C：完全不声明语言，让模型 / 用户随意

- **放弃理由**：(1) 客户端 UI 文案必须确定为某种语言，"不声明"在工程上不存在；(2) Azure Speech ASR 必须给定语言（多语种识别准确率显著低于单语种）。

## 后果

### 正面

- H5 工期不背负 i18n 抽 key 与翻译成本。
- 错误消息直接中文 → 错误信息可读性最佳（不会出现"User has no permission to do this action"这种英文回到中文用户面前）。
- v1 测试 / 文档 / UI 全部中文，认知负担低。

### 负面

- v2 引入 i18n 时需要遍历全代码库抽 key — 详见 [RISK-010](../risk-analysis.md)。这是已知技术债，签字接受。
- 短期内不能服务非中文母语用户；这是 v1 范围裁剪的明确边界。

### 中性

- 模型输出语言由 prompt + LLM 决定，不是客户端控制；用户在 Agent 配置中可改 prompt 让模型输出英文。
- 时间 / 数字格式硬编码 zh-CN locale，未来切多 locale 时需要把所有 `toLocaleString()` 调用挂载到 i18n context。

## 状态

- **状态**：accepted
- **首次发布**：2026-05-08
- **关联**：supersedes 无；上游 [ADR-001](./ADR-001-client-runtime-electron-react.md) / [OQ-015](../../01-requirements/open-questions.md)
- **置信度**：high（OQ-015 closed；与范围控制理念一致）
