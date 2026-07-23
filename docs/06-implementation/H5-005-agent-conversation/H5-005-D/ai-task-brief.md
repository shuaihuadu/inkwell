---
id: H5-005-D
title: Agent 会话停止、错误与锁屏恢复 · AI 任务简报
stage: H5
document_type: task-brief
status: draft
authors:
  - name: GitHub Copilot
    role: agent
reviewers: []
created: 2026-07-21
updated: 2026-07-21
upstream:
  - REQ-010
  - NFR-003
  - ADR-011
  - ADR-012
  - HD-017
tests: []
downstream: []
---

<!-- markdownlint-disable MD025 -->

# H5-005-D · Agent 会话停止、错误与锁屏恢复任务简报

> 本文件严格按照 `docs/_templates/implementation-task-brief.template.md` 编写，可在人工确认范围后交给 `h5-coding-executor`。
>
> `status` / `reviewers` 由 Owner 人工维护，Agent 不代签。

## 1. 任务目标

让 Desktop 用户能够停止正在生成的回复，在模型请求失败时看到错误码、原因和重试入口，并保证锁屏前已经开始的回复继续由 Electron main process 接收；解锁后聊天页从 main process 的有界请求快照恢复最新内容。

## 2. 不做范围

- 不实现应用退出或主进程重启后的 Run resume。
- 不引入 cursor、RunEventStore、事件持久化或自建 AG-UI 状态机。
- 不实现系统 suspend/resume 后的协议重连；该能力需要后端可重拉契约的独立设计。
- 不修改 Conversation、Message、Repository、MAF Hosting 或 WebApi。
- 不实现工具 Activity、多模态、Trace 或全局网络状态条。

## 3. 上游设计引用

- `AGENTS.md` §3.2～3.3：Renderer 只能通过 preload/IPC 调用后端，v1 不实现 AG-UI Run resume、cursor 或 RunEventStore。
- `docs/01-requirements/acceptance-criteria.md` AC-079：锁定后拒绝新写操作，锁定前开始的流式回复保留至完成或失败。
- `docs/01-requirements/acceptance-criteria.md` AC-089：模型故障显示错误气泡、错误码、一句话原因和重试按钮。
- `docs/03-architecture/adr/ADR-011-auto-lock-with-inflight-task-survival.md`：Electron main process 跨锁屏持有流，解锁后 Renderer 拉取最新状态。
- `docs/03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md`：协议边界与 v1 不做事件重放的约束。
- `docs/06-implementation/H5-005-agent-conversation/scope.md` §6～8：H5-005-D 的范围与禁止恢复旧 Run 模型的约束。
- `src/app/desktop/electron/main.ts`：当前 SSE fetch、认证门禁和锁屏生命周期的直接实现。
- `src/app/desktop/src/features/chat/chat-panel.tsx`：当前发送、增量渲染和服务端历史校准入口。

## 4. 测试引用

暂无独立 H4 TC；临时以 AC-079、AC-089 和本简报 §9 为验证依据。H4 测试矩阵缺口不免除 Electron E2E 覆盖。

## 5. 当前基线与问题

### 5.1 当前实现

- `electron/main.ts` 在 main process 中读取 SSE，锁屏不会主动中断已经开始的 fetch。
- `requireAuthenticated()` 已在 locked 状态拒绝新的 IPC 写操作。
- `chat-panel.tsx` 只监听实时 delta；`chat()` 完成后再从服务端读取持久消息。
- `Sender` 已使用 `loading` 状态，但没有接入 `onCancel`。
- 锁屏以覆盖层呈现，ChatPanel 不卸载。

### 5.2 待解决问题

1. main process 没有请求注册表和 `AbortController`，用户无法停止请求。
2. Renderer 监听短暂缺席或解锁后没有可查询的 main process 快照。
3. HTTP、网络和流解析失败只显示临时 toast，没有错误码、错误气泡或重试入口。
4. 失败和停止后的部分回复没有统一使用服务端消息历史校准。

## 6. 允许修改的文件

- `src/app/desktop/electron/main.ts`
- `src/app/desktop/electron/preload.ts`
- `src/app/desktop/src/shared/network/contracts.ts`
- `src/app/desktop/src/features/chat/chat-panel.tsx`
- `src/app/desktop/tests/login.spec.ts`
- `docs/06-implementation/H5-005-agent-conversation/H5-005-D/implementation-record.md`
- `docs/06-implementation/H5-005-agent-conversation/scope.md`
- `docs/06-implementation/README.md`

## 7. 禁止修改

- `src/core/**`、`tests/**` 和数据库 Migration。
- `docs/01-requirements/**`、`docs/03-architecture/**`、`docs/04-detailed-design/**`。
- Electron 的 `contextIsolation`、`nodeIntegration` 和 `sandbox` 安全配置。
- 不新增或升级 npm 依赖。
- 不让 Renderer 直接 fetch WebApi。

若完成任务必须越出允许范围，停止实施并报告证据，不得自行扩大范围。

## 8. 实现要求

### 8.1 Main process 请求生命周期

- 每个 `requestId` 对应一个 `AbortController` 和一个请求快照；重复运行中的 `requestId` 必须拒绝。
- 快照至少包含 `requestId`、状态、已累积文本和可选结构化错误；状态限定为 `running`、`completed`、`stopped`、`failed`。
- 每个 delta 先写入快照，再发送 IPC 事件，避免 Renderer 监听缺席时丢失最终状态。
- 提供查询和停止 IPC。停止只允许作用于 `running` 请求，并通过 `AbortController.abort()` 传播取消。
- 已结束快照采用固定数量上限淘汰，不能无限增长；运行中请求不得被淘汰。
- 锁定事件不得取消已有请求；新的 `chat` IPC 继续由 `requireAuthenticated()` 拒绝。

### 8.2 Preload 与 Renderer

- preload 只暴露类型化的 `chat`、`getChatRun`、`stopChat` 和状态事件，不泄漏 Electron 或 Node.js 对象。
- `Sender.onCancel` 调用 `stopChat`；停止后保留已有部分文本，不显示模型故障错误。
- ChatPanel 在 locked → authenticated 后查询当前请求快照并覆盖本地流式文本。
- 完成、停止或失败后，正式 Conversation 都尝试从服务端历史重新校准；历史刷新失败不能覆盖已累积文本。
- 模型故障显示错误码、一句话原因和重试按钮；重试使用原用户输入创建新的 `requestId`，不得复用失败的 ID。
- trial 模式没有持久 Conversation 时仍支持停止、错误和重试，但不调用历史接口。

### 8.3 错误边界

- HTTP 错误码稳定映射为 `HTTP_<status>`；网络失败为 `NETWORK_ERROR`；无法解析的流为 `STREAM_ERROR`。
- 用户主动停止不是错误，不显示错误 toast 或错误码。
- 401 仍沿用现有清理认证状态的行为。
- UI 不展示原始堆栈或敏感响应头；原因只取服务端安全文本或稳定兜底文案。

### 8.4 依赖版本策略

- 本任务不新增或升级依赖。Ant Design X 2.8.0 的 `Sender.onCancel` 已由本地类型声明确认可用。

## 9. 测试要求

1. Electron E2E：慢速 SSE 在锁屏前开始，锁屏期间继续输出，解锁后显示完整回复；锁定期间尝试的新写操作被拒绝。
2. Electron E2E：点击 Sender 停止按钮会中止连接，保留已收到文本，并恢复可发送状态。
3. Electron E2E：模拟 HTTP 429，断言错误气泡包含 `HTTP_429`、一句原因和重试按钮；重试使用新的 request ID 并成功完成。
4. 现有正式 Conversation、trial、历史列表、认证和锁屏测试继续通过。

测试必须通过真实 preload/IPC/main process 路径，不允许只验证 mock 自身。

## 10. 验收命令

从仓库根目录按顺序执行：

```shell
npm --prefix src/app/desktop run build
npm --prefix src/app/desktop run lint
npm --prefix src/app/desktop run test
npm --prefix src/app/desktop run test:e2e
dotnet build Inkwell.slnx --no-restore
```

E2E 不依赖外部 WebApi，由现有测试内 HTTP server 提供受控 SSE。若本机 Electron 无法启动，必须明确阻塞并保留失败输出，不得跳过。

## 11. 完成标准

- 用户可停止生成，停止不会被显示为模型错误。
- 锁屏前启动的回复不因锁定中断，解锁后显示 main process 持有的最新结果。
- 模型失败满足 AC-089 的错误码、原因和重试入口。
- main process 请求快照有界，且没有放宽 Electron 安全配置。
- §10 全部命令通过，无新增 warning。
- 实际修改文件是 §6 的子集，§7 保持未修改。

## 12. 风险、假设与待确认项

### 12.1 已知风险

- HTTP 连接被取消后，服务端是否持久化部分回复由现有 Hosting 行为决定；客户端必须以服务端历史为最终事实，同时保留无法刷新时的本地部分文本。
- 本任务只保证主进程存活期间的锁屏恢复，不覆盖应用退出、崩溃或系统休眠断网。

### 12.2 实施假设

- 现有锁屏覆盖层不卸载 ChatPanel，且 main process 生命周期跨锁屏；代码与 E2E 基线均支持该假设。
- 当前 fetch 实现支持标准 `AbortSignal`，无需新增依赖。

### 12.3 待 Owner 确认

无。范围直接来自 AC-079、AC-089 和 ADR-011，主动退出与 Run resume 已被上游明确排除。

## 13. H5 交付格式

完成后必须提供：

1. 修改文件清单。
2. 实际执行的验证命令与输出摘要。
3. 与本简报的偏差及原因；无偏差则明确写“无”。
4. 六字段提交信息草稿：`Design / Tests / Verify / Docs / Risk / Task`。
