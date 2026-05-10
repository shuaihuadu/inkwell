# Inkwell Agent 平台 · H1 可交互原型

> 阶段：H1（需求 + 原型）  
> 受众：`H1-PrototypeReviewer`、UI / 交互评审人  
> 这份原型 **严格按照** [docs/01-requirements/ui-spec.md](../../docs/01-requirements/ui-spec.md) /
> [user-flow.md](../../docs/01-requirements/user-flow.md) /
> [acceptance-criteria.md](../../docs/01-requirements/acceptance-criteria.md) 翻译成可点的页面。
> **它不是 H5 正式实现的雏形**，与正式实现的代码风格 / 结构 **没有追溯关系**。

## 1. 启动

```bash
# 当前目录就是 prototypes/inkwell-agent-platform/
npm install
npm run dev
# → http://127.0.0.1:5173/
```

> Node ≥ 18；推荐 Node 20+。原型里的全部数据都是 mock，不会调任何真实后端。

## 2. 路由表与状态深链

所有页面都用 `?state=<value>` 切换状态（深链，可直接复制 URL 给评审人）。
应用使用 `HashRouter`，所以 URL 形如 `http://127.0.0.1:5173/#/ui-003?state=empty-mine`。

| UI-NNN                        | 路由                   | 状态深链示例                                                                                                                                                                    |
| ----------------------------- | ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| UI-001 登录页                 | `/ui-001`              | `?state=default` / `submitting` / `failed-401` / `failed-locked` / `failed-rate` / `offline`                                                                                    |
| UI-002 锁定页（独立路由演示） | `/ui-002`              | —                                                                                                                                                                               |
| UI-003 Agent 库               | `/ui-003`              | `?state=data` / `loading` / `empty-mine` / `empty-shared` / `empty-used` / `error`                                                                                              |
| UI-004 Agent 配置             | `/ui-004?id=agent-001` | `?state=editing` / `new-draft` / `readonly` / `submitting` / `submit-failed` / `submit-success`                                                                                 |
| UI-005 对话                   | `/ui-005?id=agent-001` | `?state=data` / `loading-history` / `empty-new` / `streaming` / `tool-calling` / `recording` / `transcribing` / `file-parsing` / `error-net` / `error-model` / `image-incompat` |
| UI-006 编排                   | `/ui-006`              | `?state=editing` / `loading` / `empty` / `validation-failed` / `running` / `timeout-terminated` / `error`                                                                       |
| UI-007 调试 / Trace           | `/ui-007`              | `?state=data` / `loading` / `empty` / `replaying` / `error`                                                                                                                     |
| UI-008 版本                   | `/ui-008`              | `?state=default` / `loading` / `rolling-back` / `rollback-success` / `rollback-failed`                                                                                          |
| UI-009 Admin                  | `/ui-009`              | `?state=data` / `loading` / `empty` / `error`（默认 owner 角色看到「无权限」页；切到 Admin 才看到三档 tab）                                                                     |

## 3. 全局演示开关（顶栏）

| 开关                       | 作用                                 | 关联                        |
| -------------------------- | ------------------------------------ | --------------------------- |
| 「离线演示」Switch         | 切顶栏 Badge 在线/离线               | EX-001                      |
| 「锁定演示」Switch         | 立即触发锁定遮罩（NFR-003 / EX-006） | UI-002 / OQ-017             |
| 用户菜单（默认 owner-bob） | 三个角色切换 + 登出 + 管理入口       | REQ-001 / REQ-002 / REQ-017 |

角色清单（mock）：

- **Member（非 Owner）**：alice — 演示「我使用过」/ 只读视图
- **Member（Agent Owner）**：owner-bob — 演示「我的」/ 编辑视图（默认）
- **Admin（is_super=true）**：sa-carol — 解锁 UI-009 Admin 入口

## 4. 关键决策落点

> 来自 `docs/07-reviews/2026-05-08-openquestion-discussion.md` 已 closed 的 OQ；引用按 ui-spec 一字一致。

- **OQ-004 closed** · 一个 Agent 只允许 1 个有效 Token；新建即作废旧的 → UI-004 公开 API 区段 + 二次确认弹窗
- **OQ-008 closed** · 长期记忆三档（关 / 开-保留全文 / 开-摘要式） → UI-004 长期记忆区段
- **OQ-010 closed** · Agent 库 三档 tab：我的 / 团队共享 / 我使用过 → UI-003
- **OQ-011 closed** · 主外壳：顶栏 + 左侧 nav + 主区（ProLayout `layout="mix"`） → `src/layouts/AppLayout.tsx`
- **OQ-012 closed** · Agent 库 卡片网格 3–4 列响应式（xs/sm/md/lg/xl 断点） → UI-003
- **OQ-013 closed** · 编排画布：可视化 DAG（@xyflow/react） → UI-006
- **OQ-014 closed** · 对话页历史侧栏 默认展开、可折叠，`+ 新建会话`置顶，时间倒序 → UI-005
- **OQ-015 closed** · 文案语言：简体中文，与 ui-spec 一字一致
- **OQ-016 closed** · Token 一次性弹层勾选「我已妥善保存此 Token」才能关 → UI-004 Token Modal
- **OQ-017 closed** · 锁定中的在途任务 v1 完成自然结束、不允许新发起 → LockOverlay 文案 + UI-005 输入区禁用
- **OQ-018 closed** · trace 视觉形态推迟到原型阶段决定 → UI-007 采用「时序树」候选 A，由 PrototypeReviewer 复核
- **OQ-019 closed** · 头像 fallback：字符哈希分配背景色 + 首字符 → `src/components/AvatarFallback.tsx`
- **OQ-020 closed** · v1 不提供审计日志导出按钮 → UI-009 审计 tab
- **OQ-021 closed** · 各页空状态采用统一极简插画 + 简短文案 + 主行动按钮 → `EmptyHint`
- **OQ-022 closed** · 键盘快捷键：Enter 发送 / Shift+Enter 换行；macOS Cmd ↔ Windows Ctrl 等价 → UI-005 输入框 placeholder

## 5. 截屏

`screenshots/` 目录下已用 Playwright 在真实运行的原型上抓了 58 张截图，覆盖每个 UI-NNN 的全部状态以及 4 个关键交互弹层。命名规则：

```
UI-NNN-<state>.png       页面状态
UI-NNN-<scene>.png       关键弹层 / 交互（如 UI-004-token-modal.png、UI-006-webhook-secret-modal.png）
Lock-overlay.png         全局锁定遮罩
```

如需重新抓屏，启动 dev server 后用浏览器 / Playwright 按上方路由表逐个访问即可。

## 6. 非范围 / 已知限制

- 只演示 UI-spec 描述过的页面 / 状态 / 字段；任何 ui-spec 未列的「我顺手加上」都不会出现。
- 真实后端、真实模型、真实 Skill 加载 / RAG 解析 一律 mock，不调任何外部服务。
- 视觉、动效、多语言、无障碍、移动端适配在 ui-spec 中均未列详，本原型不主动补。
- 原型源码与 H5 正式实现的代码风格 / 结构无追溯关系，仅为评审用。

## 7. 目录结构

```
prototypes/inkwell-agent-platform/
├── package.json
├── vite.config.ts
├── tsconfig.json / tsconfig.node.json
├── index.html
├── coverage.md            ← UI-NNN × 状态映射，由 PrototypeReviewer 直接消费
├── README.md              ← 本文件
├── screenshots/           ← 真实运行的原型抓屏
└── src/
    ├── main.tsx           ← 入口；ConfigProvider(zhCN) + AntdApp
    ├── App.tsx            ← HashRouter + LockOverlay + 9 个路由
    ├── styles.css         ← 全局样式（消息气泡 / DAG 画布 / 锁定遮罩）
    ├── AppContext.tsx     ← 角色 / 锁定 / 离线 演示用 context
    ├── types/             ← 全局 TS 类型
    ├── mocks/             ← agents.ts / conversations.ts / index.ts（其他）
    ├── hooks/
    │   └── useStateQuery.ts  ← ?state=... 深链 hook
    ├── components/
    │   ├── StateSwitcher.tsx
    │   ├── EmptyHint.tsx
    │   └── AvatarFallback.tsx
    ├── layouts/
    │   ├── AppLayout.tsx     ← ProLayout 外壳（顶栏 + 左 nav）
    │   └── LockOverlay.tsx   ← NFR-003 全屏锁定遮罩
    └── pages/
        ├── UI001Login.tsx
        ├── UI002Lock.tsx
        ├── UI003AgentLibrary.tsx
        ├── UI004AgentConfig.tsx
        ├── UI005Conversation.tsx
        ├── UI006Orchestration.tsx
        ├── UI007TraceDebug.tsx
        ├── UI008Version.tsx
        └── UI009Admin.tsx
```
