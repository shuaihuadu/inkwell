# Inkwell

**AI 驱动的内容生产平台** -- 基于 [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) 构建

> **声明**: 本项目的业务需求完全由 AI 辅助生成，并非面向生产环境的商业产品。项目的核心目的是通过一个贴近真实的业务场景，系统性地学习和掌握 Microsoft Agent Framework (MAF) 的各项能力，包括多 Agent 协作、Workflow 编排、对话持久化、长期记忆等。

## 业务场景

Inkwell 模拟了一个**企业级 AI 内容生产平台**的完整工作流。在这个场景中，用户（内容运营人员）可以：

**与 AI 对话创作内容**

- 选择不同的专业 Agent 进行对话：内容写手负责撰写文章，市场分析师分析选题趋势，SEO 专家优化搜索排名，翻译 Agent 将内容翻译成多语言
- 对话过程支持流式输出，Agent 可以调用搜索工具获取最新资讯，使用 Markdown 校验和敏感词扫描等技能辅助创作
- 智能调度 Agent 作为入口接待用户，根据需求自动推荐合适的专业 Agent

**通过 Workflow 自动化生产流程**

- 内容流水线：选题分析（市场+竞品并行）→ AI 写作 → AI 审核 → 人工终审 → 发布，全流程自动编排
- Writer-Critic 循环：写作和审核 Agent 反复迭代，直到文章质量达标或达到最大修订次数
- 翻译流水线：一篇文章同时翻译为多语言（Fan-Out），翻译完成后汇聚结果（Fan-In）
- 选题讨论会：多个 Agent（市场分析师、内容编辑、SEO 专家）围绕选题轮流讨论，模拟团队会议
- 智能路由：根据用户问题类型，自动切换到写作、SEO 或翻译专家处理

**管理对话和知识**

- 所有对话自动持久化，刷新页面或重启服务后可继续之前的对话
- 历史会话列表支持搜索、重命名、删除和 Markdown 导出
- Agent 具备长期记忆能力，能跨会话检索之前讨论过的内容
- 知识库支持上传参考文档，Agent 在创作时自动检索相关资料

**可视化管理**

- Dashboard 展示文章、流水线运行等统计数据
- Workflow 拓扑图可视化（Mermaid），直观了解流程结构
- 所有 Workflow 可通过对话界面直接运行，实时查看执行进度

## 技术栈

| 层级     | 技术                                                                              |
| -------- | --------------------------------------------------------------------------------- |
| 后端     | .NET 10 + ASP.NET Core + Microsoft Agent Framework 1.1.0                          |
| LLM      | Microsoft.Extensions.AI + Microsoft Foundry                                       |
| 持久化   | Entity Framework Core（InMemory / SQL Server）                                    |
| 向量存储 | Microsoft.Extensions.VectorData + InMemory（可切换 Qdrant / Azure AI Search）     |
| 前端     | React 19 + TypeScript + Ant Design 6 + Ant Design X                               |
| 容器化   | Docker Compose（WebApi、Webapp、A2A、DTS Emulator、SQL Server、Aspire Dashboard） |

## 已实现功能

### Agent 能力

- 10+ 预定义 Agent（内容写手、审核、市场分析、竞品分析、SEO、图片分析、智能调度、翻译等）
- Function Tools、结构化输出、Agent-as-Tool、声明式 YAML Agent
- 中间件管线（内容安全护栏 + 函数调用审计，支持流式）
- Skills（Markdown 校验、可读性分析、敏感词扫描）

### Workflow 编排

- 8 条 Workflow，覆盖 Fan-Out/Fan-In、Switch、HITL、GroupChat、Handoff、SubWorkflow、MapReduce、Checkpoint
- 所有 Workflow 通过 AsAIAgent() + AG-UI 暴露为对话式端点

### 对话与记忆

- 会话持久化（基于 MAF AgentSession 序列化，跨请求保持上下文）
- 聊天裁剪（Pipeline 策略：工具结果压缩 / 摘要 / 截断）
- 长期记忆（ChatHistoryMemoryProvider + 向量存储语义检索）
- 会话管理 API（列表、搜索、重命名、删除、Markdown 导出）

### 前端

- AG-UI 集成
- 会话侧栏（历史列表、搜索、切换、导出）
- Workflow 拓扑可视化（Mermaid）+ AGUI 对话式运行

### 基础设施

- 多模型服务（Primary / Secondary Keyed IChatClient）
- 可插拔持久化与向量存储
- JWT 认证授权、OpenTelemetry 追踪
- DurableTask 托管
- Docker Compose 一键部署（7 个服务）
- Aspire Dashboard 可观测性（日志、追踪、指标）

## 快速开始

### 方式一：Docker Compose（推荐）

最快的方式，无需安装 .NET SDK 和 Node.js：

```bash
git clone https://github.com/shuaihuadu/Inkwell.git
cd Inkwell/docker
cp .env.example .env
# 编辑 .env 填入 Azure OpenAI 的 Endpoint 和 ApiKey
docker compose up -d
```

启动完成后访问 http://localhost:3000

> 需要正确配置 Azure OpenAI 的 Endpoint、Deployment Name 和 ApiKey 后，Agent 对话功能才能正常使用。
> 完整的 Docker 部署文档见 [docker/README.md](docker/README.md)。

### 方式二：本地开发

#### 前置要求

- .NET 10 SDK
- Node.js 20+
- Microsoft Foundry 服务（Chat Completion + Embedding 部署）

#### 配置

```bash
cd src/app/services/Inkwell.WebApi
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:Primary:Endpoint" "https://your-resource.services.ai.azure.com/"
dotnet user-secrets set "AzureOpenAI:Primary:DeploymentName" "gpt-4o"
dotnet user-secrets set "AzureOpenAI:Primary:ApiKey" "your-api-key"
```

#### 运行

```bash
# 后端
dotnet run --project src/app/services/Inkwell.WebApi

# 前端
cd src/app/webapp
npm install
npm run dev
```

访问 http://localhost:5188

### 方式三：.NET Aspire

本地开发另一选择，自带可观测性 Dashboard：

```bash
dotnet run --project src/app/aspire/Inkwell.AppHost
```

## 交流群

项目在持续迭代，如果你对 Microsoft Agent Framework 或 Inkwell 的实现细节感兴趣，欢迎扫码加入微信交流群，一起讨论 MAF 的使用经验、踩坑记录与最佳实践。

<img src="wechat-group.jpg" alt="Inkwell 微信交流群" width="240" />


> 二维码失效时欢迎在 Issues 中反馈。

## 许可证

MIT
