---
id: ADR-011-auto-lock-with-inflight-task-survival
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [ Inkwell ]
created: 2026-05-08
updated: 2026-05-09
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
  - ADR-001
  - ADR-002
  - ADR-012
downstream: []
---

# ADR-011 客户端自动锁定 + 在途任务跨锁屏存活

## 上下文

[NFR-003](../../01-requirements/requirements.md) 要求：“客户端 5 分钟无操作自动锁定，重新输入密码解锁。” [OQ-017 closed §A](../../01-requirements/open-questions.md) 进一步规定：“锁屏期间在途任务不中断；解锁后用户能看到完整对话与 trace。”

[Q-A6 / OQ-A002 closed §A](../open-questions-arch.md) 为这一需求选定了实现路径：**Electron 主进程背后维持 SSE订阅，UI 进程切锁屏页**。主进程生命周期跨越锁屏（只要不退出应用），因此不需要 cursor / RunEventStore / Run resume 这套事件重放机制。

[W-003 警告](../../01-requirements/repo-impact-map.md)：[NFR-003 字面](../../01-requirements/requirements.md) 没有提到“5 分钟”具体值，“5 分钟”来自 [OQ-017](../../01-requirements/open-questions.md) — 详见 [RISK-003](../risk-analysis.md)。

## 决策

**自动锁定门槛 5 分钟（[NFR-003 §1](../../01-requirements/requirements.md) + [OQ-017](../../01-requirements/open-questions.md)）；Electron 主进程跨锁屏保持单一 SSE 连接；UI 进程只是展示层，锁屏后被锁屏页覆盖，解锁后从主进程取最新对话 / trace 状态重新渲染。**

- 客户端调度（详见 [ADR-001](./ADR-001-client-runtime-electron-react.md)）：
  - 用户最后一次输入（鼠标 / 键盘 / 触控板）后启动 5 分钟 idle timer。
  - timer 触发时显示 lock screen 覆盖层，要求重新输入密码。
  - 同时 `powerMonitor.on('lock-screen')` / `app.on('browser-window-blur')` 监听系统锁屏 / 切到其他应用，作为附加触发条件。
  - 锁屏期间主进程不退出，也不主动断 SSE；仅 Renderer 进端被 lock screen 颐代。
- 协议（详见 [ADR-012](./ADR-012-client-server-protocol-rest-agui.md)）：
  - 主进程所持 SSE 是 **唯一源**：Renderer 心跳不雨露到后端。
  - 主进程维护 “最后 N 个 Run 事件” 环形缓冲区（默认 N=4 KiB）；Renderer 重新打开聊天面板时从主进程 IPC 拉取。
  - 后端不持久化 AG-UI 事件（不引入 RunEventStore）；DurableTask 仅负责“任务本身”的跨 Pod 存活，事件流重连由客户端主进程保证。
  - SSE 底层启用 [keepalive heartbeat](https://developer.mozilla.org/docs/Web/API/Server-sent_events/Using_server-sent_events) 每 15 s；超时 30 s 以主进程重连逻辑从后端 “当前 Run 状态 endpoint” 重拉 —— 该 endpoint 由 [Microsoft.Agents.AI.Hosting.AGUI.AspNetCore](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.Hosting.AGUI.AspNetCore/) 提供。
- 解锁后 UI：聊天面板从主进程环缓拉取锁屏期间产生的最新内容一次性渲染；trace 面板同理。
- 离线 / 休眠场景：
  - 主进程检测 `powerMonitor.on('suspend')`：休眠前主动发 `client_paused` 事件给后端，后端按 [DurableTask](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.DurableTask/) 语义继续推进 Run；`resume` 事件触发后主进程重连。
  - 超时（3 min 内未连通） UI 显示降级 banner，对话保留草稿；对话本身不丢（后端仍完整保留 Conversation 表上的 message 持久化）。
- 不覆盖场景：用户主动退出客户端 → 主进程释放 → 环形缓冲区与 SSE 丢失。下次启动只能从 [REQ-006 历史会话](../../01-requirements/requirements.md) 看到锁屏前的快照，这是可接受的边界。

## 备选项

### 备选 A（OQ-A002 §A，本 ADR 采用）：Electron 主进程持续保持 SSE 连接

- **被选用**：(1) 实现路径最短—不引入 cursor / RunEventStore / replay，v1 范围可控；(2) 与单用户场景贴合—主进程生命周期跨越锁屏仅依赖 OS 不关闭应用；(3) 依赖“主进程休眠后重连”兑底，这部分靠 Electron `powerMonitor` 事件 + AG-UI hosting 原生重拉能力。

### 备选 B（OQ-A002 §B）：AG-UI + SignalR 双协议

- **放弃理由**：(1) 引入两套协议（HTTP+SSE 与 WebSocket）增加客户端 / 后端复杂度；(2) AG-UI 本身已能处理跨锁屏语义，没必要再加 SignalR；(3) 与 [ADR-012](./ADR-012-client-server-protocol-rest-agui.md) “REST + AG-UI 单协议”决策冲突。

### 备选 C（OQ-A002 §C，原 Agent 默认值）：AG-UI Run resume + cursor + RunEventStore

- **放弃理由**：(1) v1 为“单用户 + 主进程可保活”场景，cursor 多点一致 + RunEventStore 高吞吐写入是过重的护栏；(2) 事件体量高频，高并发下会引入 [原 RISK-007](../risk-analysis.md) 写入热点，v1 取不到收益；(3) 后端多作一套事件持久化与生命周期管理，与 [OQ-006](../../01-requirements/open-questions.md) v1 范围控制冲突。【本 ADR 以 Owner 在 OQ-A002 复议中反转默认值、选中 §A 为准】。

### 备选 D：锁屏即终止 Run

- **放弃理由**：(1) 与 [OQ-017 closed §A](../../01-requirements/open-questions.md) “锁屏期间在途任务不中断”直接冲突；(2) Agent 任务可能跑数十秒到几分钟，频繁锁屏会让用户体验断裂。

## 后果

### 正面

- v1 实现路径最短。不需 RunEventStore / cursor 抽象，[Inkwell.AGUI.Hosting](../../01-requirements/repo-impact-map.md) 只负责 SSE 报发。
- 与 [microsoft/agent-framework](../../../../../microsoft/agent-framework/) hosting 包原生能力一致，[Microsoft.Agents.AI.Hosting.AGUI.AspNetCore](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI.Hosting.AGUI.AspNetCore/) 一行注册即可。
- 锁屏期间 Run 在 DurableTask 上继续推进 → AKS Pod 替换 / 重启不丢任务。
- UI 体感“无缝”：解锁后聊天面板从主进程拉取环缓重渲染。

### 负面

- 主进程长 SSE 依赖上游不可控环节：macOS App Nap / Windows 节能模式 / 企业代理 idle timeout / NAT 表老化。必须在主进程加“检测丢连 + 重连”重试逻辑，详见 [RISK-007](../risk-analysis.md)。
- 环形缓冲区仅能保留最近 N 条事件。锁屏期间产生事件超过 N 条时，超出部分仅能从后端“当前 Run 状态 endpoint”拉取最终状态重渲染，中间过程丢失。H3 详细设计需明确 N 默认与调优参数。
- 用户主动退出客户端 → 主进程释放 → SSE 与环缓丢。该场景下仅从 [REQ-006 历史会话](../../01-requirements/requirements.md) 看快照。

### 中性

- “5 分钟”是 OQ-017 给的具体值；[NFR-003 字面](../../01-requirements/requirements.md) 仅说“自动锁定”未给 N min 数字（W-003）。本 ADR 视为已经吸收 OQ-017 决议，5 min 等于 NFR 实际边界 — 在 [risk-analysis.md RISK-003](../risk-analysis.md) 备案这一文字差异。
- 锁屏倒计时阈值（5 min）后续可作为 admin 配置项；v1 保持固定值。

## 状态

- **状态**：accepted（接受 [OQ-A002 closed §A](../open-questions-arch.md) 决议）
- **首次发布**：2026-05-08（初版 §C） / 2026-05-09 重写为 §A
- **关联**：supersedes 无；上游 [ADR-001](./ADR-001-client-runtime-electron-react.md) / [ADR-002](./ADR-002-backend-runtime-dotnet10-aspnetcore.md) / [ADR-012](./ADR-012-client-server-protocol-rest-agui.md) / [NFR-003](../../01-requirements/requirements.md) / [OQ-017](../../01-requirements/open-questions.md) / [OQ-A002 closed §A](../open-questions-arch.md)
- **置信度**：medium（“主进程长 SSE 可靠性”是新 [RISK-007](../risk-analysis.md)，需 H4 用例验证）
