---
id: review-2026-05-08-openquestion-discussion
date: 2026-05-08
topic: H1 OpenQuestion 讨论会（OQ-001 ~ OQ-022 关闭）
participants:
  - Inkwell / Owner
  - H1-RequirementsInterviewer (agent)
  - H1-UISpecAuthor (agent)
type: requirements-review
status: archived
upstream:
  - REQ-inkwell-agent-platform
  - open-questions-inkwell-agent-platform
  - ui-spec-inkwell-agent-platform
  - user-flow-inkwell-agent-platform
  - acceptance-criteria-inkwell-agent-platform
downstream: []
---

# H1 OpenQuestion 讨论会评审记录

## 1. 基本信息

- 项目名称：Inkwell Agent Platform
- 评审阶段：H1 需求层（含 H1 上半段需求/范围 + H1 下半段 UI 维度）
- 评审对象：[`docs/01-requirements/open-questions.md`](../01-requirements/open-questions.md) 中 OQ-001 ~ OQ-022 共 22 条待澄清问题
- 评审时间：2026-05-07（第一次会议，决议 OQ-001 / OQ-002）；2026-05-08（第二次会议，决议 OQ-003 ~ OQ-022）
- 评审地点：Teams Meeting（会议原始纪要保存在云端，本归档不内嵌原文）
- 主持人：Inkwell / Owner
- 记录人：H1-UISpecAuthor (agent)
- 参与人员：Inkwell / Owner、H1-RequirementsInterviewer、H1-UISpecAuthor

## 2. 评审材料

| 材料 | 路径或链接 | 版本 |
| --- | --- | --- |
| 需求说明 | [`docs/01-requirements/requirements.md`](../01-requirements/requirements.md) | status=draft（评审前） |
| OpenQuestion 跟踪表 | [`docs/01-requirements/open-questions.md`](../01-requirements/open-questions.md) | status=partially-resolved → closed（本次评审后） |
| UI 说明 | [`docs/01-requirements/ui-spec.md`](../01-requirements/ui-spec.md) | status=draft（评审前） |
| 用户流程 | [`docs/01-requirements/user-flow.md`](../01-requirements/user-flow.md) | status=draft（评审前） |
| 验收准则 | [`docs/01-requirements/acceptance-criteria.md`](../01-requirements/acceptance-criteria.md) | status=draft（评审前） |
| 架构说明 | `<无>` | `<无>` |
| 详细设计 | `<无>` | `<无>` |
| 测试用例 | `<无>` | `<无>` |
| 代码提交 | `<无>` | `<无>` |

## 3. 评审结论

请选择一个结论：

- [x] Approved：通过，可进入下一阶段
- [ ] Approved with Changes：小修改后可进入下一阶段
- [ ] Rejected：不通过，必须返工
- [ ] Pending：信息不足，暂缓决策

> 22 条 OQ 全部 closed；三份 H1 下游产物的回写已由 H1-UISpecAuthor 同日完成；H1 阶段满足"`blocking` 级 OQ 全部解答"的硬前提，可流向 H2。`status: draft → reviewed` 的 frontmatter 翻转由 Owner 在本归档落档后人工执行（参见 `.he/HANDBOOK.md` Q7）。

## 4. 通过项

> 本节按 OQ 编号顺序列出每条决议的"是 / 否"判定结果。每条 OQ 的完整候选答 / 后果 / 回答理由 / 回写落点保留在 [`open-questions.md`](../01-requirements/open-questions.md)，本节只做指针。

### 4.A 第一次会议（2026-05-07，OQ-001 / OQ-002）

| 编号 | 内容 | 说明 |
| --- | --- | --- |
| AP-001 | OQ-001 v1 不在客户端做 PII / 敏感字段拦截 | 选 A，由后端运维兜底；详见 [open-questions.md OQ-001](../01-requirements/open-questions.md) |
| AP-002 | OQ-002 v1 角色仅 Member + 通过 `is_super=true` 提权的 Admin | 选 A，避免引入第三角色；详见 OQ-002 |

### 4.B 第二次会议（2026-05-08，OQ-003 ~ OQ-022）

| 编号 | 内容 | 说明 |
| --- | --- | --- |
| AP-003 | OQ-003 v1 不提供 SSO / OIDC，仅账号密码登录 | 选 A |
| AP-004 | OQ-004 同一 Agent 同时只允许 1 个有效公开 API Token，新 Token 立即作废旧 Token | 选 A |
| AP-005 | OQ-005 账号由后端运维通过 SQL / 管理脚本创建，v1 不提供自助注册与重置 | 选 A |
| AP-006 | OQ-006 模型可选清单完全由后端配置文件决定，UI 渲染下拉 | 选 A |
| AP-007 | OQ-007 Admin 不能编辑他人 Agent 配置，仅解封账号 + 撤销共享 + 看审计 | 选 A |
| AP-008 | OQ-008 长期记忆模式切换不清空已有摘要，跨 Agent 不共享 | 选 A |
| AP-009 | OQ-009 v1 测试矩阵仅 Win11 ≥ 22H2 + macOS 12+ Apple Silicon | 选 A |
| AP-010 | OQ-010 Agent 库三档 tab 顺序固定为 `我的` / `团队共享` / `我使用过` | 选 A |
| AP-011 | OQ-011 v1 外壳 = 顶栏 + 左侧 nav + 主区；视觉风格参考 Ant Design Pro 管理页面（H2 ADR 候选输入） | 自定义（落 A 框架 + 视觉指引） |
| AP-012 | OQ-012 Agent 库列表 = 卡片网格 3–4 列响应式 | 选 A |
| AP-013 | OQ-013 编排视图 = 可视化 DAG 画布；React Flow 作为 H2 前端架构 ADR 候选输入 | 选 A |
| AP-014 | OQ-014 对话页提供历史会话侧栏（可折叠，新建会话按钮在侧栏顶部） | 选 A |
| AP-015 | OQ-015 v1 文案仅简体中文（zh-CN），不引入 i18n key 体系 | 选 A |
| AP-016 | OQ-016 Token 一次性弹层未勾选"我已妥善保存"时阻断关闭 | 选 B |
| AP-017 | OQ-017 锁定触发瞬间的在途任务（录音 / 上传 / 流式回复）保留至完成或失败，结果在锁屏背后累积；锁定期间禁止新发起任何写操作 | 选 B（NFR-003 在 ui-spec.md 落地特例） |
| AP-018 | OQ-018 trace 主视图视觉形态推迟到原型阶段决定，ui-spec.md §7 维持"形式无关"描述 | 选 D |
| AP-019 | OQ-019 头像默认值 = 首字母圆形占位 + 哈希背景色 | 选 A |
| AP-020 | OQ-020 v1 不提供审计日志导出（CSV / Excel / 剪贴板），合规导出场景由后端运维兜底 | 选 B |
| AP-021 | OQ-021 加载与空状态视觉形式 = 骨架占位 + 文案，无插画 | 选 A |
| AP-022 | OQ-022 跨平台快捷键 = macOS Cmd ↔ Windows Ctrl 默认双端等价，v1 不提供自定义快捷键面板 | 选 A |

## 5. 修改项

> 本节登记"评审会上达成的决议要在哪里落地"。本次会议的所有回写由 H1-UISpecAuthor 在 2026-05-08 当日完成；保留在此供后续审计。

| 编号 | 问题 | 负责人 | 截止时间 | 状态 |
| --- | --- | --- | --- | --- |
| R-001 | 把 OQ-001 ~ OQ-010 决议落到 [`open-questions.md`](../01-requirements/open-questions.md) 各条 `回答 / 决策日期 / 决策人 / 卡点等级 / 回写` | H1-RequirementsInterviewer (agent) | 2026-05-08 | done |
| R-002 | 把 OQ-011 ~ OQ-022 决议落到 [`open-questions.md`](../01-requirements/open-questions.md) 各条 `回答 / 决策日期 / 决策人 / 卡点等级 / 回写`；frontmatter `status` 改为 `closed` | H1-UISpecAuthor (agent) | 2026-05-08 | done |
| R-003 | 把 OQ-011 / OQ-012 / OQ-013 / OQ-014 / OQ-015 / OQ-016 / OQ-017 / OQ-019 / OQ-020 / OQ-021 / OQ-022 决议回写到 [`ui-spec.md`](../01-requirements/ui-spec.md) §0.2 / §1.5 / §1.7 / §2.5 / §3.1 / §3.2 / §3.7 / §4.1 / §4.3.1 / §4.3.5 / §4.3.10 / §5.1 / §5.5 / §6 / §7 / §9.4 / §10.2 / §10.3 / §13 | H1-UISpecAuthor (agent) | 2026-05-08 | done |
| R-004 | 把 OQ-013 / OQ-014 / OQ-016 / OQ-017 / OQ-020 决议回写到 [`user-flow.md`](../01-requirements/user-flow.md) §0 / UF-002 / UF-003 / UF-004 / UF-005 / UF-008 / UF-010 / UF-013 异常路径与步骤 | H1-UISpecAuthor (agent) | 2026-05-08 | done |
| R-005 | 把 OQ-012 / OQ-013 / OQ-016 / OQ-017 / OQ-018 / OQ-019 / OQ-020 决议收紧到 [`acceptance-criteria.md`](../01-requirements/acceptance-criteria.md) AC-013 / AC-041 / AC-043 / AC-046 / AC-047 / AC-052 / AC-079 / AC-082 + §5 自检 | H1-UISpecAuthor (agent) | 2026-05-08 | done |
| R-006 | 把 OQ-017 NFR-003 特例（"在途任务保留到完成或失败"）字面写进 [`requirements.md`](../01-requirements/requirements.md) NFR-003 + §9 上游决策记录 | H1-RequirementsInterviewer (agent) | `<TBD-未指定>` | open |

## 6. 风险项

| 编号 | 风险 | 影响 | 缓解方案 | 负责人 |
| --- | --- | --- | --- | --- |
| RK-001 | OQ-011 视觉风格仅锁"参考 Ant Design Pro"，未锁组件库 / 主题选型 | H2 前端架构 ADR 不出来前，UI 设计无法转化为可交付组件 | 立即启动 `H2-ArchitectAdvisor` 起前端架构 ADR，把 Ant Design Pro 作为候选输入；不在 H1 锁定 | Owner / H2-ArchitectAdvisor |
| RK-002 | OQ-013 编排画布只锁"可视化 DAG"语义，React Flow 仅为候选 | H2 前端架构 ADR 未定型前，编排画布无法实现 | 同 RK-001，纳入同一份 ADR 评估 | Owner / H2-ArchitectAdvisor |
| RK-003 | OQ-017 NFR-003 特例只在 ui-spec.md / user-flow.md / acceptance-criteria.md 落地，requirements.md 字面未补 | 跨产物字面不一致，H4 测试用例从 requirements.md 反推时可能漏掉特例 | 见 §5 R-006 由 H1-RequirementsInterviewer 在下一轮更新中追加 | Owner / H1-RequirementsInterviewer |
| RK-004 | OQ-018 trace 主视图视觉形态选择"推迟到原型阶段"，AC-052 仅验字段完备性、不验视觉 | H4 测试设计在原型 reviewed 前无法对 trace 视觉做用例 | 等原型 reviewed 后由 PrototypeReviewer 回写 ui-spec.md §7 / AC-052 + 升级 OQ-018 回写行 | Owner / PrototypeReviewer |

## 7. 决策记录

> 完整决策追溯（含原因 / 替代方案 / 影响）保留在 [`open-questions.md`](../01-requirements/open-questions.md) 各 OQ 卡片。本节只做"决策摘要 + 替代方案 + 主要影响"三列收口，便于后续 ADR / Release Notes 反查。

### 7.A 第一次会议（2026-05-07）

| 编号 | 决策 | 原因 | 替代方案 | 影响 |
| --- | --- | --- | --- | --- |
| D-001 | OQ-001：v1 客户端不做 PII / 敏感字段拦截 | 团队范围内、信任边界由后端兜底；客户端不增加合规复杂度 | 客户端做正则脱敏（B）/ 客户端拒收并提示（C） | NFR-006 实现简化；合规审计需走后端运维 |
| D-002 | OQ-002：v1 角色仅 Member + 通过 `is_super=true` 提权的 Admin | 不引入第三角色，REQ-001 / REQ-017 表达完整 | 引入 Admin 独立角色（B）/ 引入 Owner / Editor / Viewer 三角色（C） | REQ-001 / REQ-017 / NFR-004 字面口径统一 |

### 7.B 第二次会议（2026-05-08）

| 编号 | 决策 | 原因 | 替代方案 | 影响 |
| --- | --- | --- | --- | --- |
| D-003 | OQ-003：账号密码登录，v1 不接 SSO / OIDC | 内部团队规模小，SSO 引入工作量与外部依赖 | 接 OIDC（B） | REQ-001 验收口径不变 |
| D-004 | OQ-004：同一 Agent 仅 1 个有效公开 API Token；新 Token 立即作废旧 Token | 实现简单 + 安全模型清晰 | 多 Token 并存（B）/ 灰度过渡期（C） | REQ-013 / EX-005 / AC-050 锁定 |
| D-005 | OQ-005：账号由后端运维 SQL / 管理脚本创建 | v1 团队封闭，自助注册与重置成本不划算 | 自助重置邮件（B）/ Admin UI 创建（C） | UI-001 文案"联系系统管理员"；REQ-017 不含创建账号 |
| D-006 | OQ-006：模型清单由后端配置文件决定 | 后端配置生效快，前端不需要硬编码 | 写死前端列表（B）/ 模型市场（C） | REQ-005 / UI-004 §4.3.3 |
| D-007 | OQ-007：Admin 仅解封 + 撤销共享 + 看审计，不能编辑他人 Agent | 权限最小化；避免越权 | Admin 全权编辑（B） | REQ-017 / UI-004 §4.8 / UI-009 |
| D-008 | OQ-008：长期记忆模式切换不清空已有摘要，跨 Agent 不共享 | 用户感知一致；不引入跨 Agent 数据流向 | 切模式清空（B）/ 跨 Agent 共享（C） | REQ-010 / UI-004 §4.3.7 |
| D-009 | OQ-009：测试矩阵仅 Win11 ≥ 22H2 + macOS 12+ Apple Silicon | 团队设备覆盖；其他平台维护成本不划算 | 加 Win10 / Intel Mac（B） | NFR-002 / AC-073 / AC-074 / AC-075 |
| D-010 | OQ-010：Agent 库三档 tab 顺序固定为 `我的` / `团队共享` / `我使用过` | 信息架构稳；Owner 主路径优先 | 用户偏好持久化（B） | UI-003 §3.1 |
| D-011 | OQ-011：v1 外壳 = 顶栏 + 左侧 nav + 主区；视觉风格参考 Ant Design Pro | 信息架构最稳；视觉给 H2 ADR 留空间 | 仅顶栏（B）/ 顶栏 + 抽屉（C） | UI-001 ~ UI-009 公共外壳；H2 前端架构 ADR 输入 |
| D-012 | OQ-012：Agent 库 = 卡片网格 3–4 列响应式 | 视觉友好；v1 用户量级 ~100 / 单用户 Agent 数量级可控 | 表格行（B）/ 卡片 + 切换表格（C） | UI-003 §3.1 / §3.2 / AC-013 |
| D-013 | OQ-013：编排视图 = 可视化 DAG 画布；React Flow 作 H2 ADR 候选 | 用户体验最直观 | DSL / YAML（B）/ 双形态（C） | UI-006 / UF-008 / AC-041 ~ AC-043；H2 前端架构 ADR 输入 |
| D-014 | OQ-014：对话页提供可折叠的历史会话侧栏 | 信息架构清晰 | 不提供（B）/ 顶部下拉（C） | UI-005 §5.1 / UF-005 / AC-084 / AC-085 |
| D-015 | OQ-015：v1 仅 zh-CN，不引入 i18n key 体系 | 内部团队中文；最快交付 | 中英双语（B）/ 跟系统语言（C） | 全 UI 文案 / AC 文案匹配 |
| D-016 | OQ-016：Token 弹层未勾选"已妥善保存"阻断关闭 | 安全模型够强；体验可接受 | 不阻断（A）/ 关闭即作废（C） | UI-004 §4.3.10 / UF-010 / AC-046 / AC-047 |
| D-017 | OQ-017：锁定触发前已发起的在途任务保留到完成 / 失败，结果在锁屏背后累积；锁定期间禁止新发起任何写操作 | 用户体验最好；NFR-003 写操作禁令通过特例显式表达 | 取消并丢弃（A）/ 暂停期间不计时（C） | NFR-003 特例（在 ui-spec.md / user-flow.md / acceptance-criteria.md 落地）；requirements.md 字面后续补 |
| D-018 | OQ-018：trace 主视图视觉形态推迟到原型阶段 | H1 不强制锁视觉；保 AC-052 字段完备 | 时序树（A）/ 平铺列表（B）/ 摘要面板（C） | UI-007 §7 / AC-052；后续由 PrototypeReviewer 回写 |
| D-019 | OQ-019：头像默认值 = 首字母圆形占位 + 哈希背景色 | 实现最简单；可读性可接受 | 图标库（B）/ Identicon（C）/ 固定灰色（D） | UI-003 / UI-004 §4.3.1 / UI-005 |
| D-020 | OQ-020：v1 不提供审计日志导出 | 最快交付；合规走后端运维 SQL | CSV / Excel 导出（A）/ 剪贴板复制（C） | UI-009 §9.4 / UF-013 / AC-082 |
| D-021 | OQ-021：加载与空状态 = 骨架 + 文案，无插画 | 实现快；视觉中性 | 加插画（B）/ spinner + 文案（C） | UI-003 ~ UI-009 各 X.6 / X.7；§10.2 / §10.3 |
| D-022 | OQ-022：跨平台快捷键双端等价（Cmd ↔ Ctrl），v1 不提供自定义 | 用户偏好刚性可接受；实现成本最低 | 自定义面板（B）/ Web 通用键（C） | §0.2 / UI-005 §5.1 / §5.4 |

## 8. 下一步动作

| 动作 | 负责人 | 截止时间 | 验收方式 |
| --- | --- | --- | --- |
| 把 [`requirements.md`](../01-requirements/requirements.md) / [`open-questions.md`](../01-requirements/open-questions.md) / [`ui-spec.md`](../01-requirements/ui-spec.md) / [`user-flow.md`](../01-requirements/user-flow.md) / [`acceptance-criteria.md`](../01-requirements/acceptance-criteria.md) 的 frontmatter `status: draft → reviewed`，并在 `reviewers:` 加 Owner | Inkwell / Owner | `<TBD-未指定>` | 5 份产物 frontmatter 翻转完成 |
| 启动 `H2-ArchitectAdvisor` 起草 H2 前端架构 ADR，把 OQ-011 Ant Design Pro 与 OQ-013 React Flow 作为候选输入做评估 | Inkwell / Owner | `<TBD-未指定>` | `docs/03-architecture/adr/ADR-NNN-frontend-architecture.md` 出现 |
| 切回 `H1-RequirementsInterviewer` 在 [`requirements.md`](../01-requirements/requirements.md) NFR-003 字面追加 OQ-017 特例（在途任务保留到完成 / 失败），并把该追加记入 §9 上游决策记录 | Inkwell / Owner | `<TBD-未指定>` | requirements.md NFR-003 文字含特例；§9 新增一条 |
| 原型 reviewed 后由 `PrototypeReviewer` 回写 [`ui-spec.md`](../01-requirements/ui-spec.md) §7.1 / §7.2 / [`acceptance-criteria.md`](../01-requirements/acceptance-criteria.md) AC-052，并把 [`open-questions.md`](../01-requirements/open-questions.md) OQ-018 "回写"行升级为指向具体落点 | Inkwell / PrototypeReviewer | `<TBD-未指定>` | OQ-018 "回写"行不再是"原型阶段决定"占位 |

> 上述 4 条动作建议由 Owner 同步登记到 [`docs/06-tasks/task-board.md`](../06-tasks/task-board.md)：可立即开工的进第 1 节"在跑任务"；需要人拍板的（如 H2 ADR 启动时机）进第 2 节"等待人工决策"。本归档不替代 task-board，两边脱钩 = 评审过了但没人推进。

## 9. 备注

- **原始素材边界声明**：本次评审的会议原始纪要保存在云端 Teams Meeting 录音 / 笔记，未在本归档内嵌；本归档基于 [`open-questions.md`](../01-requirements/open-questions.md) 中 OQ-001 ~ OQ-022 各条已书面化的 `回答 / 决策日期 / 决策人` 字段做事实抽取，不含逐条发言原话。如未来需要逐条发言审计，请回到云端原始纪要。
- **`/log-review` 流程边界**：`/log-review` 模板第 3 节"关键发言（按时间序）"在本归档以"§4 通过项 + §7 决策记录"两节合并替代——因为本次评审的输出形态是结构化的 OQ 决议（每条 OQ 卡片本身就是发言人 → 内容引用的等价物），单独再做一遍逐条发言搬运会重复且不增加追溯价值。如未来评审形态是自由讨论而非 OQ 闭合，仍按 `/log-review` 第 3 节走逐条发言归档。
- **未关联凭证**：本归档 frontmatter `downstream` 为空、§2 评审材料中"架构说明 / 详细设计 / 测试用例 / 代码提交"为 `<无>`——对齐当前 Inkwell 仓库尚未进入 H2 / H3 / H4 / H5 的真实状态；后续阶段产物出现后由 `hx-doc-gardener` 在周期性梳理时回填本归档 §2 与 frontmatter `downstream`。

## 10. 待澄清

| 编号 | 问题 | 责任人 | 截止时间 |
| --- | --- | --- | --- |
| Q-001 | §5 R-006（requirements.md NFR-003 字面补特例）的 `due` 未指定 | Inkwell / Owner | `<TBD-未指定>` |
| Q-002 | §8 4 条 NEXT 动作的 `due` 未指定 | Inkwell / Owner | `<TBD-未指定>` |
| Q-003 | OpenQuestion 讨论会原始 Teams 会议纪要 / 录音的云端访问链接（用于将来回追原话） | Inkwell / Owner | `<TBD-未指定>` |
