# 我用 Microsoft Agent Framework 造了一个 AI 内容工厂：从选题到发稿全自动

---

> **声明 —— 这是一次 Harness Engineering 实战**
>
> 本项目从需求分析、架构设计、模块拆分、代码实现、Bug 修复到全部技术文档，**几乎 100% 由 AI 自主完成**。作者全程没有手写一行业务代码，承担的只有三件事：
>
> - **编写 Copilot Instructions**：把架构约束、编码规范、领域知识沉淀成可被 AI 稳定消费的上下文
> - **把控方向**：确认需求边界、审阅关键设计、决定取舍
> - **反馈问题**：把运行结果、错误现象、Bad Case 精准地喂回给 AI，驱动它自我修正
>
> 这不是「AI 辅助编程」，而是 **Harness Engineering（AI 驾驭工程）**——人退到指挥位，把 AI 当作可以独立交付的工程团队来使用。本文记录的不只是一个项目，也是这条新工作方式的一个完整样本。

---

## 写在前面

**Microsoft Agent Framework（MAF）** 把 Semantic Kernel 的工程能力和 AutoGen 的多 Agent 协作思路融到了一起，是目前 .NET 生态里最完整的 Agent 开发框架。

这个项目的目标是用 MAF **真刀真枪**地造出一个业务系统——不是 Hello World，也不是 Demo，而是一个能跑通完整业务闭环、经得起代码审查的**企业级 AI 内容生产平台**。

项目叫 **Inkwell**（墨水瓶），灵感来自内容创作者案头那件老物件。它可以对话、可以编排、可以自我审核，代码完全开源：

**GitHub 仓库**：https://github.com/shuaihuadu/Inkwell

---

## 这不是又一个 ChatGPT 套壳

市面上的 AI 应用模板大多是「套个聊天框 + 调一下模型 API」。Inkwell 想做的事情不一样：

**它模拟了一个真实内容团队的完整工作流**——从一个主题出发，走完「市场调研 → 竞品分析 → 写作 → 审核 → 人工终审 → 发布」，中间有多个 AI Agent 互相协作，也有人工审核介入，每一步都有迹可循。

如果你见过企业内容运营团队是怎么跑流程的，在 Inkwell 里能找到对应：

| 真实团队岗位  | Inkwell 对应的 Agent / Workflow               |
| ------------- | --------------------------------------------- |
| 市场研究员    | MarketAnalysisExecutor（输出结构化 JSON）     |
| 竞品分析师    | CompetitorAnalysisExecutor                    |
| 内容写手      | WriterAgent（支持多轮修订）                   |
| 内容编辑/审核 | CriticAgent（打分 + JSON 决策）               |
| 主编终审      | RequestPort（真实的 Human-in-the-Loop 端口）  |
| SEO 专员      | SEO Agent（Function Tools 调用搜索）          |
| 多语种翻译    | TranslationAggregationExecutor（Fan-In 聚合） |

整条流水线通过 **MAF 的 Workflow 引擎** 编排，每个节点都是真实的 Executor，有真实的状态管理、错误处理和可观测性。

---

## 一个场景走完整条流水线

来看一个最典型的场景：**用户输入"效率革命与边界新思考"，平台自动产出一篇经过三重审核的文章**。

### 第一步：选题分析（Fan-Out / Fan-In 并行）

用户一句话丢进去，`InputDispatchExecutor` 把它同时扇出到两个分支：
- **市场趋势分析师**：调研该主题当前的市场热度、目标受众、内容机会
- **竞品分析师**：分析市场上已有的同类内容，提出差异化角度

这两个 Agent 用 `ChatResponseFormat.ForJsonSchema<TopicAnalysis>()` 强制输出结构化 JSON，回来后在 `AnalysisAggregationExecutor` 里 Fan-In 汇聚为统一的选题分析报告。

这里踩过一个很典型的坑——最早汇聚节点用实例字段缓存结果，跑一次没问题，跑第二次就空输出了。后来才意识到 **MAF 的 Executor 是单例**，跨运行的状态必须托管给 `IWorkflowContext.QueueStateUpdateAsync`。修完这个 Bug 的那一刻，对 MAF「工作流即有状态图」的理念理解就深了一层。

### 第二步：Writer-Critic 循环（自动修订）

选题分析完成后进入创作环节。**WriterExecutor** 根据分析报告写文章，**CriticExecutor** 从质量、准确性、吸引力、完整性四个维度打分并决策：

- 审核通过 → 进入人工终审
- 审核退回 → 带着反馈意见回到 Writer，最多修订 3 次

这整个循环在 MAF 里是用 **AddSwitch** 实现的：

```csharp
.AddSwitch(critic, sw => sw
    .AddCase<Article>(a => a?.Status == ArticleStatus.Approved, reviewPort)
    .WithDefault(writer))
```

一段代码就把"条件分支 + 循环回退"表达清楚，这比传统 BPMN 引擎友好太多了。

### 第三步：真实的人工审核（Human-in-the-Loop）

这是整个项目里最值得拿出来讲的一部分。

MAF 提供了 `RequestPort.Create<TInput, TResponse>()`，Workflow 跑到这里会**真正挂起**，等待外部响应。Inkwell 把它接到了前端：

```
Workflow 命中 RequestPort
    ↓
HitlPendingRegistry 登记挂起态（RequestId + StreamingRun）
    ↓
SSE 通道向前端发 <<<HITL_REQUEST:{json}>>> 标记
    ↓
前端 ThoughtChain 节点里渲染「通过 / 退回」按钮
    ↓
用户点击 → POST /api/hitl/{id}/respond
    ↓
StreamingRun.SendResponseAsync() 唤醒 Workflow 继续执行
```

**长连接在用户思考期间一直保持打开**，Workflow 状态在服务端挂起等待，点击按钮的瞬间无缝恢复——这才是真正意义上的 HITL，不是「假装自动批准」的摆设。

### 第四步：发布与退回

人工点击「通过」：`ReviewGateExecutor` 把文章落库到 `Articles` 表，状态改为 Published，YieldOutputAsync 产出最终内容。

人工点击「退回」：文章带着"Human reviewer requested revisions"的反馈回流到 WriterExecutor，触发下一轮修订。

---

## 三个最值得展示的工程亮点

### 亮点一：把 Workflow 伪装成 Chat Agent

MAF 的 Workflow 和 Agent 天然是两套接口，AG-UI 协议又是 Agent 专用的——那 Workflow 怎么接到聊天界面？

Inkwell 的答案是一层薄薄的 `WorkflowChatClient : IChatClient`：

- 用户输入 → 从 ChatMessage 抽出文本 → 喂给 `InProcessExecution.RunStreamingAsync`
- Workflow 流出的各种 Event → 映射成 `ChatResponseUpdate`
  - `AgentResponseUpdateEvent` → 流式 token
  - `WorkflowOutputEvent` → JSON 序列化为产出内容
  - `RequestInfoEvent` → 发 HITL 标记
  - `WorkflowErrorEvent` → 可见化错误

于是 Workflow 就成了「一个能执行复杂流程的 Agent」，Agent 的所有基础设施（AG-UI、会话持久化、AI Toolkit 调试、A2A 协议）**一行不改直接复用**。

这是 MAF 设计最优雅的地方之一——**Agent 和 Workflow 是对偶的**。

### 亮点二：思维链式的可视化

流式文本很容易变成一坨混乱的信息。Inkwell 用 Ant Design X 的 **ThoughtChain** 把 Workflow 的每一步可视化了：

```
✓ Workflow 已启动
✓ 选题分析汇总
    主题：效率革命与边界新思考
    市场趋势：...
    目标受众：...
    内容角度：...
✓ 内容写作
✓ 内容审核
⏳ 等待人工终审  [通过] [退回]
```

前端通过正则解析 `[系统]` / `[AnalysisAggregation]` / `[已发布]` 等段落标记，自动映射为步骤节点，流式中最后一个节点为 loading 状态，完成后变成 ✓，出错变成 ✗。体验上接近 Claude、GitHub Copilot Agent 的「思考过程可视化」。

### 亮点三：可观测、可调试、可部署

一个严肃的 AI 应用必须能投入生产。Inkwell 在这方面下了不少功夫：

- **OpenTelemetry 追踪**：每一次 Agent 调用、Workflow 执行都有完整的 Span
- **Aspire Dashboard**：本地开发一键启动，日志/追踪/指标三合一
- **JWT 认证 + 基于策略的授权**：EditorOrAdmin / ViewerOrAbove 等角色隔离
- **DurableTask 托管**：长耗时 Workflow 可选用 DurableTask 跑，重启不丢状态
- **A2A Server**：Agent-to-Agent 协议端口，可被其他系统远程调用
- **Docker Compose 一键部署**：7 个服务一起起，WebApi、WebApp、A2A、DTS、SQL Server、Aspire Dashboard

---

## 技术栈

| 层级     | 技术                                                      |
| -------- | --------------------------------------------------------- |
| 后端     | .NET 10 + ASP.NET Core + Microsoft Agent Framework |
| LLM      | Microsoft.Extensions.AI + Microsoft Foundry               |
| 持久化   | Entity Framework Core（InMemory / SQL Server）            |
| 向量存储 | Microsoft.Extensions.VectorData + InMemory（可切 Qdrant） |
| 前端     | React 19 + TypeScript + Ant Design 6 + Ant Design X       |
| 容器化   | Docker Compose                                            |
| 可观测   | OpenTelemetry + Aspire Dashboard                          |

---

## 现已实现的完整能力清单

### 8 条 Workflow
涵盖 **Fan-Out/Fan-In、Switch、HITL、GroupChat、Handoff、SubWorkflow、MapReduce、Checkpoint** 所有核心模式

### 10+ 预定义 Agent
内容写手 / 审核 / 市场分析 / 竞品分析 / SEO / 图片分析 / 智能调度 / 多语种翻译 / 声明式 YAML Agent

### 完整中间件栈
内容安全护栏（滑窗匹配） + 函数调用审计（流式支持）

### Skills
Markdown 校验、可读性分析、敏感词扫描（所有 Skill 都是独立进程执行）

### 记忆系统
会话级：基于 MAF AgentSession 序列化，跨请求、跨重启保持上下文
长期记忆：ChatHistoryMemoryProvider + 向量存储语义检索

### 前端体验
AG-UI 流式 + 思维链可视化 + 会话侧栏（搜索/重命名/导出）+ Workflow 拓扑图（Mermaid）

---

## 为什么值得一看

如果你：

- 想学 **Microsoft Agent Framework** 又找不到足够完整的参考项目 → Inkwell 有几十个互相衔接的模块，每一个都是真实可跑的
- 想把 AI Agent 落到 **企业级业务场景** → Inkwell 展示了从对话到工作流、从单 Agent 到多 Agent、从全自动到 HITL 的完整路径
- 在做 **.NET + AI** 的工程实践 → 少见的不妥协的 .NET 10 + MAF 组合，没有为了赶潮流切回 Python
- 想要一个**可以直接运行**的参考实现 → `docker compose up -d` 就能跑起来，不需要配环境

---

## 一键启动

```bash
git clone https://github.com/shuaihuadu/Inkwell.git
cd Inkwell/docker
cp .env.example .env
# 编辑 .env 填入 Microsoft Foundry 的 Endpoint 和 ApiKey
docker compose up -d
```

浏览器打开 http://localhost:3000，开始你的第一次 AI 内容生产之旅。

---

## 结语：一份可回溯的 Harness Engineering 样本

Inkwell 是一个**学习型开源项目**，不面向生产，而是两条主线的交汇：

- **向内**：系统性掌握 Microsoft Agent Framework 的真实演练场
- **向外**：一份完整的 **Harness Engineering 工作法**样本

从空仓库到一个能跑的平台，作者没有手写一行业务代码。所有架构取舍、代码风格、Bug 定位过程，全部沉淀在 **Git 提交历史 + Copilot Instructions + Chat 对话记录**里，任何人都能逐步回溯：

- AI 在哪些节点自主做出了正确决策
- 在哪里需要作者介入矫偏
- 一份好的 Copilot Instructions 能把 AI 的输出质量提升多少

如果你正在思考 **AI 时代开发者的角色该怎么重新定义**，Inkwell 或许能给你一个观察样本。

如果觉得有帮助，欢迎：

- **Star**：https://github.com/shuaihuadu/Inkwell
- **Issue / PR**：任何问题、建议、补充都欢迎
- **转发**：让更多 .NET + AI 开发者看到它

---

**仓库地址**：https://github.com/shuaihuadu/Inkwell

**Microsoft Agent Framework 官方**：https://github.com/microsoft/agent-framework

**Ant Design X**：https://x.ant.design

---

*愿墨水瓶里的每一滴墨，都能化作有温度的文字。*
