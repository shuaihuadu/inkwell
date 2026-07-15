# 原型覆盖矩阵

## 主题色

路由：`/themes`

- 碧海青：light / dark
- 朱砂橙：light / dark
- 曜石紫：light / dark
- 单主题详览与三主题对比
- Button、Input、Select、Tag、Badge、Alert、Table、Progress、Slider、Rate 等真实 Ant Design 组件
- Primary、Info、Success、Warning、Error、Text、Border、Container、Layout 等 Token 可视化

## Logo

路由：`/logos`

- `assets/logos/logo.svg`：Inkwell 曜石紫涟漪品牌标记
- 每个方向覆盖 16、24、32、64、96 px
- 每个方向覆盖浅底、深底、主色底和应用图标裁切预览
- 每个方向列出设计意图和小尺寸风险

## 登录页

路由：`/login`

构图方向：沉浸式工作台分栏

状态：

- 默认
- 提交中
- 账号或密码错误
- 账号已锁
- 速率超限
- 离线

该方案覆盖账号、密码、显示密码、登录按钮、管理员联系提示。原型不提供注册与密码重置入口，登录页不再显示版本/构建号（已从 ui-spec.md §1.1 移除）。

## AppShell 外壳

路由：`/shell`

外壳形态：顶栏（56px）+ 左侧 nav（展开 200px / 收起 64px，手动折叠）+ 主区（OQ-011 锁定）。

已确认的设计决策：

- 左侧 nav 是真正可展开折叠的两级树形结构，共三组（2026-07-15 两轮修订，requirements.md §13 第 29/31 条 / ui-spec.md OQ-011 修订）：`工作区`（Agent 空间）/ `资源中心`（工具管理、Skills 管理、模型管理——均为 v1 占位入口，点击只显示"即将上线"，UI-010/UI-011/UI-012）/ `系统管理`（Admin，仅 is_super 可见）。分组标题可点击折叠/展开（箭头旋转动画），Sider 收起为图标条时分组一律展开、只显示图标。占位入口带"待上线"小标签 + hover tooltip。`工作区`/`资源中心`/`Agent 空间` 原名分别为 `工作空间`/`能力管理`/`Agent 库`，2026-07-15 再次改名，同时在 `资源中心` 下新增第三个占位叶子 `模型管理`（UI-012）。
- 品牌主题色（曜石紫/朱砂橙/碧海青）三选一只是 Design Lab 评审工具的候选方案，H2 ADR 会锁定其中一套，**不是**面向最终用户的产品功能；但“浅色/暗色”外观切换是合理的最终产品功能，已加入模拟 Header。
- i18n/多语言切换明确不做（ADR-014 + AGENTS.md §3.3 禁区已锁定 v1 仅 zh-CN，原型阶段不能绕过）。
- “关于”入口：Header 品牌区一个实心圆点（无图标），`currentColor` 驱动的呼吸动画，随主题自动换色；点击弹出 Modal：版本/构建号/提交/作者/GitHub 链接同居一个信息列表，下方大尺寸二维码占据剩余整个区域。
- “个人设置”入口（2026-07-15 新增）：用户菜单“个人设置”点击弹 Modal，内含两组 `Segmented`：外观模式（亮色/暗色/跟随系统，`DesignContext` 新增 `AppearanceMode` 类型，“跟随系统”监听 `prefers-color-scheme` 实时跟随）+ 主题色（复用 `THEMES` 定义），两者都是全局生效（与顶部 Design Lab 工具条的主题/暗色开关共享同一个 `useDesign()` context）。
- “后台服务正常/重连中/后台服务异常”网络状态徽标（2026-07-15 两轮修订：先从“在线”改“网络正常”避免与用户 presence 混淆，再改“后台服务正常”避免与本机网络混淆），ui-spec.md §0.2 同步改写。
- Agent 空间占位内容：固定卡片高度 + 单行省略的标题/元信息 + 固定两行裁剪的描述，保证内容多寡不影响卡片高度一致。顶部“新建 Agent”按钮同样钻取进 Agent 设计页，但以 `new-draft`（未保存的草稿）状态打开空白配置（ui-spec.md §3.4）。两档 tab ——`我的` / `团队共享`（2026-07-15 移除原第三档 `我使用过`，requirements.md §13 第 32 条）。
- **Agent 卡片点击按发布状态分流**（2026-07-15 新决定，requirements.md §13 第 30 条 / ui-spec.md §3.4）：`MOCK_AGENTS` 新增 `published` 字段。已发布 Agent（卡片下方标“点击进入对话”）点击卡片直达新建的 **Agent 对话页**（见下方“Agent 对话页”小节）；仅存在未发布过的草稿（标“草稿” Tag，卡片下方标“点击继继编辑草稿”）仍点击钻取进 Agent 设计页。已发布卡片右上角新增 hover 才显现的“编辑”快捷图标按钮（`.inkwell-agent-card-edit-btn` + styles.css hover 规则），直达 Agent 设计页，不需先进对话。进入对话页时主导航 nav 自动收缩为图标条，“返回”时自动恢复展开（ui-spec.md §5.1）。
- 控件高度/密度规则同上方 README “设计约定”节：全部控件用标准高度，未引入紧凑密度切换。

## Agent 设计页（钻取自 AppShell · Agent 空间）

入口：`/shell` → Agent 空间 → 点击任意 Agent 卡片

不再是独立顶级路由；组件仍是 `AgentDesignPage.tsx`，通过 `onBack` 回调接受宿主（AppShell）传入的返回逻辑，外层高度改为 `100%` 以适配 AppShell 内容区，不再假设自己是整屏顶级页面。

区段：

1. 基础信息
2. Instructions
3. 模型与参数（含上下文：最大消息记录数）
4. 工具
5. Skills
6. 版本

Skills 区段只从统一 Skill 管理目录中选择并绑定，不在 Agent 设计页提供上传或创建入口。上下文策略不再单独成区段——简化为模型与参数区段底部一个数值输入（最大消息记录数），超过该数量时最早的历史消息会被裁剪。“版本与调试”区段改名“版本”并删除了“调试 / Trace (UI-007)”按钮（UI-007 本就未实现，保留一个无效按钮没意义）。**2026-07-15 再次修订**：又删除了原来的“查看版本历史 (UI-008)”跳转按钮，改为直接在“版本”区段内用 `Table` 列出全部历史版本（v1~v3，每行含版本号/状态/保存时间/保存人/变更摘要）——ui-spec.md §4.4 本来就写的是“‘版本’：跳 UI-008 版本视图（同一个 Agent 上下文）”，原来的按钮其实是冗余跳转，现在直接展示 UI-008 承诺的内容才是对齐规范。右侧 diff/回滚面板未实现，仍属原型深度简化。另外，只读模式原本在区段标题旁额外弹一条 Alert “只读模式：当前非 Owner，所有字段不可编辑”，2026-07-15 删除——字段本身变灰就已能传达只读，重复文字提示属于噪音。

头像区域现在真正变片了（列宽 `xl={4}` 而不是以前的 `xl={5}` + 内层 `maxWidth` 减肥法，方框本身充满缩小后的整个列宽，不再出现列宽未变、只是内部方框变小导致两侧留白的问题。

顶栏原来的单一“保存”按钮已拆成“存为草稿”与“发布”两个独立按钮——这是一次真正的需求变更，不只是原型调整：`requirements.md` REQ-015、`user-flow.md` UF-004 §11~13、`ui-spec.md` §4.1/§4.2/§4.4/§4.5 都已同步改写为“存为草稿（不生成新版本，不影响已发布版本或正在进行的对话）+ 发布（生成新版本并立即生效）”两级模型，`acceptance-criteria.md` 新增 AC-096 覆盖“存为草稿不产生新版本”。状态徽标新增“有未发布的修改”（已发布过但当前编辑内容尚未发布），“未保存的草稿”改名“未发布的草稿”；“发布”按钮点击后弹 Modal（说明文字 + 可选“变更说明”输入框），“存为草稿”点击后走轻量 `message` 提示，不弹 Modal。

默认密度改为 **紧凑**（套入 AppShell 后内容区空间更金贵）；“标准/紧凑”仍只调间距，不调控件高度（`density` 已从 SectionBasic/SectionInstructions 移除，因为这两节从未真正依赖它）。

状态：

- 编辑中
- 新建草稿
- 只读（非 Owner）
- 提交中
- 提交失败
- 提交成功

只读与提交中状态会禁用编辑控件；提交失败与提交成功状态有明确反馈。顶部删除操作包含二次确认。“开始对话”打开右侧内嵌的 `CopilotPanel`（copilot 形态，不是跳转到独立页面，也不是遮罩式 `Drawer`——2026-07-15 改为与配置表单并排的非模态面板，详见下方小节），用于在还未发布时快速预览/测试当前编辑中的配置；已发布 Agent 在 Agent 空间点击卡片才直达独立的 Agent 对话页（UI-005，见下方小节）。两处入口视觉形态不同但核心“发消息 → mock 回复”状态机共用，详见下方小节末尾的共享逻辑说明。

## Agent 对话页 与 内嵌对话面板（2026-07-15 新增，2026-07-15 改用 Ant Design X 重建，2026-07-15 再次对齐官方 playground 结构，2026-07-15 内嵌面板改为非模态并排面板）

两个入口：

1. `/shell` → Agent 空间 → 点击任一已发布 Agent 卡片 → 独立的 **Agent 对话页**（`AgentChatPage.tsx`，UI-005），或在对话页头部“编辑”回到 Agent 设计页后再钻回。
2. Agent 设计页顶栏“开始对话” → 内嵌的 **`CopilotPanel`**（不离开当前页面，与左侧配置表单并排显示，不是遮罩弹层）。

两者参考的 Ant Design X 官方 playground 视觉形态不同，第二轮修订不再是"提炼要点重画一版"，而是尽量贴着官方 demo 的组件组合与布局比例还原：

- **Agent 对话页**照抄 [ultramodern playground](https://ant-design-x.antgroup.com/docs/playground/ultramodern-cn) 结构：
  - 左侧 `Conversations` 会话列表（可折叠，与 §0.2 主导航 nav 同屏共存，进入本页时主导航自动收缩为图标条、"返回"后自动恢复展开），hover 会话项出现"…"菜单可删除（`menu` 回调）。
  - 无消息时是**居中的"新对话起始页**"：大号 Agent 名称 + 居中限宽的 `Sender`（不是顶部 Welcome 卡片 + 推荐问题列表那种布局）。
  - 有消息后消息流铺满整个内容区宽度（无 `maxWidth` 限制，2026-07-15 修订，见下）。
  - `Sender` 的 `footer` 插槽左侧只放"上传"按钮（2026-07-15 移除了纯视觉占位的"深度思考"开关——不必要的装饰性控件，且默认 `checked` 态自带主题色描边，实际视觉效果是一条突兀的紫色边框，被判定为噪音直接去掉），右侧是内置的语音/发送按钮（`actionNode`）；`Sender` 本身默认透明底+描边，衬在 `colorBgContainer`（纯白）背景上才会显得像官方 demo 那种"整体悬浮卡片"，不要衬灰色 `colorBgLayout`。回形针按钮 2026-07-15 补上了 `Sender.Header` + `Attachments` 上传面板（点击展开/收起，`beforeUpload={() => false}` 拦截真实上传），此前只是个没有 `onClick` 的摆设按钮点不开任何东西——与 UI-004 的 `CopilotPanel` 用同一套 Attachments 交互，统一两处入口的行为，为后续多模态输入预留一致的 UI 落点。**2026-07-15 再次修订**：对照官方源码（`ultramodern.tsx` 的 `chatSender` 只有 `padding: token.paddingXS` 没有任何 `maxWidth`）发现之前给 `Sender` 外层套了 `maxWidth:860` 的居中限宽容器是自己加的、官方压根没有——改成 `width:"100%"` + 两侧 `token.paddingXS`（8px）小边距，`Sender` 才是像官方那样几乎占满对话区宽度；`Bubble.List` 的 `styles.root` 同步去掉 `marginInline:"auto"` 只保留 `maxWidth:940`（照抄官方数值）。**2026-07-15 三度修订**：`maxWidth:940` 也一起去掉了——用户发消息后发现气泡右侧仍有明显空白，根因是这个上限把 `Bubble.List` 根容器限制在 940px 内且左对齐（不居中），一旦聊天面板实际宽度超过 940px，右对齐的用户气泡只能贴到这个 940px 内框自己的右边，而不是聊天面板的真实右边缘，二者之间就是那段空白；去掉宽度上限后 `Bubble.List` 才和 `Sender` 共享同一条真实右边缘。
  - ai/user 气泡**不设置 `avatar`**（2026-07-15 修订）：对照官方 `ultramodern.tsx` / `copilot.tsx` 源码，两处 `role` 配置都没有 `avatar` 字段，气泡左侧/右侧都不显示头像——之前 `createChatBubbleRoles` 给 user/ai 各挂了一个 `Avatar` 图标，是自己加的、不是官方行为，已删除（`Bubble` 的 `avatar` 不传时默认不渲染，不需要显式传 `false`）。
  - ai 气泡底部挂"重新生成 / 复制 / 点赞点踩"三个动作（`Actions` + `Actions.Copy` + `Actions.Feedback`），"重新生成"真的会调用 `useMockChat` 的 `retryLast()` 重新生成一条回复，不是摆设按钮。
  - `Bubble.List` 用 Ant Design X 原生的 `loading: true` 占位气泡展示"正在回复"动画，不再自己拼一行"正在回复…"文字。
- **Agent 设计页内嵌面板**照抄 [copilot playground](https://ant-design-x.antgroup.com/docs/playground/copilot-cn) 结构，且是**非模态、与左侧配置表单并排的固定宽度面板**（`CopilotPanel`，宽度 400px，`width: open ? 400 : 0` 过渡动画），不是 antd `Drawer` 遮罩弹层——打开面板后左侧配置表单仍可继续滚动/编辑（对齐官方 demo 的 `.copilotWrapper` 左右并排布局，也是用户明确要求的行为）：
  - 顶部标题栏：Agent 头像/名称/模型信息 + 右侧“新建会话”（`+`）与“会话历史”（气泡图标，`Popover trigger="click"` 弹出一个小型 `Conversations` 列表）两个图标按钮，右上角自带一个 `CloseOutlined` 关闭按钮（不再是 Drawer 内置的关闭图标）。
  - 消息流上方是快捷问题按钮行（"研究框架"/"报告目录"），始终显示、不只在空状态才出现，点击直接发送对应问题（对齐官方 demo 的 `chatSender` 里那两个常驻快捷按钮）。
  - `Sender` 增加 `header`（`Sender.Header` + `Attachments` 上传面板，点回形针图标展开/收起，纯视觉不接入真实解析）与 `allowSpeech`。
  - 无消息时用 `Welcome` + `Prompts` 展示推荐问题；ai 气泡同样带"重新生成/复制/点赞点踩"动作（与 Agent 对话页共用同一份 `chatBubbleRoles.tsx` 配置）。
- 两处都用 [`Think`](https://ant-design-x.antgroup.com/components/think-cn) 组件承载工具调用气泡（自带展开/收起，ui-spec.md §5.1）。

## Harness / Todos / 流式输出模拟（2026-07-15 新增）

用户看了 [Think](https://ant-design-x.antgroup.com/components/think-cn) 和 [ThoughtChain](https://ant-design-x.antgroup.com/components/thought-chain-cn) 两个组件文档后，要求把"工具调用、Todos、Harness"这几种场景也设计展示出来，并模拟真实的流式输出。落地成果：

- **工具调用**：沿用已有的 `system` 角色 + `Think` 承载，不变。
- **思考过程**（`"reasoning"` 角色，2026-07-15 二度修订时新加）：对应官方 Harness 控制台的 `ReasoningDisplayObserver`，同样用 `Think` 承载，loading 时标题显示"深度思考中"，完成后变成"已完成思考"。
- **Todos**（`src/chat/TodosPanel.tsx`，新增 `"todos"` 角色）：一个轻量自定义清单组件，对应 `microsoft/agent-framework` 的 `dotnet/src/Microsoft.Agents.AI/Harness/Todo/TodoProvider` 维护的任务清单。**2026-07-15 二度修订**：用户明确要求不要用删除线——pending 灰色文字+灰色空心圆点、in-progress 普通文宗颜色+`LoadingOutlined`、done 绿色文字+`CheckCircleFilled`，不再对 done 文字加删除线，“从灰到绿逐渐点亮”比删除线更符合“任务逐步完成”的直觉，不会让人误读成“划掉/取消”。
- **Harness**（`src/chat/HarnessThoughtChain.tsx`，新增 `"harness"` 角色）：用 Ant Design X 的 `ThoughtChain` 组件承载一轮 plan→execute 自主循环的步骤链（制定计划 → 分别调用两个不同工具 → 整理结果），`status` 沿用 ThoughtChain 自带的 `loading`/`success`/`error`/`abort` 词汇，有 `detail` 的步骤才允许展开（`collapsible`）。
- **用量提示**（`"usage"` 角色，2026-07-15 二度修订时新加）：对应官方 Harness 控制台的 `UsageDisplayObserver`（📊 Tokens 提示），纯文本小字号 caption，耐面在最终回复之前一次性出现。
- **演示编排**（`src/chat/harnessDemo.ts`，2026-07-15 二度修订）：`isHarnessTrigger(text)` 检测用户输入是否包含"研究/调研/分析一下/深度"等关键词；命中后 `runHarnessDemo(...)` 对照 `microsoft/agent-framework` 的 `dotnet/samples/02-agents/Harness` 真实控制台 Observer 会展示的内容种类（`ReasoningDisplayObserver`/`ToolCallDisplayObserver`/`TodoProvider`/`UsageDisplayObserver`）尽量都模拟出来，按时间线依次追加/更新：思考过程 → harness 步骤链（计划→两个独立的工具调用→整理结果）→ todos 任务清单逐项 pending→in-progress→done → 用量提示 → 流式回复——不是一次性摆出的静态截图，两处聊天页面（`AgentChatPage`/`CopilotPanel`）的 `Sender`、`Prompts`、快捷按钮统一走 `handleUserSubmit` 判断是否命中触发词，命中则调 `runHarnessDemo`，否则走原来的 `submit()`。
- **可浏览的静态种子会话**（`AgentChatPage.tsx` 的 `s4` 会话，2026-07-15 二度修订时新加）：因为官方演示需要输入触发词才能看到，为了让主聊天历史里也能直接浏览到完整场景，新加一个静态种子会话（"调研一下行业报告模板"），展示上述所有气泡类型已完成的最终状态（不播动画，直接静态展开）。
- **流式输出模拟**：没有用 Ant Design X `Bubble` 自带的 `typing` 动画——它内部用 `useLayoutEffect` + `requestAnimationFrame` 循环实现且没有 effect cleanup，React `StrictMode`（`main.tsx` 已启用）在开发模式下的双重 effect 调用会把这个 rAF 循环打断在第一帧，实际表现为"字幕永远卡在开头几个字不再推进"。改成 `useMockChat.ts` 新增的 `streamMessage(setMessages, fullText, options)`：先插入一条空内容的 ai 消息，再用普通 `window.setTimeout` 按字符分块（默认 `step:3, interval:28ms`）反复更新同一条消息的 `content`，不涉及 effect/渲染生命周期，不受 StrictMode 双调用影响；`appendReply`（常规单条 mock 回复）和 `runHarnessDemo`（Harness 演示的最终回复）都改用这个函数，两处入口的"流式"效果是同一套实现。

## Agent Loop 演示场景（2026-07-15 新增）

对应 `microsoft/agent-framework` 的 `dotnet/src/Microsoft.Agents.AI/Harness/Loop/LoopAgent`：`LoopAgent` 反复重新调用被包装的 Agent，每轮结束后由 `LoopEvaluator` 判断 `ShouldReinvoke`（继续/停止），继续时把 `Feedback` 带进下一轮输入，直到某个评估器判定满意为止（`LoopAgentOptions.MaxIterations` 兜底）。跟 Harness 的 plan→execute 步骤链是两种不同的自主循环——Harness 侧重"计划 + 工具调用 + 任务清单"，Agent Loop 侧重"产出 → 评估 → 按反馈重新产出"的迭代优化模式（典型场景：文案打磨、代码审查后修改）。

- **新增 `"loop"` 角色**（`ChatMessage.loopSteps`）：跟 `harness` 角色一样复用同一个 `HarnessThoughtChain` 渲染组件（ThoughtChain 本身是通用的步骤链，不必为 Loop 再造一个视觉组件），只是步骤内容语义换成"第 N 轮"+"评估反馈"。评估反馈为"继续优化"时 `status` 用 `"error"`（红色 ⊗ 图标，直观表达"这轮没通过"），为"已达标"时用 `"success"`（绿色 ✓），跟常见的 CI/CD pass/fail 或代码评审 approve/reject 视觉惯例一致，不是字面意义上的系统报错。
- **演示编排**（`src/chat/harnessDemo.ts` 的 `isAgentLoopTrigger` + `runAgentLoopDemo`）：检测用户输入是否包含"优化/改进/迭代/精简/润色"等关键词（跟 Harness 的触发词集合完全不重叠）；命中后按时间线播放"第 1 轮生成初稿 → 评估反馈需要继续优化 → 第 2 轮根据反馈修改 → 评估反馈已达标结束循环 → 流式回复"。两处聊天页面的 `handleUserSubmit` 先判断 `isHarnessTrigger`/`isAgentLoopTrigger`，命中 Loop 触发词时调 `runAgentLoopDemo`，否则走 `runHarnessDemo`。
- **可浏览的静态种子会话**（`AgentChatPage.tsx` 的 `s5` 会话"优化一段产品介绍文案"）：同 Harness 的 `s4`，把 Loop 场景的最终完成态直接摆成一条可浏览的历史记录。
- **踩坑记录**：种子数据/演示文案最初把 `${userText}`（用户原话）直接拼进每一轮的草稿正文里（如"帮我优化一下这段文案：我们的产品…"），导致草稿听起来像是把用户的整句话又重复了一遍，不自然。修复：草稿正文只保留真正的文案内容，`${userText}` 只用在步骤的 `description`（如"基于『xxx』生成初稿"）里做上下文提示，不混进正文。**教训：给"AI 产出的内容"写种子/演示文案时，别图省事直接把用户输入原样拼进产出里，先读一遍拼出来的完整句子，检查是不是自然的表达。**

**排查记录（供以后遇到类似问题参考）**：
1. 打字动画卡在第一帧——用 `node_modules/@ant-design/x/es/bubble/hooks/useTyping.js` 源码确认是 `useLayoutEffect` 驱动 rAF 循环、无 cleanup，`StrictMode` 双调用会打断循环；改用自己的 `setTimeout` 分块更新彻底绕开这个坑。
2. 修复打字动画时顺手给 `useMockChat` 的内部函数（`resetMessages`/`appendReply`/`sendMessage`/`retryLast`/`submit`/`startNewSession`）全部用 `useCallback` + ref（`messagesRef`/`replyingRef`/`mockReplyRef`/`inputRef`）包装到"绝对稳定"，这一步引入了一个新 bug：`submit` 曾经写成 `setInput(current => { ...; sendMessage(text); return ""; })`，在 `setState` 的函数式 updater 里调用有副作用的 `sendMessage`——React `StrictMode` 会故意双调用 updater 函数来抓这类不纯函数，导致消息真的发送了两次（出现"两个子元素 key 相同"的 React 报错 + 气泡真的重复了一遍）。修复：改成读 `inputRef.current`（一个普通 ref），不再把副作用塞进 `setState` 的 updater 里。**教训：`setState(prev => {...})` 的 updater 必须是纯函数，不能在里面调用任何有副作用的函数（哪怜是"看起来只是读一下最新值"的动机），需要读最新值时用 ref，不要用函数式 updater 夹带私货。**

## 用量统计位置 + 图标、流式输出自动跟随滚动（2026-07-15 新增）

用户反馈两点：① Token 用量统计不应该单独占一条气泡展示在回复前面，应该挪到这条回复消息自己的最后面；图标不要用 emoji；② 流式输出时页面没有自动跟随往下滚动。

- **用量统计挪位置 + 换图标**：`usage` 角色整个删除，改成 `ChatMessage.ai` 新增可选字段 `usage?: string`，挂在触发这次用量统计的那条最终 ai 回复消息自己身上；`chatBubbleRoles.tsx` 的 `ai` 角色 `footer` 从 `info.extraInfo.usage` 读出来，渲染在正文下方、Actions（重新生成/复制/点赞点踩）上方，图标从 emoji `📊` 换成 `@ant-design/icons` 的 `BarChartOutlined`。`useMockChat.streamMessage` 新增 `usage` 选项，创建消息时直接带上；`harnessDemo.ts` 的 `runHarnessDemo` 最终 `streamMessage` 调用带上 `usage` 选项，不再单独 `setMessages` 插入一条 `usage` 气泡。
- **流式输出自动跟随滚动**：根因排查详见下方"教训"条目——本质是给 `Bubble.List` 包了一层多余的 `overflow:auto` 外壳、且 `Bubble.List` 自己只写了 `max-height:100%` 没写 `height`，导致它内部真正应该滚动的 `.scroll-box` 永远拿不到确定高度、永远不会触发溢出。修复：外层改成 `flex:1 + minHeight:0 + overflow:"hidden"`，`<Bubble.List style={{height:"100%"}}>` 显式传一个真正的 `height`，padding 从外层挪到 `<Bubble.List styles={{scroll:{padding:...}}}>`。两处聊天页面（`AgentChatPage`/`CopilotPanel`）都做了同样的修复，用真实的 `getBoundingClientRect`/`scrollHeight`/`clientHeight` 测量 + 实际触发 Harness 演示流式输出验证过，确认新内容会自动保持在可见范围内、不需要用户手动拖动滚动条。

## Harness / Agent Loop 演示改成 AG-UI 协议事件驱动（2026-07-15 新增）

用户提议：既然 Inkwell 后端真实返回的是基于 AG-UI 协议的事件流（[ADR-012](../../docs/03-architecture/adr/ADR-012-client-server-protocol-rest-agui.md)），这两个演示场景是不是也应该直接模拟 AG-UI 协议，而不是一串 ad hoc 的 `setTimeout` + 手写数据结构？

先去查了 [AG-UI 协议官方文档](https://docs.ag-ui.com/concepts/events) 的真实事件分类——确认它确实有一类 **Activity Events**（`ACTIVITY_SNAPSHOT`/`ACTIVITY_DELTA`，带 `activityType` 判别字段 + 任意结构化 `content`/RFC 6902 JSON Patch，专门用来承载"消息之间的结构化进度信息"），这跟 Harness 步骤链/Agent Loop 迭代轮次/Todos 任务清单三种场景概念上是天然吻合的。用户最终选择"要做就做全，字节级模拟真实 SSE/JSON Patch"（而不是折中的"只换内部数据形状，不做协议字节级还原"）。

- **新增 `src/chat/agui/` 目录**：
  - `types.ts`：AG-UI 事件类型定义（`RUN_STARTED`/`RUN_FINISHED`、`TEXT_MESSAGE_START/CONTENT/END`、`TOOL_CALL_START/ARGS/END/RESULT`、`ACTIVITY_SNAPSHOT/DELTA`、`REASONING_START/MESSAGE_START/MESSAGE_CONTENT/MESSAGE_END/END`、`CUSTOM`），字段名和判别字符串都对照官方文档 `EventType` 枚举，没有凑巧编造。
  - `jsonPatch.ts`：最小 RFC 6902 JSON Patch 应用器，只支持本原型用得到的路径形状（`/steps/0` 替换、`/steps/-` 追加）和 `add`/`replace`/`remove` 三种操作，不是完整实现。
  - `sse.ts`：`encodeSSEFrame`（编码成 `data: <json>\n\n` 一帧文本）+ `SSEStreamDecoder`（增量解析，支持半帧缓冲）+ `playAGUITimeline`（按时间线播放：每个事件真的先编码成文本再解码回对象，不是直接把 JS 对象引用传给处理函数——这是"字节级"的关键，不是假装模拟）。
  - `reducer.ts`：`createAGUIEventHandler(setMessages, setReplying)`，把单个 AG-UI 事件翻译成 `ChatMessage` 状态变化；`TOOL_CALL_*` 事件本身只作为"这是一次真实工具调用"的协议真实性存档，可视化效果统一由配套发出的 `ACTIVITY_DELTA` 驱动（详见文件内注释里的取舍说明）。
- **`harnessDemo.ts` 改写**：`runHarnessDemo`/`runAgentLoopDemo` 不再手写一串 `setTimeout` 直接改 `ChatMessage`，而是先构建一份带绝对时刻（`at` 毫秒数）的 `AGUIEvent` 时间线数组，交给 `playAGUITimeline` 播放。两个函数的外部签名（`(userText, setMessages, setReplying)`）完全没变，`isHarnessTrigger`/`isAgentLoopTrigger` 也没变，所以 `AgentChatPage.tsx`/`AgentDesignPage.tsx` 两处调用方不需要改任何代码。
- **验证**：`tsc -b --force` 全部通过；分别在 `AgentChatPage`/`CopilotPanel` 上实际触发 Harness 和 Agent Loop 两个演示，全程监听浏览器 console error/pageerror（均为空），逐帧截图/快照比对——视觉效果跟改造前完全一致（同样的 ThoughtChain 步骤、todos 清单、红绿图标、用量提示位置），证明这是一次纯内部实现层的替换，没有引入任何可见的回归。
- **有意识的取舍（不是偷懒漏做）**：
  - 只实现了两个场景真正用得到的事件子集，没有引入 `STATE_SNAPSHOT`/`STATE_DELTA`/`MESSAGES_SNAPSHOT`/`RAW`/`STEP_STARTED`/`STEP_FINISHED`——这些协议里有，但本原型场景用不上，加了也是摆设。
  - `ACTIVITY_DELTA` 的 `patch` 只用了 `replace`/`add`，`jsonPatch.ts` 也只实现了这两个 op（外加 `remove` 备用）；没有做 `move`/`copy`/`test` 这些 RFC 6902 里更少用的操作。
  - `TOOL_CALL_*` 事件和 `ACTIVITY_DELTA` 事件在本演示里是"配套发出"的（同一个时间点既发一个标识"这是一次工具调用"的事件，又发一个真正驱动 UI 渲染的 Activity 事件），reducer 里对 `TOOL_CALL_*` 基本是空实现——这是有意的取舍：把两条协议通道都摆出来（贴近真实后端可能同时发工具执行记录 + UI 专用摘要两条通道的做法），但只让其中一条真正驱动这个原型的可视化，避免为了"两条通道互相打通"这件事本身去写不必要的桥接逻辑。

## 全部对话内容统一收口到 AG-UI 事件（2026-07-15 补充）

用户进一步要求：不只是 Harness/Agent Loop 这两个"结构化演示"，**普通单条 mock 回复**和**各页面点开就能看到的静态种子历史**也都要走 AG-UI 事件，不能留一份"看起来独立发明的" `ChatMessage` 数据，否则以后接手的人容易把 `ChatMessage` 误当成随便定的展示形状，看不出它实际上是协议事件翻译出来的结果。

- **普通单条 mock 回复**（`useMockChat.ts` 的 `appendReply`，被 `sendMessage`/`retryLast` 共用）：不再自己维护一套 `window.setTimeout` 逐字追加 `content` 的逻辑，改成跟 Harness/Agent Loop 完全一样的套路——构建 `RUN_STARTED → TEXT_MESSAGE_START → TEXT_MESSAGE_CONTENT(逐块) → TEXT_MESSAGE_END → RUN_FINISHED` 时间线，交给 `playAGUITimeline` 播放。原来的 `streamMessage` 函数整个删除（不再需要，留着反而是一份没人用又长得像"另一套实现"的死代码，容易误导人）。
- **`agui/textStream.ts`（新增）**：把"文本按字符分块生成 `TEXT_MESSAGE_CONTENT` 事件"这段逻辑从 `harnessDemo.ts` 挪到这个共享文件（`streamingTextEvents` + `streamingDuration`），`harnessDemo.ts` 和 `useMockChat.ts` 两处流式回复共用同一份节奏，不重复实现。
- **`agui/reducer.ts` 补上 `system` 角色（工具调用气泡）的事件映射**：之前 `TOOL_CALL_*` 事件完全是空实现（只服务于 Harness 内嵌的工具调用，靠配套的 `ACTIVITY_DELTA` 驱动可视化）；现在区分两种情况——带 `parentMessageId`（挂在某个 Harness/Loop activity 下）的走原来的路径（`ACTIVITY_DELTA` 驱动，`TOOL_CALL_*` 空实现）；不带 `parentMessageId` 的独立工具调用（比如一次简单的知识库检索，对应 `system` 角色的 Think 气泡），`TOOL_CALL_START` 建一条 `system` 消息（`toolName` 取自 `toolCallName`），`TOOL_CALL_RESULT` 把结果文本填进 `content`。
- **`agui/replay.ts`（新增）**：静态种子历史不再手写一份长得像 `ChatMessage` 的数组，而是描述成"用户说了什么 + 助手侧发生过哪些 AG-UI 事件"（`SeedStep[]`），用 `replaySeed` 同步重放——内部用一个"假的" `setMessages`（直接把 updater 函数应用到一个局部变量上，不经过 React state）驱动跟实时演示完全同一个 `createAGUIEventHandler`，保证种子数据的"最终定稿状态"和真实演示用的是同一套翻译逻辑，不是两份平行维护、容易跑偏的实现。附带两个小的事件构造helper：`assistantTextEvents`（一次性把整段回复当一个 delta 发出，静态种子不需要真的一块一块流式）、`toolCallEvents`（独立工具调用的 Start→End→Result 三元组）。
- **`AgentChatPage.tsx` 的 `mockConversation` 全部重写**：`s1`（自我介绍，工具调用+回复）、`s2`/`s3`（纯文本一问一答）、`s4`（Harness 完整流程）、`s5`（Agent Loop 完整流程）五个种子会话，全部改成先描述 `SeedStep[]`，再用 `replaySeed(...).map(...)` 重新编号 id/统一时间戳（原来每个会话内所有消息共用同一个显示时间，这个行为保留）。
- **验证**：`tsc -b --force` 全部通过（包括 `useMockChat.ts` ↔ `agui/reducer.ts` 之间故意保留的循环依赖——两边都只在函数体内调用对方的导出，不在模块顶层执行，ESM 循环引用在这种模式下是安全的）；浏览器里逐一点开 `s1`/`s4`/`s5` 三个种子会话 + 发一条不命中任何触发词的普通消息，全程监听 console error/pageerror（均为空），视觉效果跟改造前逐字一致。

## 删除"独立工具调用气泡"（`system` 角色，2026-07-15 补充）

用户发现 `s1`（自我介绍与能力咨询）种子会话里有一条"调用 知识库检索"气泡，追问它跟新设计的 AG-UI 管线是什么关系。排查发现：这条气泡虽然确实走的是 AG-UI 事件（`toolCallEvents` 生成的 `TOOL_CALL_START/END/RESULT`，喂给同一个 `agui/reducer.ts`），没有绕开管线；但**全代码库里只有这一处在用**，而且**没有任何关键词能像 Harness/Agent Loop 那样打字触发它**——只存在于这一条静态种子里，是个孤立的、没法被验证复现的展示用例。用户决定直接删掉。

- **`agui/reducer.ts`**：去掉 `TOOL_CALL_START` 里"没有 `parentMessageId` 就建一条 `system` 消息"的分支，`TOOL_CALL_RESULT` 也去掉对应的内容回填逻辑；`TOOL_CALL_*` 四个事件恢复成纯粹的空实现（仅作为协议真实性存档，配合 Harness 内嵌工具调用的 `ACTIVITY_DELTA` 使用，跟之前 Harness 那部分的处理方式一致）。
- **`agui/replay.ts`**：删掉不再被任何地方使用的 `toolCallEvents` 辅助函数。
- **`useMockChat.ts`**：`ChatRole` 去掉 `"system"`，`ChatMessage` 去掉 `toolName` 字段。
- **`chatBubbleRoles.tsx`**：删掉 `system` 角色的 Bubble 配置（Think + `ToolOutlined` 承载的气泡），`toBubbleItems` 的 `extraInfo` 分支也删掉对应那一支；`ToolOutlined` import 一并清理。
- **`AgentChatPage.tsx`**：`s1` 种子会话去掉 `toolCallEvents(...)` 那一步，只保留"用户问自我介绍 → AI 直接回答"，不再夹一条工具调用气泡。
- **保留了什么**：Harness 演示里内嵌的工具调用（`tool1Id`/`tool2Id`，带 `parentMessageId`）完全不受影响——那部分工具调用的可视化本来就是靠配套的 `ACTIVITY_DELTA` 驱动 ThoughtChain 步骤，不依赖被删掉的 `system` 角色；`ui-spec.md §5.1` 里"工具调用气泡"这个消息类型的展示需求，也仍然由 Harness 的 ThoughtChain（"调用工具：网页搜索"这类步骤）覆盖，不是完全没有工具调用的可视化了，只是不再有一个单独的、更简单的 Think 气泡形式。
- **验证**：`tsc -b --force` 通过；浏览器里点开 `s1` 会话确认工具调用气泡已经消失，只剩用户问题和 AI 回复两条消息，console error/pageerror 均为空。

**教训**：判断一段代码是不是"真孤立"，不能只看它有没有走正确的架构/管线——`system` 角色虽然技术上完全没有绕开 AG-UI 事件管线，但因为全代码库只有一处引用、且没有任何触发路径能复现它，实际上跟"没接入任何东西的死代码"效果相同。排查"这东西有没有用"，除了看它的实现是否规范，还要看它在系统里有没有别的地方真的依赖/触发它——`grep` 一下引用次数和触发路径，比单看代码写得对不对更能回答"这东西是不是该删"这个问题。

## 消息气泡支持 Markdown 渲染（2026-07-15 新增，随后改用官方 `@ant-design/x-markdown`）

user/ai 气泡的正文原来是纯文本展示——真实场景里模型回复几乎总会带 Markdown 格式（列表、加粗、代码、链接），不支持解析的话这些标记符会原样露出来。第一版用了社区常见的 `react-markdown` + `remark-gfm`，用户提醒 Ant Design X 官方就有配套的 `@ant-design/x-markdown`（版本号跟已经在用的 `@ant-design/x` 对齐），改用官方组件：

- **依赖**：`@ant-design/x-markdown`（替换掉 `react-markdown`/`remark-gfm`）。用 [marked](https://github.com/markedjs/marked) 做底层解析，专门为"大模型流式输出"场景设计（低阻塞、流式友好），默认不解释 markdown 源文本里的原始 HTML 标签、不需要 `dangerouslySetInnerHTML`，安全默认值跟 `react-markdown` 是同一个思路，但作为官方配套组件，跟已经在用的 `Bubble`/主题体系更贴合。
- **`src/chat/ChatMarkdown.tsx`（重写）**：不再手写一整套 `components` 样式覆盖，直接用官方内置主题（`@ant-design/x-markdown/themes/{light,dark}.css` + `x-markdown-light`/`x-markdown-dark` 类名，跟应用的 `isDark` 状态联动）；内置主题默认的行间距是给"文档阅读"场景调的，在气泡这种窄容器里偏松散，按官方文档"基于内置主题类叠加自定义类、覆盖 CSS 变量"的方式收紧（`--margin-block`/`--margin-li`/`--margin-pre` 等），且直接通过 `style` 内联设置这些 CSS 变量（不用拼 `<style>` 标签塞进 DOM，每条消息互相独立，也不会在页面里堆重复的样式标签）；链接色顺带同步成当前应用的主色（`token.colorPrimary`），跟三套主题（曜石紫/朱砂橙/碧海青）切换保持一致；`openLinksInNewTab` 交给组件自带的选项，不用手写 `target="_blank"`（还顺带在外部链接后面加了个"↗"图标，官方组件自带的细节）。
- **`chatBubbleRoles.tsx`**：`user`/`ai` 两个角色都加上 `contentRender`，用 `ChatMarkdown` 替代默认的纯文本展示；`footer` 里 `Actions.Copy` 依然拷贝原始 Markdown 源文本（不是渲染后的内容），这是对的——复制应该拷贝可编辑的原始文本，不是渲染结果。
- **`useMockChat.ts` 的 `defaultMockReply`**：特意改成带上加粗/斜体/行内代码/列表/链接/引用块的示例文本，方便在原型里直接打字验证渲染效果，不用另外造数据。
- **流式输出期间的行为**：内容在 `TEXT_MESSAGE_CONTENT` 事件逐块累加时，中途会出现"没闭合的 `**` 或代码块"，这是预期内的、`XMarkdown` 能容忍的短暂现象（闭合之前先按纯文本展示那一小段，不会报错/崩溃）；`XMarkdown` 其实还专门提供了 `streaming`（`hasNextChunk`/`enableAnimation`/`tail` 光标动画等）配置项用来对接真正的流式场景，本次先用最基础的 `content` 用法验证渲染能力，没有接这套配置——如果以后想要更贴近真实"打字机+光标"效果，可以再加。
- **验证**：卡在一个"Vite 依赖预打包缓存过期"的坑——刚装完新包、dev server 还在跑，浏览器报 `504 Outdated Optimize Dep`，重启 dev server（顺带清了 `node_modules/.vite`）后恢复正常。恢复后在 `AgentChatPage` 发一条任意消息，实测加粗/斜体/行内代码/列表/链接/引用块全部正确渲染成真实 DOM 元素（用 DOM `textContent` 核对文本内容完全正确，截图 JPEG 压缩偶尔会让中文字形看起来像别的字，那是截图压缩的视觉伪影，不是真实渲染 bug，排查时要用 DOM 文本核对而不是只看截图）；`tsc -b --force` 通过。
3. 修完 (2) 后一度怀疑又出现"点击/发送完全没反应"的问题，且伴随浏览器主线程被完全占满（`requestAnimationFrame` 排队 20+ 秒都不回调）——一开始怀疑是新代码引入了死循环，重启了 vite dev server、清了端口、开了全新的浏览器 tab 反复验证，结果**全新 tab 完全正常**，问题只出现在这一整个超长会话里反复被拿来试验（多次触发 Harness 演示、多次 reload、多次 HMR）的那一个旧 tab 上，怀疑是该 tab 长期积累的定时器/状态残留把主线程堵住了，并非代码本身的 bug。**教训：长时间反复在同一个浏览器 tab 上做大量交互测试后，如果突然出现"所有交互都没反应"且伴随主线程假死，先怀疑是不是这个 tab 自己积累出了问题（开一个全新 tab 复测），不要急着断定是刚改的代码引入了死循环。**

依赖备注：`ThoughtChain` 从 `@ant-design/x` 顶层直接 `import { ThoughtChain, type ThoughtChainItemType } from "@ant-design/x"` 即可拿到（`node_modules/@ant-design/x/es/index.d.ts` 已导出），不需要额外装包。

**共享核心逻辑**（你构思的“两个只是 UI 不一样，核心逻辑可以共用”已落实为代码，不只是口头结论）：

- `src/chat/useMockChat.ts`：管理一段对话的消息列表 + "发送 → 700ms 后追加 mock 回复"的状态机，两个页面都用同一个 hook，只是传入不同的初始消息/回复文本工厂函数。切换会话（仅 Agent 对话页需要）由调用方自己维护，调 `resetMessages` 重新灌入该会话的历史即可。2026-07-15 补充：hook 现在还顺带管 `input`/`setInput` 状态，暴露 `submit(value?)`（trim 校验 + 发送 + 清空输入框，两处页面此前各自写的三行样板收成一行调用）和 `startNewSession(next?)`（"当前已是空会话"提示 + 清空消息的空会话保护，返回布尔值告诉调用方要不要继续处理自己的会话列表逻辑）——顺带修掉了 `CopilotPanel` 里"新建会话"从来没真正清空过消息的潜在 bug（之前那处 `useMockChat` 调用没解构 `resetMessages`，只切了 `activeSession`）。
- `src/chat/chatBubbleRoles.tsx`：共享的 `Bubble.List` `role` 配置工厂（user/ai 头像+对齐方向，system 角色用 `Think` 承载工具调用）+ `toBubbleItems` 转换函数，两个页面都直接复用，保证气泡视觉风格一致。2026-07-15 补充：新增 `useChatBubbleRoles(onRetry)` hook，把两处页面里完全一样的 `useToken` + `useMemo(() => createChatBubbleRoles(...), [...])` 包装收进去，调用点从 4 行缩成 1 行。
- `src/chat/useAttachments.ts` + `src/chat/ChatAttachmentsHeader.tsx`（2026-07-15 新增）：两处 `Sender` 的“回形针 → `Sender.Header` 展开上传面板”整套状态（`attachmentsOpen`/`files`）和 JSX（`Sender.Header` + `Attachments`，`beforeUpload={() => false}` 拦截真实上传）此前逐字重复，现在收成一个 hook + 一个组件，两处 `Sender` 的 `header` 属性直接传 `<ChatAttachmentsHeader open onOpenChange files onFilesChange />`。这是刻意为“统一两处上传入口行为，为后续接入真实多模态解析”铺路的抽取——以后真要接真实上传，只需要改这一个组件。
- 真正不共用的只有布局层（有无会话侧栏/并排面板 vs 独立页面）和空状态文案（UI-005 的居中起始页大标题 vs 内嵌面板的 `Welcome` + 研究提示词）。

**`CopilotPanel` 打开时的宽度联动**：`CopilotPanel` 固定 400px 且与配置表单并排（而不是遮罩），在较窄视口下会挤占配置表单的可用宽度，叠加 AppShell 主导航（展开 200px）与 Agent 设计页自身的“配置区段”侧边栏（展开 176px）两层已有的固定宽侧栏，三层宽度叠加足以把表单内容区压缩到几乎为零、触发浏览器逐字换行的破坏性渲染（每个字单独占一行）。修复方式：`AgentDesignPage` 新增 `onCopilotOpenChange` 回调 prop，打开/关闭面板时既收起/展开 AppShell 主导航（`AppShellExplorer.tsx` 里 `setCollapsed`），也同步收起/展开自身的“配置区段”侧边栏（`setSiderCollapsed`），三处联动尽量让出宽度；同时给 Agent 头部的名称/版本信息区块和“配置区段”内容面板分别加了 `minWidth`（140px / 280px）兜底——当可用宽度依然不够时，宁可让内容整块换行/被 `overflow:auto` 横向滚动裁切，也不能让 flex 子项在 `minWidth:0` 下无限收缩到发生逐字换行。

依赖：新增 `@ant-design/x@^2.8.0`（peer 依赖 `antd@^6.1.1` 与本原型已用的 antd 6.5.1 兼容）；`RootLayout.tsx` 新增 `XProvider locale={zhCN}` 包裹全站，保证 Ant Design X 组件文案也走 zh-CN（与 ADR-014 不做 i18n 的约定一致）。

## 亮暗主题开关统一为太阳/月亮 `Switch`（2026-07-15 新增）

原来 Design Lab `NavBar` 用的是 `Switch` + `BulbFilled`/`BulbOutlined`（灯泡图标），`AppShellExplorer` 顶栏用的是普通 `Button` + 同款灯泡图标；两处视觉不统一，且灯泡图标语义上更像"开灯/关灯"而不是"亮色/暗色模式"。统一改为 `Switch` + `@ant-design/icons` 的 `SunFilled`/`MoonFilled`（太阳/月亮），`checkedChildren`/`unCheckedChildren` 分别放月亮/太阳，`checked={isDark}` 直接绑 `useDesign()` 的 `isDark`，`onChange={setIsDark}`：

- `NavBar.tsx`：仅替换图标导入（`BulbOutlined/BulbFilled` → `SunFilled/MoonFilled`），`Switch` 本身机制不变。
- `AppShellExplorer.tsx`：顶栏原来的 `Button` 整个换成同款 `Switch`（`size="small"`），行为不变（仍是 `setAppearanceMode`/`setIsDark` 驱动的全局外观切换）。
- "个人设置" Drawer 里 `Segmented`（亮色/暗色/跟随系统）继续用 `BulbOutlined`/`BulbFilled`，未纳入本次统一范围——那是三态选择控件，跟顶栏的二态开关是不同的交互场景，图标语义不冲突就不强行统一。

## 锁定页（UI-002，2026-07-15 新增）

真实桌面客户端（`src/app/desktop/`）已经有可运行的锁定功能（`electron/main.ts` 的 5 分钟闲置计时器 + `powerMonitor.on('lock-screen')` + `browser-window-blur` 双触发、`src/features/auth/lock-page.tsx`），但视觉上只是硬编码 CSS（`src/index.css` 里的十六进制色值），从未经过 ui-spec.md §2 的设计评审——原型里补上这一课，同时借机对照 ui-spec.md §2 / acceptance-criteria.md AC-076~080 / ADR-011 核对真实实现的差距（差距本身只做了分析汇报，未改真实桌面代码）。

新增 `src/components/LockScreen.tsx`，作为可复用的全屏遮罩组件，集成进 `/shell`：

- **触发入口**：用户头像下拉菜单新增"锁定"菜单项（图标 `LockOutlined`），点击直接把 `AppShellExplorer` 的 `locked` 状态设为 `true`，渲染遮罩。真实产品里锁定是闲置/失焦自动触发，没有"手动锁定"菜单项，但原型里给一个手动入口更方便评审，也符合很多真实产品里"账号菜单→锁定屏幕"的常见模式。
- **状态覆盖**（对应 ui-spec.md §2 状态表）：
  - 默认：头像 + "Inkwell 已锁定" 标题 + "{username}，请输入密码继续" 副标题 + 密码框 + "解锁" 按钮 + "切换账号"/"登出" 链接按钮。
  - 解锁中：提交后 700ms 模拟网络往返，按钮 `loading`，表单禁用。
  - 解锁失败（401）：`Alert type="error"` 展示"密码错误，请重试"，密码框清空并重新聚焦。
  - 多次解锁失败/账号锁定（429，对应 AC-080）：连续 3 次失败后 `Alert` 展示"多次解锁失败，账号已临时锁定。请联系管理员"，密码框和按钮永久禁用（本次会话内）。
  - 离线：复用 AppShellExplorer 顶部"设计评审"控制条已有的 `network` 模拟器（而不是新增一个专属的离线开关），`network !== "online"` 时 `Alert` 展示"网络异常，已断开。请检查网络连接"，密码框和"解锁"按钮禁用，但"切换账号"/"登出"仍可点击（这两个操作语义上不依赖当前网络连通性，只是终止本地会话跳回登录页）。
- **密码约定**：任意非空密码都会触发成功解锁（`onUnlock()`），唯一例外是字面值 `"wrong"`（大小写不敏感、自动 trim）会走失败分支——这是故意简化，方便评审时不需要"记住正确密码"，用 `wrong` 这个 magic string 就能预览失败/锁定态。
- **验证**：`tsc -b --force` EXIT:0；通过浏览器交互测试逐一走完默认/解锁中/解锁失败（1 次、2 次）/账号锁定（3 次）/离线/成功解锁/切换账号/登出全部状态，视觉截图确认暗色（曜石紫）主题下 Token 应用正确、无控制台报错。测试过程中发现一个已知的非阻断性 antd 警告：`AppShellExplorer.tsx` 里"切换账号"/"登出"用的是静态 `message.info(...)`（不是 `App.useApp()` 的 context 版），antd v6 会警告"无法感知动态主题"——这是延续代码库里已有的多数写法（`useMockChat.ts`/`AgentDesignPage.tsx` 同样用静态 `message`，只有 `LogoExplorer.tsx` 用了 `App.useApp()`），未在本次改动中处理，如果以后要统一成 `App.useApp()` 需要额外把整页包一层 `<App>`，属于更大范围的改动。
- **未纳入范围**：ui-spec.md §2.5 OQ-017"在途任务特例"（锁屏期间保留录音/上传/流式回复至完成，解锁后呈现结果）本次未在原型里模拟，纯静态遮罩不涉及任何真实的进行中任务；如果后续需要评审这一细节，需要专门设计一个"锁屏时后台有任务在跑"的模拟场景。

## 视口与截图

Playwright 配置覆盖：

- 1440 × 900：desktop-hd
- 1280 × 800：desktop-md
- 768 × 720：tablet
- 390 × 844：mobile

`npm run screenshot` 生成以下评审证据到 `screenshots/`：

- `01`–`02`：Design Lab 桌面与移动端
- `03`–`04`：三主题对比与平板视口
- `05`：正式 Logo 桌面视图
- `06`–`07`：工作台分栏桌面与移动视图
- `08`–`09`：Agent 会话面板与平板视图

截图测试同时检查主题、Logo、登录与 Agent 关键交互，以及页面横向溢出。表格允许自身横向滚动，但页面根节点不得产生横向滚动。

## 人工评审项

- 从三套主题中选择主方向，并决定是否保留独立暗色 Token
- 确认正式 Logo 在主题色、深色背景与小尺寸下的适配方式
- 确认工作台分栏在窄窗口下的降级方式
- 在 Agent 标准密度与紧凑密度之间确定默认值
- 决定 Agent 区段导航在窄窗口下采用折叠侧栏还是后续改为其他导航方式
