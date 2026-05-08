---
id: ADR-001-client-runtime-electron-react
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [Inkwell]
created: 2026-05-08
updated: 2026-05-08
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
  - OQ-011
downstream:
  - ADR-006
  - ADR-011
  - ADR-012
  - ADR-014
---

# ADR-001 客户端运行时：Electron + React + Vite + TypeScript

## 上下文

Inkwell 是企业内部使用的 Agent 平台，[NFR-002](../../01-requirements/requirements.md) 锁定客户端必须同时支持 Windows 11 与 macOS 12+ Apple Silicon，[OQ-011 closed §A](../../01-requirements/open-questions.md) 已锁"顶栏 + 左侧 nav + 主区"外壳并提示视觉风格沿用 [Ant Design Pro](https://pro.ant.design/)。[NFR-001](../../01-requirements/requirements.md) 强制联网工作（不要求离线）。客户端需要承担五类"原生层"职责：(1) [NFR-003](../../01-requirements/requirements.md) 自动锁定调度（窗口失焦 + idle 监听），(2) [REQ-016](../../01-requirements/requirements.md) 麦克风 / 文件系统访问，(3) Electron 主进程持有跨锁屏的会话状态（详见 [ADR-011](./ADR-011-auto-lock-with-inflight-task-survival.md)），(4) [OQ-022 closed §A](../../01-requirements/open-questions.md) macOS Cmd ↔ Windows Ctrl 等价快捷键，(5) 自动更新分发。

候选运行时与背后的 trade-off 在第三步反问中通过 Q-A1 收口；用户答 A（Electron + React + Vite + TypeScript）。本 ADR 把决策落在书面证据上。

## 决策

**客户端运行时锁定为：Electron 38+ + React 19 + Vite 6 + TypeScript 5.x（截至 2026-05-08 最新稳定 minor），UI 视觉风格参考 Ant Design Pro / Pro Layout。**

- 具体补丁版本在 H5 第一个编码任务卡内固定（`package.json` 精确锁版本 + `package-lock.json` 提交）；本 ADR 只锁 major / minor 边界。
- 主进程（Electron Main）承担：自动锁定调度、麦克风权限、自动更新、AG-UI 长连接（详见 [ADR-012](./ADR-012-client-server-protocol-rest-agui.md)）。
- 渲染进程（Renderer）承担：所有 UI-001 ~ UI-009 渲染、状态管理、与主进程 IPC。
- 构建：Vite 单工具链覆盖 dev / prod；不引入 webpack。
- 测试：Vitest（单元）+ Playwright（E2E，跨 Win11 / macOS 12+）（待 [OQ-A007](../open-questions-arch.md) 接受默认值后正式锁）。

## 备选项

### 备选 A：Tauri 2.0 + React + Vite + TypeScript

- **放弃理由**：(1) Tauri 主进程是 Rust，团队当前无 Rust 经验，违反 [agents/architect-advisor/AGENT.md §6 "对团队维护能力的影响"](../../../.he/agents/architect-advisor/AGENT.md)。(2) Tauri 在 macOS 上对 [Web Audio + MediaRecorder API](https://developer.mozilla.org/docs/Web/API/MediaRecorder) 的兼容性不如 Electron 成熟，REQ-016 语音输入风险更高。(3) Tauri 自动更新链路需要自建签名服务，Electron 有 `electron-updater` 一体化方案。

### 备选 B：仅 Web App（PWA）+ 浏览器壳

- **放弃理由**：(1) PWA 在 macOS Safari 下对 `MediaRecorder.start(timeSlice)` 不稳定，会让 [UF-005](../../01-requirements/user-flow.md) 录音体验下降。(2) 无法实现 [NFR-003 + OQ-017](../../01-requirements/open-questions.md) 跨锁屏的"主进程持订阅"模式（浏览器 tab 失焦后会被休眠）。(3) 无法满足 OQ-022 macOS Cmd 修饰键差异（浏览器拦截系统快捷键不可靠）。

### 备选 C：原生（Swift / WPF）双端各一份

- **放弃理由**：(1) 团队规模无法维护两套独立代码库；(2) 与 OQ-006 closed §A "v1 范围风险已签字"冲突——重复实现会让工期不可控；(3) UI 视觉一致性需要双端两套设计资产。

## 后果

### 正面

- React + Vite + TypeScript 的开发循环（HMR < 1 s）符合 v1 单人 / 小团队工期需求。
- Electron 主进程提供完整 Node.js 能力，[ADR-011 自动锁定 + OQ-017 在途任务保活](./ADR-011-auto-lock-with-inflight-task-survival.md) 落地路径清晰（`app.on('browser-window-blur')` + `powerMonitor.on('lock-screen')` + `ipcMain` 持订阅）。
- Ant Design Pro 视觉风格能复用大量管理后台模式（[UI-009 管理页](../../01-requirements/ui-spec.md) / [UI-007 调试页](../../01-requirements/ui-spec.md) 都是典型管理后台布局）。
- 与 [microsoft/agent-framework](../../../../../microsoft/agent-framework/) AG-UI 客户端 SDK（基于 fetch + ReadableStream，浏览器与 Node.js 双兼容）天然适配 Electron 渲染进程。

### 负面

- Electron 二进制包体大（≈ 90 MB），单次发布对网络与磁盘要求高；通过 [electron-builder](https://www.electron.build/) 自动化分发到 Windows / macOS 即可缓解。
- Renderer 与 Main 之间的 IPC 通信成本（[contextIsolation](https://www.electronjs.org/docs/latest/tutorial/context-isolation) 必须开启）；通过 `preload.ts` + `contextBridge` 显式暴露受控 API 缓解。
- React 19 的 Server Components / Actions / `use` API 与 Concurrent Mode 学习曲线对新人有要求；通过团队内部 R&D 文档与代码评审缓解。

### 中性

- Vite 6 仅支持 ES Module + Node 20+；旧版浏览器兼容性不在 NFR-002 范围内。
- Ant Design Pro 是视觉**风格参考**而非组件库锁定；具体组件库选型（Ant Design 5.x / 自研 Design System）留给 H3 详细设计阶段。

## 状态

- **状态**：accepted
- **首次发布**：2026-05-08
- **关联**：supersedes 无；下游 [ADR-006](./ADR-006-orchestration-canvas-react-flow.md) / [ADR-011](./ADR-011-auto-lock-with-inflight-task-survival.md) / [ADR-012](./ADR-012-client-server-protocol-rest-agui.md) / [ADR-014](./ADR-014-i18n-out-of-scope-v1.md)
- **置信度**：high（团队经验 + 业界主流 + 与下游 ADR 配套已经过反向验证）
