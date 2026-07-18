---
id: repo-impact-map-inkwell-agent-platform
stage: H1
status: reviewed
authors:
  - name: H1-RepoImpactMapper
    role: agent
reviewers: [Inkwell]
created: 2026-05-08
updated: 2026-05-08
upstream:
  - REQ-inkwell-agent-platform
  - ui-spec-inkwell-agent-platform
  - user-flow-inkwell-agent-platform
  - acceptance-criteria-inkwell-agent-platform
downstream: []
---

# Inkwell Agent 平台 · 影响面地图（H1 ↔ H3 衔接）

> 本文件用途：把已 reviewed 的 REQ / NFR 映射到**真实仓库**的代码与文档，作为 H2 / H3 设计输入。
>
> 撰写约束：本仓库当前为绿地状态（见 §0），所有"受影响文件（已存在）"列均为 `—`，所有路径建议均标注为"建议"。文中所有表格 ≤ 4 列，长内容下沉到 bullet 子段以避开 lint MD060。

---

## 0. 基线状态（必读）

本仓库当前为**绿地（greenfield）项目**：

- **顶层目录**：仅 `.git/` / `.github/` / `.he/` / `docs/` / `LICENSE`
- **源码**：无（无 `src/` / 无 `*.cs` / 无 `*.ts` / 无 `*.py` / 无任何编程语言文件）
- **测试**：无（无 `tests/`）
- **工程化骨架**：无（无 `Directory.Build.props` / `pyproject.toml` / `package.json` / 任何 `*.sln`）
- **顶层 `AGENTS.md`**：不存在——因此**没有禁区目录约束**
- **顶层 `README.md`**：不存在
- **既有 ADR**：无（`docs/03-architecture/` 不存在）
- **既有详细设计**：无（`docs/04-detailed-design/` 不存在）
- **H1 产出（仅有的输入）**：[docs/01-requirements/](./) 下 `requirements.md` / `ui-spec.md` / `user-flow.md` / `acceptance-criteria.md` / `open-questions.md`
- **评审记录**：[2026-05-08-openquestion-discussion.md](../07-reviews/2026-05-08-openquestion-discussion.md)

直接推论：

1. 第 2 节"影响面"所有"受影响文件（已存在）"均为 `—`——这不是搜索失败，是基线不存在。
2. 第 2 节"预计新增文件"列**全部标注为"建议"**，最终路径由 H2 架构 + H3 详细设计决定。
3. 整张表的置信度天然倾向 `low / medium`——没有现存代码可作为 `high` 直接证据；本约束已在 §4 显式记入。
4. 不触发"REQ 与 AGENTS.md 禁区冲突"阻塞——因为没有 `AGENTS.md`。

H1 ↔ H3 衔接的关键事实：

- **H1 已锁的技术倾向（不视为 H2 决策）**：[OQ-011 closed](./open-questions.md) 视觉风格参考 Ant Design Pro；[OQ-013 closed](./open-questions.md) 编排画布候选 React Flow。
- **H1 已锁的范围排除**：[OQ-015 closed](./open-questions.md) v1 仅 zh-CN（不引入 i18n 框架）；[OQ-020 closed](./open-questions.md) v1 不做审计导出。
- **H1 已签字接受的风险**：[OQ-006 closed §A](./open-questions.md) 范围全做的工期风险；[OQ-002 closed §B](./open-questions.md) v1 不设性能底线。

---

## 1. 阅读约定

- **REQ-NNN / NFR-NNN / EX-NNN** 编号取自 [requirements.md §5.1 / §6 / §7](./requirements.md)。
- **UI-NNN** 编号取自 [ui-spec.md](./ui-spec.md)；**UF-NNN** 取自 [user-flow.md](./user-flow.md)；**AC-NNN** 取自 [acceptance-criteria.md](./acceptance-criteria.md)。
- 本文最初产于绿地阶段；未标记“已实现”的 `apps/desktop/`、`src/server/Inkwell.*` 仍是历史建议路径，不作为当前拓扑事实。当前目录以 [AGENTS.md §3](../../AGENTS.md) 与对应 H3 文档为准；已实现条目应使用仓库真实路径。
- **置信度判定**：
  - `high` = 代码中找到直接证据（绿地状态下不可能命中，全文无 `high`）。
  - `medium` = 上游已有明确决策（OQ closed / REQ 验收口径明确）→ 建议路径具备语义支撑。
  - `low` = 路径建议依赖 H2 架构选型（DB Provider、向量库、协议选型）→ 待 H2 锁后回写。

---

## 2. 影响面（按 REQ / NFR 逐条）

### 2.1 REQ-001 用户登录

- **受影响模块**：`Inkwell.Abstractions.Auth`、`Inkwell.Core.Auth`、EF Core User 持久化、WebApi Session Authentication、Electron 登录与会话 IPC
- **已实现文件**：`src/core/Inkwell.Abstractions/Auth/`、`src/core/Inkwell.Core/Auth/`、`src/core/Inkwell.WebApi/{Authentication,Controllers/AuthController.cs}`、`src/app/desktop/src/features/auth/`、`src/app/desktop/electron/{main.ts,preload.ts}`
- **测试**：`tests/Inkwell.Core.Tests/Auth/AuthServiceTests.cs`、`tests/Inkwell.WebApi.Tests/Controllers/AuthControllerTests.cs`、`src/app/desktop/tests/login.spec.ts`
- **风险**：仍不提供自助注册 / SSO / 公网 OAuth；初始 Admin 依赖安全配置 Seed 密码，后续账号由 Admin 创建
- **置信度**：high（已实现并验证）

### 2.2 REQ-002 Agent 列表与基础管理

- **建议受影响模块**：后端 Agent CRUD + 共享；客户端 Agent 库 / 列表 / 详情
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.Agents/`
  - 客户端：`apps/desktop/src/features/agent-library/`、`apps/desktop/src/features/agent-detail/`
- **建议测试**：后端集成测试（Owner / 团队共享 / 我使用过 三档过滤）+ 客户端 E2E
- **风险**：[OQ-010 closed §C](./open-questions.md) 三档 tab 信息架构最复杂；"我使用过"语义需 H3 决定（与对话历史表关联）
- **置信度**：medium

### 2.3 REQ-003 Agent 基础属性

- **建议受影响模块**：后端 Agent 基础属性表；客户端 [UI-004 §4.3.1](./ui-spec.md)
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.Agents/`（基础属性字段落入该模块，具体表 / DDL 由 H3 决定）
  - 客户端：`apps/desktop/src/features/agent-detail/sections/BasicInfo.tsx`
- **建议测试**：边界值单元测试（名称 1 ~ 50 字 / 描述 ≤ 500 字 / 头像默认值）
- **风险**：头像未上传时默认值"首字母圆形占位"（[OQ-019 closed §A](./open-questions.md)）需在客户端与后端列表一致渲染
- **置信度**：medium

### 2.4 REQ-004 Instructions / System Prompt

- **建议受影响模块**：后端 Instructions 存储；客户端 [UI-004 §4.3.2](./ui-spec.md)
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：与 REQ-003 同模块 `src/server/Inkwell.Agents/`
  - 客户端：`apps/desktop/src/features/agent-detail/sections/Instructions.tsx`
- **建议测试**：单元测试（超长警告但不阻断）
- **风险**：32 K 字符警告阈值是 H3 决策；REQ-004 字面"无字数硬上限"
- **置信度**：low

### 2.5 REQ-005 模型选择

- **建议受影响模块**：后端模型注册表 + 路由；客户端 [UI-004 §4.3.3](./ui-spec.md)
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.Models/Providers/`（建议每家模型一个 provider 子模块：AzureOpenAI / OpenAI / Claude / Qwen / 智谱）
  - 客户端：`apps/desktop/src/features/agent-detail/sections/ModelSelection.tsx`
- **建议测试**：后端集成测试（v1 至少 Azure OpenAI 可用 + 其他厂商接入位预留）
- **风险**：v1 必须支持 Azure OpenAI；其他厂商仅占接入位 → 预留接口设计需在 H3 锁定
- **置信度**：medium

### 2.6 REQ-006 模型参数配置

- **建议受影响模块**：与 REQ-005 同模块；客户端 [UI-004 §4.3.4](./ui-spec.md)
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：与 REQ-005 同模块
  - 客户端：`apps/desktop/src/features/agent-detail/sections/ModelParams.tsx`
- **建议测试**：单元测试（默认值回退）
- **风险**：temperature / top_p 等参数语义在不同厂商 SDK 下需做适配映射
- **置信度**：low

### 2.7 REQ-007 Function Calling / 工具调用

- **建议受影响模块**：后端工具注册表 + 调用代理；客户端 [UI-004 §4.3.5](./ui-spec.md) 工具区段
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.Tools/`（注册中心 + 调用代理）
  - 客户端：`apps/desktop/src/features/agent-detail/sections/Tools.tsx`
- **建议测试**：后端集成测试（Function Calling 协议适配 + 错误注入）
- **风险**：EX-003 工具失败注入对话上下文 → 与 Microsoft Agent Framework 的 ToolInvocation 契约挂钩，H3 决定具体协议
- **置信度**：medium

### 2.8 REQ-008 Agent Skills

- **建议受影响模块**：后端 Skill 加载（v1 静态）；客户端 [UI-004 §4.3.6](./ui-spec.md) Skills 区段
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.Skills/`（仅静态加载，**不**含执行 / 沙箱）
  - 客户端：`apps/desktop/src/features/skill-upload/`（前置拒收 `scripts/`）
- **建议测试**：后端单元测试（SKILL.md 解析 / `scripts/` 拒收）+ 客户端 E2E（拒收文案）
- **风险**：EX-008 Skill 加载失败的运行时表现需与 Agent 推理流协同；v1 不预留 `ISkillExecutor` 抽象
- **置信度**：medium

### 2.9 REQ-009 知识库 / RAG

- **建议受影响模块**：后端文件解析管线 + 知识库；客户端 [UI-004 §4.3.7](./ui-spec.md) 知识库区段
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.KnowledgeBase/`（文件解析 + chunk + embedding + 检索）
  - 客户端：`apps/desktop/src/features/agent-detail/sections/KnowledgeBase.tsx`
- **建议测试**：后端集成测试（PDF / Word / Markdown / 纯文本解析）
- **风险**：向量库选型 + DB Provider 切换边界（待 H2 Q-A4-followup）→ 此条 H3 才能定具体存储路径
- **置信度**：low

### 2.10 REQ-010 多轮对话与长期记忆

- **建议受影响模块**：后端长期记忆策略；客户端 [UI-004 §4.3.8](./ui-spec.md)
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.Memory/`（开 / 关 / 摘要式三策略）
  - 客户端：`apps/desktop/src/features/agent-detail/sections/LongTermMemory.tsx`
- **建议测试**：单元测试（关闭语义 / 摘要触发条件）
- **风险**：[OQ-008 closed §C](./open-questions.md)：与对话历史表的关系推迟到 H3；本表不预设"衍生 vs 独立"
- **置信度**：low

### 2.11 REQ-011 触发器

- **建议受影响模块**：后端触发器（cron + webhook）；客户端 UI-006 触发器弹窗
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.Triggers/`（CronScheduler / WebhookEndpoint / SecretStore）
  - 客户端：`apps/desktop/src/features/orchestration/Trigger.tsx`
- **建议测试**：后端集成测试（cron 表达式校验 / webhook 入站 + Secret 一次性显示）
- **风险**：Webhook Secret 一次性显示策略需要后端 + 客户端协同实现"掩码态再访问"
- **置信度**：medium

### 2.12 REQ-012 多 Agent 协作 / 编排

- **建议受影响模块**：后端 DAG 编排执行（基于 Microsoft Agent Framework Workflows）；客户端 UI-006 编排画布
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.Orchestrations/`（DAG schema / 节点 / 版本锁定 / 执行历史）
  - 客户端：`apps/desktop/src/features/orchestration/Canvas.tsx`（[OQ-013 closed](./open-questions.md) React Flow）
- **建议测试**：后端单元测试（无循环 / 无未绑定 / 节点版本锁）+ 客户端 E2E（拖拽）
- **风险**：EX-007 死循环 / 超时强制终止 → 与执行引擎挂钩；React Flow 仅是 H2 候选库
- **置信度**：medium

### 2.13 REQ-013 公开 API / Webhook 暴露

- **建议受影响模块**：后端公开 API 鉴权 + Token；客户端 [UI-004 §4.3.10](./ui-spec.md) 公开 API 区段
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.PublicApi/`（Token 生成 / 校验 / 撤销 / 审计）
  - 客户端：`apps/desktop/src/features/agent-detail/sections/PublicApi.tsx`
- **建议测试**：后端集成测试（OQ-004 单 Token 模型 / 旧 Token 401）+ 客户端 E2E
- **风险**：[OQ-004 closed §A](./open-questions.md) 单 Token：调用方切换有窗口期，需在审计中可见
- **置信度**：medium

### 2.14 REQ-014 调试 / 评测

- **建议受影响模块**：后端 trace 写入 + 评测；客户端 UI-007 调试页 + UI-005 工具栏调试入口
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.Traces/`（trace 持久化 / 检索 / 重放）
  - 客户端：`apps/desktop/src/features/debug/`、`apps/desktop/src/features/eval/`
- **建议测试**：后端集成测试（trace 字段完整性 / 重放等价）
- **风险**：trace 视觉形态 [OQ-018 closed §D](./open-questions.md) 推迟到原型；字段已确定
- **置信度**：medium

### 2.15 REQ-015 版本管理

- **建议受影响模块**：后端版本管理；客户端 UI-008 版本页
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.Versioning/`（版本生成 / diff / 回滚 / 与编排锁定的关系）
  - 客户端：`apps/desktop/src/features/version/`
- **建议测试**：后端集成测试（回滚生成新版 / 编排引用 latest 不受历史回滚影响）
- **风险**：破坏性回滚的字段判定推迟到 H3
- **置信度**：low

### 2.16 REQ-016 多模态输入

- **建议受影响模块**：后端 ASR 接入（Azure Speech）+ 多模态分发；客户端 UI-005 输入区
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.Multimodal/`（语音 / 图片 / 文档预处理；ASR 子模块依 [OQ-003 closed §A](./open-questions.md)）
  - 客户端：`apps/desktop/src/features/chat/inputs/Voice.tsx`、`Image.tsx`、`Document.tsx`
- **建议测试**：后端集成测试（Azure Speech 调用 + 失败降级）+ 客户端 E2E（录音 / 拒收）
- **风险**：EX-004 模型不支持视觉时上传组件前置拒收的判定需要后端"模型能力清单"；麦克风 / 通知权限两端差异
- **置信度**：medium

### 2.17 REQ-017 Admin 用户管理

- **受影响模块**：`Inkwell.Abstractions.Auth` 契约与 User Model、`Inkwell.Core.Auth` 业务规则、EF Core User 持久化、WebApi Auth 路由与策略、Electron IPC 与 UI-009；撤销共享仍由 `Inkwell.Core.Agents` 负责
- **已实现文件**：
  - 后端：`src/core/Inkwell.Abstractions/Auth/`、`src/core/Inkwell.Core/Auth/AuthService.cs`、`src/core/Inkwell.WebApi/{Authentication,Controllers/AuthController.cs}`、`src/core/providers/Persistence/Inkwell.Persistence.EFCore*/Migrations/`
  - 客户端：`src/app/desktop/src/features/users/`、`src/app/desktop/src/features/auth/change-password-modal.tsx`、`src/app/desktop/{electron/main.ts,electron/preload.ts}`
- **测试**：`tests/Inkwell.Core.Tests/Auth/AuthServiceTests.cs` 覆盖自动锁定、会话版本、临时密码与状态正交；`tests/Inkwell.WebApi.Tests/Controllers/AuthControllerTests.cs` 覆盖 Admin actor 传递及不存在主动锁定接口；Electron E2E 覆盖普通用户不可见系统管理入口
- **风险**：初始单 Admin 被锁定时仍需 SQL / 管理脚本运维兜底；临时密码只能显示一次且不得进入日志
- **置信度**：high（已实现并通过 build / test / E2E 验证）

### 2.18 NFR-001 联网要求

- **建议受影响模块**：全局客户端连通性提示（EX-001）
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 客户端：`apps/desktop/src/shared/network/ConnectivityWatcher.ts`
  - 后端：`src/server/Inkwell.Health/`
- **建议测试**：客户端 E2E（断网 toast / 写操作禁用）
- **风险**：v1 客户端**禁止**降级到本地缓存对话
- **置信度**：medium

### 2.19 NFR-002 客户端跨平台

- **建议受影响模块**：客户端跨平台桌面壳
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - `apps/desktop/electron/`（主进程 / 预加载 / 自动更新）
  - CI 矩阵：Win11 + macOS 12+ Apple Silicon
- **建议测试**：跨平台 E2E（两端等价）
- **风险**：麦克风 / 通知 / 截屏权限两端差异；macOS Cmd ↔ Win Ctrl 等价（[OQ-022 closed §A](./open-questions.md)）
- **置信度**：medium

### 2.20 NFR-003 客户端自动锁定

- **建议受影响模块**：客户端自动锁定调度；后端在途任务保活
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 客户端：`apps/desktop/src/features/lock/`（监听窗口失焦 + idle）
  - 后端：在途任务结果累积位（建议落入对话 / Trace 表的 `pending` 队列）
- **建议测试**：客户端 E2E（5 分钟空闲 / 失焦 → 锁屏）+ 集成测试（OQ-017 在途任务跨锁屏存活）
- **风险**：**W-003 残留**——[requirements.md §6 NFR-003 / §11 NFR-003 验收口径](./requirements.md) 字面**未补 OQ-017 特例**；下游 ui-spec / user-flow / acceptance 已写入特例。建议在 H2 risk-analysis.md 留一条 RISK
- **置信度**：medium

### 2.21 NFR-004 审计日志

- **建议受影响模块**：后端审计日志写入与查询；客户端 [UI-009 §9.4](./ui-spec.md)
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.AuditLog/`（事件类型表 + 索引 + 检索）
  - 客户端：`apps/desktop/src/features/admin/sections/AuditLog.tsx`
- **建议测试**：后端集成测试（事件类型穷举 / 大数据集分页）+ 客户端 E2E（筛选 / 详情）
- **风险**：[OQ-020 closed §B](./open-questions.md)：v1 不做导出；合规导出走后端运维 SQL
- **置信度**：medium

### 2.22 NFR-005 对话历史持久化

- **建议受影响模块**：后端对话表全量持久化；客户端多端一致拉取
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：
  - 后端：`src/server/Inkwell.Conversations/`
  - 客户端：依赖 REQ-002 + UI-005
- **建议测试**：后端集成测试（多端登录看到同一历史）
- **风险**：客户端**不**做本地缓存对话（NFR-001）；与 [OQ-014 closed §A](./open-questions.md) 历史会话侧栏的拉取节奏挂钩
- **置信度**：medium

### 2.23 NFR-006 数据出境合规

- **建议受影响模块**：后端调用记录（不引入脱敏管线）
- **受影响文件（已存在）**：—
- **预计新增文件（建议）**：复用 `Inkwell.AuditLog` + 模型调用 trace；不新增脱敏管线
- **建议测试**：集成测试（DPA 已签厂商可正常调用）
- **风险**：[OQ-001 closed §A](./open-questions.md) 风险全部前移到厂商合同与人工审核
- **置信度**：medium

### 2.24 异常路径 EX-001 ~ EX-009

EX 不单独占节，所有 EX 均为对应 REQ 的错误路径，已在上述各条"风险"行内引用。完整 EX 清单见 [requirements.md §7](./requirements.md) 与 [user-flow.md UF-014](./user-flow.md)。本文件不重述。

---

## 3. 模块依赖摘要

> **绿地状态特别说明**：本节描述的是 H2 起步时**建议建立**的模块拓扑，不是当前事实。

### 3.1 建议的模块拓扑（输入给 H2-ArchitectAdvisor）

```text
apps/desktop/                      Electron 客户端
  electron/                        主进程 + 预加载 + 自动更新
  src/
    features/
      auth/                        REQ-001 / NFR-003
      lock/                        NFR-003 + OQ-017 在途任务保活
      agent-library/               REQ-002（OQ-010 三档 tab）
      agent-detail/                REQ-002 ~ REQ-013（OQ-012 卡片网格）
      chat/                        REQ-010 + REQ-016（OQ-014 历史会话侧栏）
      orchestration/               REQ-011 + REQ-012（OQ-013 React Flow 候选）
      debug/                       REQ-014（OQ-018 trace 视觉待原型）
      eval/                        REQ-014（评测样本 / 重放）
      version/                     REQ-015
      admin/                       REQ-017
      skill-upload/                REQ-008 前置拒收
    shared/
      network/                     NFR-001
      design-system/               OQ-011 沿用 Ant Design Pro 风格

src/server/                        ASP.NET Core 后端（待 H2 锁定）
  Inkwell.Auth/                    REQ-001 / REQ-017 / NFR-003
  Inkwell.Agents/                  REQ-002 ~ REQ-006
  Inkwell.Models/                  REQ-005 / REQ-006
  Inkwell.Tools/                   REQ-007
  Inkwell.Skills/                  REQ-008（v1 静态加载）
  Inkwell.KnowledgeBase/           REQ-009
  Inkwell.Memory/                  REQ-010
  Inkwell.Triggers/                REQ-011
  Inkwell.Orchestrations/          REQ-012（基于 Microsoft Agent Framework Workflows）
  Inkwell.PublicApi/               REQ-013
  Inkwell.Traces/                  REQ-014
  Inkwell.Versioning/              REQ-015
  Inkwell.Multimodal/              REQ-016（含 Azure Speech ASR）
  Inkwell.AuditLog/                NFR-004
  Inkwell.Conversations/           NFR-005
  Inkwell.Health/                  NFR-001 探针

tests/                             待 H2 锁单元 / 集成 / E2E 测试框架
deploy/                            Docker Compose（dev）+ AKS manifests / Helm（prod）
```

### 3.2 直接依赖与被依赖（不列传递依赖）

- **客户端 → 后端**：所有 `apps/desktop/src/features/*` 通过 `apps/desktop/src/shared/network/` 调用后端 API。
- **后端模块间**：
  - `Inkwell.Agents` 被 `Inkwell.Orchestrations / Inkwell.PublicApi / Inkwell.Versioning / Inkwell.Traces` 依赖。
  - `Inkwell.AuditLog` 被几乎所有写操作模块依赖。
- **外部系统**：
  - Azure OpenAI（REQ-005 必需）
  - Azure Speech（REQ-016 / [OQ-003 closed §A](./open-questions.md)）
  - 模型其他厂商按 REQ-005 预留接入位（v1 不真做）。

### 3.3 已知技术债务

无（绿地项目，无既有 `tech-debt-tracker.md`）。

---

## 4. 缺失发现

按 [agents/repo-impact-mapper/AGENT.md §4.3](../../.he/agents/repo-impact-mapper/AGENT.md) 要求：扫描中发现但**不在任何 REQ 内**的潜在缺口或与既有约定冲突的项，单独列出。**不混入第 2 节影响面**——是否补需求由人工决定。

### 4.1 MISS-001 无 `AGENTS.md` / 无 `README.md`

- **描述**：仓库根级缺工程入口文档；[copilot-instructions.md](../../.github/copilot-instructions.md) 引用了"项目身份与技术栈以仓库根 README.md / AGENTS.md 为准"。
- **建议处理**：H2 锁定技术栈后**必须**先建立 `AGENTS.md`（模块边界 / 禁区）+ 顶层 `README.md`（项目门户与索引）。

### 4.2 MISS-002 无 `Directory.Build.props` / 无 `.editorconfig`

- **描述**：[copilot-instructions.md](../../.github/copilot-instructions.md) 硬约束要求 `dotnet test` 与 `dotnet format --verify-no-changes`，但当前无 .NET 解决方案。
- **建议处理**：H5 第一个 TASK 必须是"建立 .NET 工程化骨架 + CI"。

### 4.3 MISS-003 无 `tech-debt-tracker.md`

- **描述**：[.he/docs/tech-debt-gc.md](../../.he/docs/tech-debt-gc.md) 的 GC 流程依赖此文件。
- **建议处理**：不阻塞 H2；任意一条技术债被识别时才需要建立。

### 4.4 MISS-004 NFR-003 字面缺 OQ-017 在途任务特例（W-003）

- **描述**：[requirements.md §6 NFR-003 / §11 NFR-003 验收口径](./requirements.md) 仍是"硬锁定"措辞；[ui-spec.md §2.5 / §5.5](./ui-spec.md)、[user-flow.md UF-002 / UF-005](./user-flow.md)、[acceptance-criteria.md AC-076 ~ AC-079](./acceptance-criteria.md) 已落入特例。
- **建议处理**：二选一——
  - (a) 由 `H1-RequirementsInterviewer` 在 §9 上游决策追加一条特例说明
  - (b) H2 [risk-analysis.md](../03-architecture/risk-analysis.md) 留一条 RISK 记录文字漂移

### 4.5 MISS-005 DB Provider 切换边界未锁

- **描述**：OQ-010 三档 tab + OQ-014 历史会话侧栏 + REQ-009 知识库 + REQ-014 trace 都是潜在的"高写入 + 检索"场景；EF Core Provider 切换 + 向量库选型边界（H2 Q-A4-followup）尚未答清。
- **建议处理**：H2 ADR-004 必须把"InMemory / SQL Server / PostgreSQL + 向量库"边界一次性锁死。

### 4.6 MISS-006 客户端↔后端协议的"在途任务跨锁屏存活"机制

- **描述**：NFR-003 + OQ-017 closed 要求锁屏期间录音 / 上传 / 流式继续；这要求协议层（AG-UI）支持 run 续订或 cursor 重连，本 H1 未涉及。
- **建议处理**：H2 ADR-012（客户端↔后端协议）必须明确选 AG-UI A / B / C 中的一种（见 H2-ArchitectAdvisor 待答 Q-A6-followup）。

### 4.7 MISS-007 Azure Speech ASR 失败降级路径

- **描述**：[OQ-003 closed §A](./open-questions.md) 选 Azure Speech；但若 Azure Speech 不可用，UI 当前无降级路径表述（语音→拒收？语音→报错？）。
- **建议处理**：由 `H1-RequirementsInterviewer` 在下一轮补一条 EX，或 H3 在 [Multimodal 模块详细设计](../04-detailed-design/) 里显式声明。

### 4.8 MISS-008 OQ-018 trace 视觉形态推迟到原型阶段

- **描述**：[OQ-018 closed §D](./open-questions.md) 推迟；[copilot-instructions.md §2](../../.github/copilot-instructions.md) 未列 PrototypeReviewer 入口（实际有 [.he/agents/prototype-reviewer/](../../.he/agents/prototype-reviewer/) 已就绪）。
- **建议处理**：不阻塞 H2 / H3；待原型出来由 PrototypeReviewer 回写 ui-spec.md §7。

### 4.9 MISS-009 M3 多用户后端的并发 / 性能基线缺失

- **描述**：[OQ-002 closed §B](./open-questions.md) v1 不设性能底线；但用户量级 ~100 / 单用户 Agent 数不限 / 文档总量级未定 → H2 / H3 没有任何"何时算实现完成"的客观判据。
- **建议处理**：H2 architecture.md §性能假设里至少给出"软目标 + 风险"，H4 测试用例不强求性能。

### 4.10 MISS-010 i18n 资产管理在 v1 不存在

- **描述**：[OQ-015 closed §A](./open-questions.md) v1 仅 zh-CN；但客户端文案散落在各 feature 目录会让 v2 国际化重做成本上升。
- **建议处理**：建议 H2 前端架构 ADR 至少声明"v1 文案集中维护位置（即使硬编码也用 i18n key 风格）"，避免 v2 翻全部文案。

> 上述 10 条 MISS 都不阻塞 H1 → H2 转阶段。MISS-001 / MISS-002 是 H2 / H5 起步必须做的工程化前置；MISS-004 是 W-003 显式残留风险；其余是 H2 / H3 阶段的合理输入。

---

## 5. 阻塞返回

按 [io-contracts.md §5](../../.he/agents/_shared/io-contracts.md)：本次扫描**无阻塞**。

- `requirements.md` 状态不达标：未触发（`status: reviewed`，`reviewers: [Inkwell]`）。
- 关键 REQ 与 `AGENTS.md` 禁区冲突：未触发（无 `AGENTS.md`，无禁区）。
- 仓库根目录下找不到任何源码：**形式上触发但本质不是阻塞**——这是合法的绿地状态而非克隆不完整；本文件 §0 已显式声明，并将 low 占比高的原因记入 §4。

---

## 6. 给 H2-ArchitectAdvisor 的输入摘要

按 [copilot-instructions.md §2](../../.github/copilot-instructions.md)：H2 起步建议把以下三段直接复制到 architecture.md / tech-selection.md 输入区。

### 6.1 必须复用 vs 全新建立

- **必须复用**：无（绿地项目）。
- **全新建立**：所有模块（见 §3.1 拓扑）。
- **可借鉴的外部代码库（非锁定，仅作为 H2 ADR 的输入候选）**：
  - [microsoft/agent-framework dotnet](../../../../microsoft/agent-framework/dotnet/) —— 与已锁的 Microsoft Agent Framework 配套，含 `Microsoft.Agents.AI` / `Microsoft.Agents.AI.Hosting` / `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` / `Microsoft.Agents.AI.AGUI` / `Microsoft.Agents.AI.Workflows` / `Microsoft.Agents.AI.DurableTask`，可直接覆盖 REQ-005 / REQ-007 / REQ-008 / REQ-012 / REQ-014 大部分基础能力。
  - Ant Design Pro 视觉风格（OQ-011 候选）。
  - React Flow（OQ-013 编排画布候选）。

### 6.2 已锁的 H1 决策清单（H2 ADR 起草时不再质疑）

- [OQ-001 closed §A](./open-questions.md) 数据可原文发送 + 不做脱敏
- [OQ-002 closed §B](./open-questions.md) v1 不设性能底线
- [OQ-003 closed §A](./open-questions.md) ASR = Azure Speech 后端独立服务
- [OQ-004 closed §A](./open-questions.md) 单 Token 模型
- [OQ-005 closed §A](./open-questions.md) 用户名 / 密码 + 后端运维 SQL 创建
- [OQ-006 closed §A](./open-questions.md) 范围全做风险已签字接受
- [OQ-007 closed §C](./open-questions.md) v1 即出最小管理员页（当前字段 `is_admin=true`）
- [OQ-008 closed §C](./open-questions.md) 长期记忆与对话表关系推迟到 H3
- [OQ-009 closed §B](./open-questions.md) Win11 + macOS 12+ Apple Silicon
- [OQ-010 closed §C](./open-questions.md) 三档 tab
- [OQ-011 closed §A](./open-questions.md) 顶栏 + 左侧 nav + 主区（Ant Design Pro 风格候选）
- [OQ-012 closed §A](./open-questions.md) 卡片网格
- [OQ-013 closed §A](./open-questions.md) 可视化 DAG 画布（React Flow 候选）
- [OQ-014 closed §A](./open-questions.md) 历史会话侧栏
- [OQ-015 closed §A](./open-questions.md) v1 仅 zh-CN
- [OQ-016 closed §B](./open-questions.md) Token 弹层阻断关闭
- [OQ-017 closed §B](./open-questions.md) 在途任务保留到完成 / 失败
- [OQ-018 closed §D](./open-questions.md) trace 视觉推迟到原型
- [OQ-019 closed §A](./open-questions.md) 首字母圆形占位
- [OQ-020 closed §B](./open-questions.md) v1 不做审计导出
- [OQ-021 closed §A](./open-questions.md) 骨架 + 文案（无插画）
- [OQ-022 closed §A](./open-questions.md) Cmd ↔ Ctrl 双端等价 + 不提供自定义

### 6.3 必须由 H2 锁定的开放点

- DB Provider 切换 + 向量库选型边界（MISS-005）
- 客户端↔后端协议的在途任务跨锁屏存活机制（MISS-006）
- 性能软目标 + 风险接受口径（MISS-009）
- W-003 NFR-003 字面修补 vs RISK 记录二选一（MISS-004）

---

> **本文件交付后下一步**：
>
> 1. 由人工 review 后把 frontmatter `status: draft` → `reviewed`，并补 `reviewers`。
> 2. `H2-ArchitectAdvisor` 解除 blocked 状态后，按 §6 摘要起草 H2 五件套（[architecture.md](../03-architecture/architecture.md) / [tech-selection.md](../03-architecture/tech-selection.md) / [risk-analysis.md](../03-architecture/risk-analysis.md) / [adr/](../03-architecture/adr/) / [open-questions-arch.md](../03-architecture/open-questions-arch.md)）。
> 3. 本文件随后由 H2-ArchitectAdvisor 在 ADR 编号确定后回写"建议路径"为正式路径——本回写动作不视为本 Agent 职责。
