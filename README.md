# Inkwell

**AI 驱动的内容生产平台** -- 基于 [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) 构建

> **声明**: 本项目的业务需求完全由 AI 辅助生成，并非面向生产环境的商业产品。项目的核心目的是通过一个贴近真实的业务场景，系统性地学习和掌握 Microsoft Agent Framework (MAF) 的各项能力，包括多 Agent 协作、Workflow 编排、对话持久化、长期记忆等。

## 技术栈

| 层级     | 技术                                                                         |
| -------- | ---------------------------------------------------------------------------- |
| 后端     | .NET 10 / ASP.NET Core                                                       |
| AI 编排  | Microsoft Agent Framework 1.1.0                                              |
| LLM      | Microsoft.Extensions.AI + Azure OpenAI                                       |
| 持久化   | Entity Framework Core (InMemory / SQL Server)                                |
| 向量存储 | Microsoft.Extensions.VectorData + InMemory (可切换 Qdrant / Azure AI Search) |
| 前端     | React 19 + TypeScript + Ant Design 6 + Ant Design X                          |

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

- AG-UI 流式对话（SSE 逐字输出 + Markdown 渲染）
- 会话侧栏（历史列表、搜索、切换、导出）
- Workflow 拓扑可视化（Mermaid）+ AGUI 对话式运行

### 基础设施

- 多模型服务（Primary / Secondary Keyed IChatClient）
- 可插拔持久化与向量存储
- JWT 认证授权、OpenTelemetry 追踪
- DurableTask 托管（Console + Azure Functions）

## 快速开始

### 前置要求

- .NET 10 SDK
- Azure OpenAI 服务（Chat Completion + Embedding 部署）
- Node.js 20+

### 配置

```bash
cd src/app/webapi/Inkwell.WebApi
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:Primary:Endpoint" "https://your-resource.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:Primary:DeploymentName" "gpt-4o"
dotnet user-secrets set "AzureOpenAI:Primary:ApiKey" "your-api-key"
```

### 运行

```bash
# 后端
dotnet run --project src/app/webapi/Inkwell.WebApi

# 前端
cd src/app/webapp
npm install
npm run dev
```

访问 http://localhost:5188

## 许可证

MIT

访问 http://localhost:5188

## 许可证

MIT
