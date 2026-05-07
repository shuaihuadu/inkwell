---
id: open-questions-arch-custom-agent
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers:
  - name: self-review
    decision: approved
    date: 2026-05-07
created: 2026-05-07
updated: 2026-05-07
upstream:
  - architecture-custom-agent
downstream: []
---

# 自定义 Agent 功能 · H2 待澄清清单

> 本表登记 H2 反问轮中**未在会话中得到答复**或**得到答复但需后续验证**的项；按 [agents/_shared/io-contracts.md 第 5 节](../../.he/) 规范每条都标 `blocking` / `non-blocking`。`blocking` 项关闭前 H2 不视作 approved。

## 字段说明

- **影响范围**：哪些 REQ / RISK / 模块会被这条 OQ 答案改写
- **建议默认值**：H2-ArchitectAdvisor 推荐项；项目负责人可覆盖但需明确
- **卡点等级**：`blocking` 阻塞 H2 评审通过；`non-blocking` 不阻塞但需在 H3 / H5 之前关闭
- **关闭时机**：何时必须有答案

---

## OQ-A-001 平台登录子系统的 IdP 与协议

- **问题**：生产期 OIDC handler 接的具体 IdP 是哪个？协议（OIDC / SAML / OAuth2）是哪种？token 中的用户主键 claim 名是什么？
- **影响范围**：[RISK-004](./risk-analysis.md)；[ADR-004](./adr/ADR-004-platform-login-dev-mock.md)；[architecture.md 第 7 节](./architecture.md#7-鉴权与权限模型)；[GAP-003](../01-requirements/repo-impact-map.md#3-缺失发现43)；R7
- **建议默认值**：H3 详细设计前给出至少候选 IdP 短列表（如 Azure Entra ID / Auth0 / Keycloak），H5 编码任务卡里固定一个
- **卡点等级**：non-blocking（H2 + H3 不阻塞，dev mock 路径可继续推进；H5 之前必须关闭，否则生产无法部署）
- **关闭时机**：H5 编码任务启动前

## OQ-A-002 联网搜索（T-1）服务商

- **问题**：T-1 真实实现服务商是哪家？SLA / 单查询成本 / 数据驻留如何？
- **影响范围**：[RISK-005](./risk-analysis.md)；[ADR-005](./adr/ADR-005-web-search-mock-first.md)；REQ-011 / AC-011-3/4/5；[R8](../01-requirements/requirements.md)；[GAP-005](../01-requirements/repo-impact-map.md#3-缺失发现43)
- **建议默认值**：候选 = Bing Web Search API / Tavily / Brave Search API / Google CSE；H5 编码任务卡里固定一个
- **卡点等级**：non-blocking（本期 Mock 实现已可覆盖 [E6 首次启用须知](../01-requirements/requirements.md) 全部交互测试；H6 release notes 之前必须关闭，否则不能切真实用户）
- **关闭时机**：H6 阶段切真实搜索前

## OQ-A-003 MAF + AG-UI 集成的具体 NuGet 包（已关闭 2026-05-07）

- **问题**：[docs.ag-ui.com](https://docs.ag-ui.com/) 列出 "Microsoft Agent Framework · 1st Party · Supported"，但具体集成是哪个 NuGet 包？包名 / 命名空间 / 端点入口形状如何？
- **答案**（H2-ArchitectAdvisor 2026-05-07 在用户授权下查证 [microsoft/agent-framework `dotnet/src/`](https://github.com/microsoft/agent-framework/tree/main/dotnet/src)）：
  - **协议层 / 客户端**：[`Microsoft.Agents.AI.AGUI`](https://github.com/microsoft/agent-framework/tree/main/dotnet/src/Microsoft.Agents.AI.AGUI)（含 `AGUIChatClient` / `AGUIHttpService`）—— 用于把远程 AGUI agent 包装成 `AIAgent` 在客户端 / 调用方使用
  - **服务端 hosting**：[`Microsoft.Agents.AI.Hosting.AGUI.AspNetCore`](https://github.com/microsoft/agent-framework/tree/main/dotnet/src/Microsoft.Agents.AI.Hosting.AGUI.AspNetCore)（含 `AGUIEndpointRouteBuilderExtensions` / `ServiceCollectionExtensions` / `AGUIServerSentEventsResult` / `AGUIChatResponseUpdateStreamExtensions` / `AGUIJsonSerializerOptions`）—— 标准 ASP.NET Core "DI 注册 + Endpoint 映射" 模式，默认 transport = SSE（与 [ADR-002](./adr/ADR-002-agui-as-chat-protocol.md) 设计前提一致）
  - **维护状态**：服务端 hosting 包 3 周前刚加 session storage 支持，活跃；协议层 2 个月前 bugfix
  - **次级风险**：[Microsoft Learn Integrations 页](https://learn.microsoft.com/en-us/agent-framework/integrations/) 标 **"AG UI · Preview"**（不是 Released）—— 落入 [RISK-001](./risk-analysis.md) 跟踪范围，但不构成 H2 阻塞
- **影响范围**：[RISK-001](./risk-analysis.md) / [RISK-009](./risk-analysis.md)（缓解已更新为引用具体包）；[ADR-002](./adr/ADR-002-agui-as-chat-protocol.md)（已固化包名）；[architecture.md 第 3.4 节](./architecture.md#34-ag-ui-端点)（已固化包名）
- **状态**：✅ resolved 2026-05-07
- **关闭时机**：~~H2 选型评审前~~ 已在 H2 选型阶段关闭

## OQ-A-004 OQ-030 试运行下半屏最终形态（H1 转入）

- **问题**：本期同页 React 组件 vs vNext 切 iframe 的最终形态决策（A / B / C 三选一）
- **影响范围**：REQ-004；[OQ-030](../01-requirements/open-questions.md)；[ui-spec.md 第 2.2 节](../01-requirements/ui-spec.md)；H3 P2 ↔ 试运行子页之间的契约
- **建议默认值**：**C 本期 A / vNext 视平台聊天页面成熟度切 B**；与 [评审记录第 5 节 C-1](../07-reviews/2026-05-07-h1-prototype-custom-agent.md) 暂定立场一致
- **卡点等级**：non-blocking（本期 A 同页组件已落锤；C 选项允许 vNext 切换）
- **关闭时机**：H2 选型评审前由项目负责人确认 C；写回 [task-board.md TASK-2026-05-07-001](../06-tasks/task-board.md)

## OQ-A-005 K8s 集群基线

- **问题**：生产 K8s 版本基线？Ingress 控制器（NGINX / Traefik / 其他）？Redis 部署形态（Sentinel / Cluster / Azure Cache for Redis）？K8s 集群是 AKS / EKS / 自建 K3s？
- **影响范围**：[RISK-006](./risk-analysis.md)；[ADR-006](./adr/ADR-006-deployment-split.md)；[architecture.md 第 9 节](./architecture.md#9-部署方式)；H3 Helm chart 编写
- **建议默认值**：Kubernetes ≥ 1.28；NGINX Ingress Controller；Azure Cache for Redis Sentinel；AKS（与 Azure OpenAI / Blob 同生态）
- **卡点等级**：non-blocking（H3 Helm chart 阶段必须关闭，否则模板无法定型）
- **关闭时机**：H3 详细设计开始前

## OQ-A-006 Azure OpenAI region 与数据驻留

- **问题**：Azure OpenAI gpt-4.1 部署 region 是哪个？是否与 Azure Blob 同 region（[NFR-004](../01-requirements/requirements.md) 数据驻留）？
- **影响范围**：[architecture.md 第 13 节](./architecture.md#13-安全设计)；NFR-004；GAP-004
- **建议默认值**：默认 East US 2 或 Sweden Central（gpt-4.1 公开可用 region）；与 Azure Blob 同 region
- **卡点等级**：non-blocking（H3 详细设计可继续；H6 release notes 之前必须关闭，写入运维须知）
- **关闭时机**：H6 release notes 之前

## OQ-A-007 [评估] 是否引入 CopilotKit React UI

- **问题**：前端是否引入 CopilotKit 的 React UI 组件库（[copilotkit.ai](https://copilotkit.ai/)）作为 AG-UI 客户端的 UI 包装？
- **影响范围**：[architecture.md 第 2.1 节](./architecture.md#21-技术栈)；[RISK-001](./risk-analysis.md)
- **建议默认值**：**不引入**。理由：AntD 5 已提供完整 UI 套件，引入 CopilotKit UI 会与 AntD 主题冲突；前端只用 `@ag-ui/client`（HttpAgent + SSE 协议层），UI 自建于 AntD 之上
- **卡点等级**：non-blocking
- **关闭时机**：H3 详细设计前由前端代表确认

## OQ-A-008 Outbox 模式具体落地

- **问题**：架构第 6 节"OutboxMessages 表 + DB → Queue → 处理器"模式的具体细节（消息 schema、retry 策略、dead-letter 处理、消费者幂等性）
- **影响范围**：[architecture.md 第 4.3 节](./architecture.md#43-主要表h3-细化) / [第 6 节](./architecture.md#6-消息机制队列-multi-provider)；H3 详细设计
- **建议默认值**：H2 不替 H3 决定具体形态；H3 阶段细化
- **卡点等级**：non-blocking
- **关闭时机**：H3 详细设计

---

## 卡点统计

| 等级 | 数量 | 编号 |
| --- | --- | --- |
| **blocking** | 0 | — |
| non-blocking | 7 | OQ-A-001 / 002 / 004 / 005 / 006 / 007 / 008 |
| resolved | 1 | OQ-A-003（2026-05-07） |

H2 评审前不再有 blocking 项。non-blocking 项分别在 H3（OQ-A-005）/ H5（OQ-A-001 / 008 / 007）/ H6（OQ-A-002 / 006）/ H2 选型评审会（OQ-A-004）关闭。
