# Inkwell 需求文档

## 项目定位

Inkwell 是一个 AI 驱动的内容生产平台，通过 Microsoft Agent Framework（MAF）编排多个 AI Agent 和人工环节，实现从选题到发布的端到端自动化。项目的双重目标：

1. **业务目标**：构建可用的内容生产流水线
2. **技术目标**：通过真实场景完整覆盖 MAF 的核心能力

---

## Phase 1：核心流水线（已完成）

### 需求 1.1：选题分析

**用户故事**：作为内容运营人员，我希望输入一个主题后，系统能自动从市场趋势和竞品内容两个维度进行分析，为后续写作提供方向。

**验收标准**：
- 输入：主题字符串（如 "The Future of AI in Healthcare"）
- 两个分析维度并行执行，互不阻塞
- 两个分析都完成后，合并为一份统一的分析报告
- 分析报告存入共享状态，供后续环节引用
- 输出阶段性结果通知（选题分析完成）

**MAF 能力**：Fan-Out / Fan-In Barrier、SharedState、YieldOutputAsync

### 需求 1.2：内容创作

**用户故事**：作为内容运营人员，我希望系统基于选题分析报告自动撰写文章，并经过 AI 审核，不通过则自动修改，直到通过或达到最大修订次数。

**验收标准**：
- Writer Agent 基于分析报告撰写 300-500 字的文章
- Critic Agent 审核文章，给出通过/退回决策和反馈
- 退回时 Writer 读取审核反馈，在原稿基础上修改
- 最多修订 3 次（可配置），超过后强制通过
- 修订次数通过 Executor 内部状态跟踪

**MAF 能力**：Writer-Critic Loop、AddSwitch 条件路由、SharedState

### 需求 1.3：人工审核

**用户故事**：作为终审编辑，我希望在 AI 审核通过后能看到完整文章，决定是否批准发布。退回则触发重写。

**验收标准**：
- Workflow 暂停，展示文章标题、版本号、内容
- 编辑输入 y 批准 / n 退回
- 批准：输出最终文章
- 退回：触发 Writer 重写循环

**MAF 能力**：Human-in-the-Loop（RequestPort）、ReviewGateExecutor

---

## Phase 2：高级模式（计划中）

### 需求 2.1：多语言翻译

**用户故事**：作为国际内容运营人员，我希望一篇文章能同时翻译成多种语言（中文、日文、法文等），翻译结果汇总后统一发布。

**验收标准**：
- 文章发布后，Fan-Out 到多个翻译 Agent
- 每个翻译 Agent 独立工作，互不影响
- 所有翻译完成后 Fan-In 汇总
- 翻译结果与原文一起存入共享状态

**MAF 能力**：Fan-Out / Fan-In（第二层并行）、Agent 绑定

### 需求 2.2：SEO 优化

**用户故事**：作为 SEO 专员，我希望系统能自动分析文章的关键词密度、标题评分等 SEO 指标，并给出优化建议。

**验收标准**：
- 通过函数工具调用 SEO 分析逻辑
- 分析结果反馈给 Writer Agent 进行优化
- SEO 评分达标后进入下一环节

**MAF 能力**：Function Tool

### 需求 2.3：条件发布路由

**用户故事**：作为发布管理员，我希望根据内容类型和目标渠道，自动路由到不同的发布流程（博客、社交媒体、邮件通讯等）。

**验收标准**：
- 根据文章元数据（类型、标签、长度）条件路由
- 不同发布渠道有不同的格式化需求
- 多渠道可并行发布

**MAF 能力**：AddSwitch、AddEdge\<T\> 条件路由

### 需求 2.4：Checkpoint 恢复

**用户故事**：作为运维人员，我希望长时间运行的流水线在中断后能从断点恢复，不用从头开始。

**验收标准**：
- 每个 SuperStep 完成后生成 Checkpoint
- 进程重启后从最近的 Checkpoint 恢复
- 共享状态在恢复后保持一致

**MAF 能力**：CheckpointManager、ResumeStreamingAsync

---

## Phase 3：可观测与可视化（计划中）

### 需求 3.1：执行追踪

**用户故事**：作为开发者，我希望能看到流水线每个环节的执行耗时和 Token 消耗，快速定位性能瓶颈。

**验收标准**：
- 每个 Executor 的执行时间可在 Application Insights / Aspire Dashboard 中查看
- 完整的分布式 Trace 链路
- 支持 `EnableSensitiveData` 查看消息内容

**MAF 能力**：WithOpenTelemetry、ActivitySource

### 需求 3.2：Workflow 可视化

**用户故事**：作为技术负责人，我希望能导出流水线的结构图，用于文档和团队沟通。

**验收标准**：
- 导出 Mermaid 格式（嵌入 README）
- 导出 DOT 格式（生成 SVG/PNG）

**MAF 能力**：ToMermaidString、ToDotString

---

## Phase 4：声明式与服务化（计划中）

### 需求 4.1：YAML 模板

**用户故事**：作为非技术用户，我希望通过 YAML 文件定义简化版的内容流水线，不需要写 C# 代码。

**验收标准**：
- 提供简化版 YAML 模板（顺序写作流程）
- 通过 `DeclarativeWorkflowBuilder.Build` 加载运行
- 运行结果与代码版本一致

**MAF 能力**：DeclarativeWorkflowBuilder、Power Fx 表达式

### 需求 4.2：Code Eject

**用户故事**：作为开发者，我希望将 YAML 模板转换为 C# 代码，方便进一步自定义。

**验收标准**：
- 从 YAML 生成等价的 C# 代码
- 生成的代码可编译运行

**MAF 能力**：DeclarativeWorkflowBuilder.Eject

### 需求 4.3：Workflow as Agent API

**用户故事**：作为系统集成者，我希望整个流水线封装为一个 Agent，通过 API 接口调用。

**验收标准**：
- 通过 `workflow.AsAIAgent()` 封装
- 外部调用者看到的是标准 Agent 接口
- 支持流式输出

**MAF 能力**：AsAIAgent、WorkflowHostAgent

---

## Phase 5：生产级（计划中）

### 需求 5.1：选题讨论会

**用户故事**：作为内容团队负责人，我希望多个角色 Agent（市场、编辑、SEO）能轮流发言讨论选题，最终达成共识。

**验收标准**：
- 3-5 个 Agent 参与讨论
- GroupChatManager 控制发言顺序
- 讨论达到共识或最大轮次后结束

**MAF 能力**：GroupChatWorkflowBuilder、RoundRobinGroupChatManager

### 需求 5.2：子流程复用

**用户故事**：作为架构师，我希望"内容创作"模块能作为独立子流程，嵌入到不同的主流程中复用。

**验收标准**：
- "创作子流程"（Writer-Critic Loop）封装为独立 Workflow
- 通过 `SubworkflowBinding` 嵌入主流程
- 子流程的共享状态与主流程隔离

**MAF 能力**：SubworkflowBinding、BindAsExecutor

### 需求 5.3：分布式执行

**用户故事**：作为运维人员，我希望流水线在生产环境下通过 Azure Functions 持久化运行，服务重启后自动恢复。

**验收标准**：
- 通过 DurableTask 模式执行
- Agent 作为 Durable Entity 持久化
- Azure Functions 托管

**MAF 能力**：DurableTask、Azure Functions Hosting

---

## 非功能性需求

### 安全
- 所有密钥通过 user-secrets 或环境变量管理，不硬编码
- `EnableSensitiveData` 仅在开发环境启用

### 代码质量
- 所有公共类和方法有 XML 文档注释
- 遵循 .editorconfig 规范
- file-scoped namespace

### 可维护性
- 每个 Executor 职责单一
- 共享状态通过 `StateScopes` 常量管理
- Workflow 拓扑通过注释清晰记录
