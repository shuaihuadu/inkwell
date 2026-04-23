# Inkwell 多 AI Provider 支持设计方案

> 命名约定：本文统一使用 `AIProvider` 作为"AI 模型供应商"层的命名前缀，避免与 `Inkwell.Abstractions/Models/` 下的数据模型（`ArticleRecord` / `SessionInfo` 等）混淆。

## 1. 背景与现状

目前 Inkwell 只接入了 Azure OpenAI，配置节固定为 `AzureOpenAI`，注册方法名 `UseAzureOpenAI`。但 MAF 官方示例已覆盖 16 种 AI 供应商，真实业务里也存在多种诉求：国产模型（DeepSeek / Qwen / Moonshot / 智谱）、Claude、本地 Ollama、AWS Bedrock 等。本设计明确"如何优雅地支持多供应商"。

### 1.1 当前耦合度评估

好消息：**业务代码已经与 Azure OpenAI 完全解耦**。

- 所有 Agent / Workflow / Middleware 依赖的是 `Microsoft.Extensions.AI.IChatClient` 与 `IEmbeddingGenerator<string, Embedding<float>>`。
- 仓库里没有一处业务代码直接引用 `Azure.AI.OpenAI` 的类型。

**仅有三个耦合点**：

| 耦合点 | 位置 | 性质 |
|--------|------|------|
| 注册入口 | `src/app/services/Inkwell.WebApi/AzureOpenAIServiceCollectionExtensions.cs` | 方法名 / NuGet 引用绑定到 Azure |
| 配置模型 | `src/core/Inkwell.Abstractions/AzureOpenAIOptions.cs` | 类名 / 字段名绑定到 Azure（`Endpoint` / `DeploymentName`） |
| 配置节名 | `appsettings.json` 的 `"AzureOpenAI"` 节 | 字符串绑定到 Azure |

**结论**：抽象已经到位，只需要新增一层"多供应商注册适配"。

---

## 2. 可接入的供应商盘点

`Microsoft.Extensions.AI` 生态已经为主流供应商提供了 `.AsIChatClient()` 扩展：

| 供应商 | NuGet 包 | Chat | Embedding | 备注 |
|--------|---------|------|-----------|------|
| Azure OpenAI | `Azure.AI.OpenAI` | ✅ | ✅ | 当前默认 |
| OpenAI 官方 | `OpenAI` | ✅ | ✅ | 与 Azure 同源 SDK，切换成本极低 |
| Anthropic Claude | `Anthropic.SDK` | ✅ | ❌ | 官方 .NET SDK + MEAI 适配 |
| Ollama（本地） | `OllamaSharp` | ✅ | ✅ | 本地模型首选，开发场景好用 |
| Google Gemini | `Mscc.GenerativeAI.Microsoft` | ✅ | ✅ | 非官方但活跃 |
| AWS Bedrock | `AWSSDK.BedrockRuntime` + MEAI 适配 | ✅ | ✅ | 企业云选型 |
| DeepSeek / 通义 / 智谱 / Moonshot / Groq / Together / OpenRouter | 走 OpenAI 兼容端点 | ✅ | ⚠️ | 绝大多数国产 / 第三方模型都提供 OpenAI 兼容协议，复用 `OpenAI` 包修改 `BaseUrl` 即可 |
| ONNX / 本地 GGUF | `Microsoft.Extensions.AI.OnnxRuntimeGenAI` | ✅ | ⚠️ | 离线部署场景 |

**关键洞察**：只要供应商实现 OpenAI 兼容协议（DeepSeek、Moonshot、Qwen、Zhipu 等均支持），都能用一个 `OpenAICompatibleChatProvider` 统一覆盖——这是**投入产出比最高的扩展点**。

---

## 3. 核心设计

### 3.1 设计思路

把"一段 `AzureOpenAI` 配置 → 一个 `IChatClient`"升级为：

- **命名化槽位**：配置里可以命名任意多个模型（`primary` / `secondary` / `creative` / `local` / `deepseek` ...）
- **供应商可插拔**：每种供应商实现 `IAIChatProvider`，注册时查表分发
- **逻辑键与物理槽位解耦**：`Routing` 小节把 `Primary` / `Secondary` 这种"逻辑角色"映射到具体命名槽位，业务代码不变
- **Keyed DI 沿用**：通过 `AIProviderKeys.Primary/Secondary` 的 Keyed Singleton 供业务消费

### 3.2 新配置结构（appsettings.json）

```jsonc
{
  "AIProviders": {
    "Chat": {
      "primary":    { "Provider": "AzureOpenAI",      "Endpoint": "https://...", "Deployment": "gpt-4o",           "ApiKey": "" },
      "secondary":  { "Provider": "OpenAI",           "Model": "gpt-4o-mini",                                       "ApiKey": "sk-..." },
      "creative":   { "Provider": "Anthropic",        "Model": "claude-sonnet-4",                                   "ApiKey": "..." },
      "local":      { "Provider": "Ollama",           "Endpoint": "http://localhost:11434", "Model": "qwen2.5:14b" },
      "deepseek":   { "Provider": "OpenAICompatible", "BaseUrl": "https://api.deepseek.com/v1", "Model": "deepseek-chat", "ApiKey": "..." }
    },
    "Embedding": {
      "default":    { "Provider": "AzureOpenAI", "Endpoint": "https://...", "Deployment": "text-embedding-3-large" }
    },
    "Routing": {
      "Primary":   "primary",
      "Secondary": "secondary",
      "Title":     "secondary"
    }
  }
}
```

**要点**：

- `AIProviders:Chat:{name}` 是**命名槽位**，不再硬编码 Primary / Secondary。
- `Provider` 字段做判别键，每种 Provider 各自消费自己的字段集（`Endpoint` vs `BaseUrl` vs `Deployment` vs `Model`）。
- `AIProviders:Routing` 把"逻辑角色"映射到"命名槽位"——业务代码依然只认 `AIProviderKeys.Primary`，但运维可以随时把 `primary` 指向任何一个物理槽位。
- `Title` 为"生成会话标题"这种低价值任务预留独立映射点，便于成本优化。

### 3.3 抽象接口（放在 `Inkwell.Abstractions`）

```csharp
// 1) 单个供应商端点配置
public sealed class AIEndpointOptions
{
    public string Provider { get; set; } = "";      // AzureOpenAI | OpenAI | OpenAICompatible | Anthropic | Ollama | Gemini | Bedrock
    public string? Endpoint { get; set; }            // Azure Endpoint / Ollama URL
    public string? BaseUrl { get; set; }             // OpenAI 兼容协议的基地址
    public string? Deployment { get; set; }          // Azure 专用
    public string? Model { get; set; }               // 非 Azure 的模型名
    public string? ApiKey { get; set; }              // 空值的语义由各 Provider 自行决定（Azure 走 DefaultAzureCredential、OpenAI 读 OPENAI_API_KEY、Ollama 不需要）
    public Dictionary<string, string> Extras { get; set; } = new();   // 供应商私有参数
}

// 2) 提供方插件接口
//    实现类作为 DI Singleton 注册，构造函数可注入 ILogger / IHttpClientFactory / TokenCredential 等依赖
public interface IAIChatProvider
{
    string Name { get; }                             // 与配置 Provider 字段匹配
    IChatClient CreateChatClient(AIEndpointOptions options);
}

public interface IAIEmbeddingProvider
{
    string Name { get; }
    IEmbeddingGenerator<string, Embedding<float>> CreateEmbeddingGenerator(AIEndpointOptions options);
}

// 3) 聚合选项（取代 AzureOpenAIOptions）
public sealed class AIProviderOptions
{
    public const string SectionName = "AIProviders";
    public Dictionary<string, AIEndpointOptions> Chat { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, AIEndpointOptions> Embedding { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public AIProviderRoutingOptions Routing { get; set; } = new();
}

public sealed class AIProviderRoutingOptions
{
    public string Primary { get; set; } = "primary";
    public string Secondary { get; set; } = "secondary";
    public string Title { get; set; } = "secondary";
    public string Embedding { get; set; } = "default";   // 非 Keyed 默认 IEmbeddingGenerator 指向此槽位
}

// 4) Keyed DI 逻辑键（取代当前的 ModelServiceKeys）
public static class AIProviderKeys
{
    public const string Primary = "ai:primary";
    public const string Secondary = "ai:secondary";
    public const string Title = "ai:title";
}
```

### 3.4 统一注册入口

把原来的 `UseAzureOpenAI` + `UseAzureOpenAIEmbedding` 合并为一个 `UseAIProviders`。关键实现要点：**绝不在 `ConfigureServices` 阶段调用 `BuildServiceProvider()`**（会被 ASP0000 警告，且会复制一份 Singleton 实例导致资源泄漏）。所有 Provider 的查表与实例化都放进 Keyed 工厂委托，由最终的 `IServiceProvider` 在首次 resolve 时驱动，这样 Provider 的构造函数依赖（`ILogger` / `IHttpClientFactory` / `TokenCredential`）也能正常注入。

```csharp
public static InkwellCoreBuilder UseAIProviders(this InkwellCoreBuilder b, IConfiguration cfg)
{
    b.Services.Configure<AIProviderOptions>(cfg.GetSection(AIProviderOptions.SectionName));
    AIProviderOptions opts = cfg.GetSection(AIProviderOptions.SectionName).Get<AIProviderOptions>() ?? new();

    // —— Chat：每个命名槽位注册一个 Keyed Singleton，Provider 在工厂里从 sp 解析 ——
    foreach ((string name, AIEndpointOptions endpoint) in opts.Chat)
    {
        AIEndpointOptions slot = endpoint;   // 避免闭包捕获循环变量
        b.Services.AddKeyedSingleton<IChatClient>(name, (sp, _) =>
        {
            IAIChatProvider provider = ResolveChatProvider(sp, slot.Provider, name);
            IChatClient raw = provider.CreateChatClient(slot);
            return DecorateChatClient(sp, raw);    // 横切能力：OpenTelemetry / Logging / 重试
        });
    }

    // Routing：把逻辑键映射到命名槽位
    b.Services.AddKeyedSingleton<IChatClient>(AIProviderKeys.Primary,
        (sp, _) => sp.GetRequiredKeyedService<IChatClient>(
            sp.GetRequiredService<IOptions<AIProviderOptions>>().Value.Routing.Primary));
    b.Services.AddKeyedSingleton<IChatClient>(AIProviderKeys.Secondary,
        (sp, _) => sp.GetRequiredKeyedService<IChatClient>(
            sp.GetRequiredService<IOptions<AIProviderOptions>>().Value.Routing.Secondary));
    b.Services.AddKeyedSingleton<IChatClient>(AIProviderKeys.Title,
        (sp, _) => sp.GetRequiredKeyedService<IChatClient>(
            sp.GetRequiredService<IOptions<AIProviderOptions>>().Value.Routing.Title));

    // —— Embedding：每槽位 Keyed 注册 + 默认槽位额外注册为非 Keyed 单例（向后兼容） ——
    foreach ((string name, AIEndpointOptions endpoint) in opts.Embedding)
    {
        AIEndpointOptions slot = endpoint;
        b.Services.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>(name, (sp, _) =>
            ResolveEmbeddingProvider(sp, slot.Provider, name).CreateEmbeddingGenerator(slot));
    }
    b.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        sp.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>(
            sp.GetRequiredService<IOptions<AIProviderOptions>>().Value.Routing.Embedding));

    return b;
}

private static IAIChatProvider ResolveChatProvider(IServiceProvider sp, string providerName, string slot)
    => sp.GetServices<IAIChatProvider>()
         .FirstOrDefault(p => string.Equals(p.Name, providerName, StringComparison.OrdinalIgnoreCase))
       ?? throw new InvalidOperationException($"Unknown AI provider '{providerName}' for slot '{slot}'.");
```

业务代码**完全不变**——依旧通过 Keyed DI 拿 `IChatClient` / `IEmbeddingGenerator`，底层已经可以是 Azure / OpenAI / Claude / Ollama 任意一种。现有依赖非 Keyed `IEmbeddingGenerator` 的服务（`AgentMemoryService` / `KnowledgeBaseService`）无需改动，自动指向 `Routing.Embedding` 指定的默认槽位。

### 3.5 Program.cs 最终形态

```csharp
builder.Services.AddInkwellCore()
    .AddAzureOpenAIProvider()      // 按需添加供应商
    .AddOpenAIProvider()
    .AddAnthropicProvider()
    .AddOllamaProvider()
    .UseAIProviders(builder.Configuration);
```

---

## 4. 分层与项目拆分

为了让"引入一个新供应商"不污染核心项目，按 `src/providers/` 既有模式把每个 Provider 拆成独立的薄项目：

```
src/providers/
  Inkwell.AI.AzureOpenAI/       # 依赖 Azure.AI.OpenAI
  Inkwell.AI.OpenAI/            # 依赖 OpenAI（含 Compatible 模式）
  Inkwell.AI.Anthropic/         # 依赖 Anthropic.SDK
  Inkwell.AI.Ollama/            # 依赖 OllamaSharp
```

每个项目暴露一个注册方法。Provider 实现类作为 DI Singleton，构造函数可注入任何需要的依赖：

```csharp
// Inkwell.AI.OpenAI
public static InkwellCoreBuilder AddOpenAIProvider(this InkwellCoreBuilder b)
{
    b.Services.AddSingleton<IAIChatProvider, OpenAIChatProvider>();
    b.Services.AddSingleton<IAIChatProvider, OpenAICompatibleChatProvider>();
    b.Services.AddSingleton<IAIEmbeddingProvider, OpenAIEmbeddingProvider>();
    return b;
}

internal sealed class OpenAICompatibleChatProvider(
    IHttpClientFactory httpFactory,
    ILogger<OpenAICompatibleChatProvider> logger) : IAIChatProvider
{
    public string Name => "OpenAICompatible";

    public IChatClient CreateChatClient(AIEndpointOptions options)
    {
        // 通过注入的 IHttpClientFactory 复用连接池，而不是 new HttpClient()
        OpenAIClient client = new(new ApiKeyCredential(options.ApiKey!),
            new OpenAIClientOptions { Endpoint = new Uri(options.BaseUrl!), Transport = ... });
        return client.GetChatClient(options.Model!).AsIChatClient();
    }
}
```

**好处**：核心项目只依赖 `Microsoft.Extensions.AI.Abstractions`，WebApi 按需引用 `Inkwell.AI.*` 项目，体积和耦合都可控。

### 4.1 横切能力扩展点

`UseAIProviders` 是 Inkwell 里**唯一创建 `IChatClient` 的地方**，天然适合统一叠加横切能力。实现上暴露一个 `DecorateChatClient(IServiceProvider, IChatClient)` 扩展点（见 §3.4 工厂委托中的调用）：

```csharp
private static IChatClient DecorateChatClient(IServiceProvider sp, IChatClient inner)
    => inner.AsBuilder()
        .UseOpenTelemetry(sp.GetService<ILoggerFactory>(), "Inkwell.AI")
        .UseLogging(sp.GetService<ILoggerFactory>())
        // 可继续 .UseFunctionInvocation() / .UseDistributedCache() / 重试 Policy
        .Build();
```

这样 **OpenTelemetry 追踪、日志、函数调用中间件、分布式缓存**等能力对所有供应商统一生效，不需要每个 Provider 实现重复一遍。

---

## 5. 设计收益

1. **成本优化**：给"生成会话标题"等低价值任务绑定便宜的模型（`Routing.Title` 独立映射），省钱立竿见影。
2. **灰度发布**：某个 Agent 可以显式指定用哪个命名槽位（例如 Writer 走 `creative` → Claude），而非所有 Agent 都吃 Primary。
3. **本地开发无云依赖**：开发环境把 `Routing.Primary` 指向 `local` 槽位（Ollama），完全离线可跑。
4. **可插拔的 Provider 注册**：`IAIChatProvider` 本身是 DI Singleton，测试时可直接注入 Mock 实现覆盖某种供应商，不用动业务代码里的 `IChatClient` 消费路径。
5. **统一横切能力**：OpenTelemetry / 日志 / 函数调用 / 缓存中间件集中在 `DecorateChatClient` 一处装配，所有供应商一视同仁。
6. **运维可视化**：`IAIChatProvider.Name` + 配置里的命名槽位天然就是"模型管理"页面的数据源。

---

## 6. 渐进迁移路径

不建议一次性大改，按下面三步走，每步都可独立上线：

| 步骤 | 动作 | 破坏性 |
|------|------|--------|
| **Step 1** | 给 `AzureOpenAIModelOptions`（Primary/Secondary 子对象）加可选 `Provider` 字段（默认 `AzureOpenAI`），并新增 `OpenAICompatibleChatProvider`，`UseAzureOpenAI` 按 `Provider` 分流 | 无破坏，DeepSeek / 国产 OpenAI 兼容模型立即可用 |
| **Step 2** | 引入 `AIProviderOptions` 新配置节（`AIProviders:*`），新增 `UseAIProviders`，旧 `UseAzureOpenAI` 保留作为向后兼容 | 旧配置继续生效 |
| **Step 3** | 拆分 `src/providers/Inkwell.AI.*`，`ModelServiceKeys` 更名为 `AIProviderKeys`，老方法标 `[Obsolete]`，下个版本移除。影响面：`Inkwell.Abstractions/ModelServiceKeys.cs`、`Inkwell.Core` 的 Agent/Workflow 注册扩展、`Inkwell.WebApi/Program.cs`、`SessionPersistenceMiddleware` 等所有引用 Keyed 键的地方 | 需要统一替换符号并更新 csproj 引用 |

**优先级建议**：

- Step 1 投入极小，立即打开国产模型生态——**高价值、低成本**。
- Step 2 是真正的质变（多命名槽位 + Routing）——**核心里程碑**。
- Step 3 属于代码整洁性，可推迟到供应商 ≥ 3 个时再做。

---

## 7. 关键决策点

在落地前需要确认以下几项：

1. **迁移节奏**：选 Step 1（小步快跑）还是直接 Step 2（一次到位新配置节）？
2. **配置节命名**：采用 `AIProviders`（已确定，避免与数据 `Models/` 混淆）；过渡期可同时读取旧的 `AzureOpenAI` 节。
3. **Provider 项目拆分时机**：现在就拆 `src/providers/Inkwell.AI.*`，还是先集中在 WebApi，等供应商 ≥ 3 个再拆？
4. **首批目标供应商**：除 AzureOpenAI 外最先支持哪些？建议 `OpenAICompatible`（覆盖 DeepSeek / Qwen / Moonshot 等国产一堆）+ `Ollama`（本地）。
5. **Embedding 是否多供应商**：建议暂时只支持 Azure OpenAI，因为 Anthropic / Ollama 的 embedding 质量与覆盖度暂时不如 OpenAI 系。
6. **是否支持配置热重载**：默认不支持（`IChatClient` 为 Singleton，改配置需重启）。如需"换 key / 切 deployment 不重启"，需基于 `IOptionsMonitor<AIProviderOptions>` 引入 `IChatClientFactory` 并把 `IChatClient` 改为 Scoped，建议二期再做。

---

## 8. 与其他模块的关系

- **Agent 中间件**：`ContentGuardrailMiddleware` / `FunctionCallAuditMiddleware` 通过 `ChatClientAgent.Use()` 套在 `IChatClient` 上，不关心底层供应商，无改动。
- **会话持久化**：`SessionPersistenceMiddleware` 吃的是 `IChatClient? titleGenerator` 参数，支持按 `Routing.Title` 指向独立槽位。
- **向量存储**：当前 `AgentMemoryService` / `KnowledgeBaseService` 依赖 `IEmbeddingGenerator`，`UseAIProviders` 会把 `Embedding.default` 槽位注册为默认单例，保持向后兼容。
- **WorkflowChatClient**：仅把 Workflow 包装成 `IChatClient`，与 AI 供应商完全正交。

---

## 9. 总结

当前 Inkwell 的核心抽象足够干净——业务代码只认 `IChatClient` / `IEmbeddingGenerator`，`ModelServiceKeys` Keyed DI 也已就位。要升级成"多 AI Provider"只需补一层配置驱动的注册适配：

1. 把 `AzureOpenAIOptions` 升级为 `AIProviderOptions`（命名槽位 + Routing 映射，聚合根）+ `AIEndpointOptions`（单条端点）。
2. 抽 `IAIChatProvider` / `IAIEmbeddingProvider` 插件接口，`ModelServiceKeys` 更名为 `AIProviderKeys`。
3. 按 `src/providers/Inkwell.AI.*` 拆分每种供应商的独立项目。

整套方案向后兼容（老配置可保留一段时间），可按 Step 1 → Step 2 → Step 3 渐进推进，既能立即解锁国产模型与本地 Ollama 开发体验，又不牺牲核心项目的简洁性。

> 命名小结：`AIProvider` = AI 模型供应商层；`Models/` = 业务数据模型层。两者语义独立，不再混淆。
