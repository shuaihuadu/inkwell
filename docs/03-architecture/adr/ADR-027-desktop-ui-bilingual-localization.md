---
id: ADR-027-desktop-ui-bilingual-localization
stage: H2
status: draft
authors:
  - name: GitHub Copilot
    role: agent
reviewers: []
created: 2026-08-24
updated: 2026-08-24
upstream:
  - REQ-inkwell-agent-platform
  - ADR-001
  - ADR-014
downstream:
  - REQ-019
---

# ADR-027 桌面端 UI 支持中英文切换

## 上下文

[ADR-014](./ADR-014-i18n-out-of-scope-v1.md) 与 [OQ-015](../../01-requirements/open-questions.md) 曾将 v1 限定为仅 `zh-CN`。2026-08-24，Owner 要求桌面端 UI 支持中文与英文，并要求公共操作文案集中复用。本决策只扩大客户端自有 UI 文案范围，不同时国际化后端错误、模型输出或其他业务内容。

## 决策

桌面端 Renderer 支持 `zh-CN` 与 `en-US` 两种界面语言：

- 默认偏好为 `zh-CN`，用户可在“个人设置 / Preferences”中选择 `zh-CN`、`en-US` 或“跟随系统 / System”。
- “跟随系统”读取客户端系统首选语言：中文映射到 `zh-CN`，英文映射到 `en-US`，其他语言回退 `zh-CN`；客户端运行期间收到系统语言变化事件时重新解析。
- 语言切换即时生效，并以本地客户端偏好持久化；应用重启后保持最近一次选择。
- 使用 `i18next` + `react-i18next` 管理消息资源。资源按 `common`、`shell`、`auth`、`agents`、`chat` 等领域组织。
- “取消、确认、保存、关闭、刷新、重试、搜索、查看、编辑、删除、复制”等跨页面操作统一复用 `common.*`，领域资源不重复定义。
- Ant Design locale、日期与数字格式化、`html.lang` 与当前界面语言保持一致。
- 英文资源缺少 key 时回退到 `zh-CN`，避免展示裸 key 或空白文本。

本期明确不处理：

- 后端 `ProblemDetails`、API 错误正文及异常消息；前端可翻译自己添加的上下文前缀，但服务端正文保持原样。
- 用户输入、Agent 配置内容、Tool / Skill / Model 元数据、模型输出与 prompt。
- Azure Speech ASR 语言及多模态处理策略。
- 除 `zh-CN` / `en-US` 以外的界面资源、服务端同步用户语言偏好。

## 与 ADR-014 的关系

本 ADR 经 Owner 接受后 supersede ADR-014 的“桌面端 UI 仅 `zh-CN`、不引入 i18n 框架”结论。ADR-014 关于后端错误、模型 prompt 与 ASR 不随 UI locale 切换的边界继续有效，直到对应能力另行立项。

OQ-015 作为历史决策记录保留原文，不回溯改写。

## 后果

### 正面

- 桌面端可直接服务中英文用户。
- 公共操作文案集中维护，减少重复翻译和术语漂移。
- 第三方组件、日期与数字格式跟随同一 locale，避免页面局部语言不一致。

### 负面

- 新增或修改界面时必须同时维护两份消息资源。
- 英文领域术语需要持续人工校对。
- 当前语言偏好只保存在本机，不会跨设备同步。

### 风险控制

- 单元测试覆盖默认语言、英文公共文案和 fallback。
- ESLint 与源码扫描用于发现组件中的固定 UI 字面量。
- 单元测试覆盖系统中文、系统英文及不支持语言的解析结果；Electron E2E 覆盖语言切换即时生效与重启保持。

## 状态

- **状态**：draft，待 Owner 审阅。
- **拟替代**：ADR-014 的桌面端 UI 范围。
- **置信度**：high（功能已按本决策实现并通过 desktop test / lint / build）。
