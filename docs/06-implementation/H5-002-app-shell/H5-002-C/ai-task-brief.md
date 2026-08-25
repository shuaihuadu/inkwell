---
id: H5-002-C
title: Desktop 后台连接状态与全局错误处理 · AI 任务简报
stage: H5
document_type: task-brief
status: draft
authors:
  - name: GitHub Copilot
    role: agent
reviewers: []
created: 2026-08-25
updated: 2026-08-25
upstream:
  - NFR-001
  - EX-001
  - H5-002
tests:
  - AC-071
  - AC-072
downstream: []
---

<!-- markdownlint-disable MD025 -->

# H5-002-C · Desktop 后台连接状态与全局错误处理任务简报

> 本文件严格按照 `docs/_templates/implementation-task-brief.template.md` 编写，可在人工确认范围后交给 `h5-coding-executor`。
>
> `status` / `reviewers` 由 Owner 人工维护，Agent 不代签。

## 1. 任务目标

让 Desktop 依据 WebApi `/healthz` 的真实可达性展示“后台服务正常 / 重连中 / 后台服务异常”，在连接异常时显示全局错误条并拒绝新的写请求；同时把普通 API 的 401、429 与 5xx 映射为一致的全局行为。

## 2. 不做范围

- 不新增或修改 WebApi 健康端点；复用现有匿名 `GET /healthz`。
- 不实现离线数据缓存、离线写入队列或自动重放。
- 不改变聊天流内错误、停止和重试语义；继续由 H5-005 处理。
- 不实现系统 suspend/resume 后的协议级 Run 恢复。
- 不逐页重写现有页面级和字段级错误提示。

## 3. 上游设计引用

- `AGENTS.md` §3.1～3.3：Electron 主进程与 Renderer 只能通过安全 IPC 通信，v1 禁止本地缓存对话。
- `docs/01-requirements/requirements.md` NFR-001、EX-001：客户端强制联网，后端不可达时不得降级到本地能力。
- `docs/01-requirements/ui-spec.md` §0.2、§10.1、§11：三态连接徽标、全局错误条及错误层级。
- `docs/01-requirements/acceptance-criteria.md` AC-071、AC-072：启动断网和登录后断网行为。
- `docs/06-implementation/H5-002-app-shell/scope.md` §6～9：H5-002-C 边界。
- `prototypes/inkwell-visual-design/src/pages/AppShellExplorer.tsx`：连接徽标和全局错误条的视觉与文案基线。
- `src/core/Inkwell.WebApi/Program.cs`：现有匿名 `GET /healthz`。

## 4. 测试引用

暂无独立 H4 TC；临时以 AC-071、AC-072 和本简报 §9 为验证依据。测试设计缺口记录在 §12。

## 5. 当前基线与问题

### 5.1 当前实现

- `electron/main.ts` 通过统一 `request<T>()` 访问 WebApi，并在普通请求返回 401 时清除认证。
- `workspace-shell.tsx` 已有连接状态徽标位置，但固定显示“后台服务正常”。
- 登录和解锁已有 offline 失败结果，聊天有独立的流内错误模型。
- WebApi 已公开 `GET /healthz`。

### 5.2 待解决问题

1. Desktop 没有真实连接状态探测、重连轮询或 Renderer 状态订阅。
2. 网络异常时普通写请求仍会发出，且所有页面没有统一全局错误条。
3. 普通 API 的 429 与 5xx 没有统一、可本地化的全局错误分类。
4. 顶栏状态与实际 WebApi 可达性无关。

## 6. 允许修改的文件

仅允许修改或新建以下路径：

- `docs/06-implementation/H5-002-app-shell/H5-002-C/**`
- `docs/06-implementation/H5-002-app-shell/scope.md`
- `docs/06-implementation/desktop-implementation-roadmap.md`
- `src/app/desktop/electron/main.ts`
- `src/app/desktop/electron/preload.ts`
- `src/app/desktop/src/shared/network/contracts.ts`
- `src/app/desktop/src/shared/network/desktop-api.ts`
- `src/app/desktop/src/shared/i18n/resources.ts`
- `src/app/desktop/src/features/shell/**`
- `src/app/desktop/src/features/auth/**`
- `src/app/desktop/src/app-shell.tsx`
- `src/app/desktop/src/index.css`
- `src/app/desktop/tests/login.spec.ts`
- `src/app/desktop/electron/*.test.ts`

## 7. 禁止修改

- `src/core/**`、`tests/**` 和数据库 Migration。
- Agent、Conversation、Tool、Skill、Model、Admin 的业务契约与业务规则。
- Electron 安全设置；不得关闭 `contextIsolation` 或开启 `nodeIntegration`。
- 不使用 `navigator.onLine` 代替 WebApi 健康探测。
- 不把后端错误正文直接显示为全局 5xx 文案。

## 8. 实现要求

### 8.1 连接状态

- 定义 Renderer 与主进程共享的 `online | reconnecting | offline` 连接状态契约。
- 主进程以 `/healthz` 为唯一健康依据；启动时立即探测，在线时低频轮询，失败后进入 reconnecting 并使用有界退避重试，连续失败后进入 offline。
- 状态变化通过 preload 暴露的最小 IPC API 广播；新 Renderer 可主动读取当前快照。
- 状态恢复为 online 后自动收起 EX-001 全局错误条。

### 8.2 请求与全局错误

- 普通 API 请求遇到网络异常时更新连接状态；成功响应可恢复 online。
- 非 GET/HEAD 请求在 offline 状态下必须在主进程发出前拒绝，防止绕过 Renderer 门禁。
- 401 继续清除认证状态；登录和解锁保留各自现有局部错误映射。
- 429 形成全局 rate-limited 错误，并在可用时保留 `Retry-After` 秒数。
- 5xx 形成不泄露后端正文的全局 service-unavailable 错误。
- 全局错误事件不得覆盖聊天流内的专用错误展示。

### 8.3 壳层体验

- 顶栏连接徽标严格对齐原型三态颜色和文案。
- 全局错误条位于主窗口内容顶部，不自动消失；EX-001 在恢复 online 后自动收起。
- offline 时通过共享状态向页面提供写操作门禁；至少覆盖登录按钮和现有 AppShell 下的业务提交入口，不影响纯读取、本地导航、登出或锁定。
- 固定 UI 文案同时维护 `zh-CN` 与 `en-US`。

### 8.4 依赖版本策略

- 本任务不新增或升级 npm/NuGet 依赖，复用 Electron、React、Zustand、React Query 与 Ant Design 现有版本。

## 9. 测试要求

1. Electron E2E：启动时 WebApi 不可达，登录页显示 EX-001，登录按钮禁用；服务恢复后状态自动恢复且按钮重新启用。
2. Electron E2E：登录后连接失败，Shell 显示 offline/reconnecting 状态与全局错误条，写操作被拒绝；恢复后错误条收起。
3. Electron E2E：普通请求收到 429 时显示统一限流错误，收到 5xx 时显示统一服务异常，不泄露响应正文。
4. Electron E2E：普通请求收到 401 时沿现有认证状态机返回登录页。
5. 单元测试：连接状态转换、退避边界和结构化错误映射的纯函数或状态逻辑。

测试必须触达真实主进程 IPC/request 路径，不允许只验证 mock 自身。

## 10. 验收命令

按顺序执行：

```shell
npm --prefix src/app/desktop run test
npm --prefix src/app/desktop run lint
npm --prefix src/app/desktop run build
npm --prefix src/app/desktop run test:e2e

dotnet build Inkwell.slnx
git diff --check
```

## 11. 完成标准

- 顶栏状态来自 WebApi 实际可达性，而非静态文案或本机网络接口状态。
- EX-001、401、429、5xx 按 §8 映射，网络恢复可自动清理离线全局错误。
- offline 时新写请求被主进程拒绝，读取与本地导航不被错误禁用。
- §10 全部命令通过，无新增 warning。
- 实际修改文件是 §6 的子集，§7 保持未修改。
- 返回修改文件、验证摘要、偏差和六字段提交信息草稿，但不运行 git 提交命令。

## 12. 风险、假设与待确认项

### 12.1 已知风险

- 瞬时失败若直接标记 offline 会产生状态抖动；通过 reconnecting 中间态和连续失败阈值控制。
- 自动重试不得重放业务写请求；只重试幂等健康探测。
- 认证接口需要保留局部错误语义，不能被全局映射吞掉。

### 12.2 实施假设

- `/healthz` 是进程存活与基本服务可达性的探测依据，不表示所有下游依赖都健康；该语义与现有端点实现一致。
- 全局连接状态由 Electron 主进程持有，避免多个 Renderer 各自启动轮询。
- `Retry-After` 仅用于提示和控制健康探测节奏，不自动重放用户操作。

### 12.3 待 Owner 确认

无。原型、H1 需求与 H5 scope 已明确本任务产品行为。

## 13. H5 交付格式

完成后必须在对话中提供：

1. 修改文件清单。
2. 实际执行的验证命令与输出摘要。
3. 与本简报的偏差及原因；无偏差则明确写“无”。
4. 六字段提交信息草稿：`Design / Tests / Verify / Docs / Risk / Task`。
