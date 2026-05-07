---
id: risk-analysis-custom-agent
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
  - tech-selection-custom-agent
downstream: []
---

# 自定义 Agent 功能 · H2 风险分析

> 每条风险按 stages.md / Agent 规范第 4.3 节字段：`类别 / 触发条件 / 影响范围 / 缓解方案 / 残余风险`。

| 编号 | 类别 | 触发条件 | 影响范围 | 缓解方案（可执行） | 残余风险 |
| --- | --- | --- | --- | --- | --- |
| RISK-001 | 兼容性 / 外部依赖 | MAF AG-UI hosting 包 `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` 在 [Microsoft Learn Integrations](https://learn.microsoft.com/en-us/agent-framework/integrations/) 标 "AG UI · Preview"（不是 Released），API 形状 / 命名空间在 1.x→2.x 期间可能 breaking | REQ-004 试运行 / 对话调试；后端 `/api/agui/run` 端点；架构第 3.4 / 3.5 节；[ADR-002](./adr/ADR-002-agui-as-chat-protocol.md) | NuGet 包版本锁到具体 minor（与主包 `Microsoft.Agents.AI` 1.4.x 同步）；订阅 `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` 的 [GitHub Releases](https://github.com/microsoft/agent-framework/releases)；H6 升版前跑全集成测；如未来包被废弃或独立 [AG-UI .NET SDK GA](https://docs.ag-ui.com/)，开 ADR 替换；fallback 仍保留可选项：自实现 SSE handler 输出 16 种 EventType 子集（≤ 5 人天，由 hosting 包源码做实现参考） | Preview → GA 期间可能数次 breaking change，每次需 H5 / H6 联合升级任务跟进 |
| RISK-002 | 兼容性 / 外部依赖 | `Microsoft.Agents.AI.Foundry` 仍 prerelease；MAF 仍处早期演进期 breaking change 未保证 | 后端 ChatOrchestrator；REQ-004 / REQ-011 试运行 + Tool 调用 | 仅依赖 GA 主包 `Microsoft.Agents.AI 1.4.x`；不引入 prerelease 子包（如 Foundry / Workflows.Declarative.Mcp）；NuGet 锁版本到 1.4.x；订阅 GitHub Releases；H6 阶段每次升版前跑全集成测 | MAF 1.x→2.x major 升级时仍可能 breaking；H5 升级任务由 H3 单独立 task |
| RISK-003 | 团队能力 / 交付周期 | M1 多 provider 抽象（DB / 队列 / 缓存 / 对象存储）需要每 provider 一套实现 + 集成测覆盖每种 provider | H5 编码工作量；H4 测试矩阵 | **抽象接口最小化**（每个能力 ≤ 5 个公共方法）；CI 矩阵按 PR 标签触发（PR 标签 `provider:postgres` 才跑 PG 集成测）；用 Testcontainers 避免每个开发者本地起 4 套服务；明确"InMemory 仅测试 / Redis 仅多副本生产"在 [architecture.md 第 4-6 节](./architecture.md) | M1 抽象漏抽（如某个原生 SQL 性能特性被绕过）；首次切 provider 必出问题 → H6 阶段强制压测每 provider |
| RISK-004 | 安全 / 合规 | dev mock 登录如被错误带到生产构建，等于无鉴权 | 全部 REQ；任何用户都能访问任何 Agent | 1. dev mock handler 用 `#if DEBUG` 条件编译排除生产二进制；2. `Authentication:Mode=DevMock` 检测在生产构建启动时主动 throw `InvalidOperationException`；3. CI 在 release build 后跑 smoke test 验证 `Authentication:Mode=DevMock` 启动失败；4. K8s Helm chart 默认 `Authentication.Mode=Oidc`，禁止覆盖 | OIDC IdP 选定（GAP-003）前生产无法上线——已登记 OQ-A-001 |
| RISK-005 | 外部依赖 / 合规 | 联网搜索 Mock → 真实切换时（GAP-005 / OQ-A-002 关闭后），上下游契约可能不兼容（如返回字段、超时、错误语义） | REQ-011 / AC-011-3/4/5；试运行 Tool 调用 | `IWebSearchTool` 接口必须使用平台中立的契约（输入 `query`、输出 `Results: List<{Title, Snippet, Url}>`）；Mock 实现遵守同契约；切换时只换实现不改接口；切换前由 H4 阶段补充 [E6 首次启用须知](../01-requirements/requirements.md) 真实供应商场景的契约测试 | 服务商 SLA 与 [R8](../01-requirements/requirements.md) 缓解需 H2 重启 OQ-A-002 后再核 |
| RISK-006 | 性能 / 可用性 | K8s Ingress 默认配置会缓冲 SSE 响应，导致 AG-UI 事件流在客户端延迟出现或卡住 | REQ-004 试运行用户体验；NFR-002 性能目标 | Helm chart 模板默认设置：`nginx.ingress.kubernetes.io/proxy-buffering: "off"`、`nginx.ingress.kubernetes.io/proxy-read-timeout: "3600"`、`nginx.ingress.kubernetes.io/proxy-send-timeout: "3600"`；如用其他 Ingress（Traefik / HAProxy）H3 阶段补对应配置；H6 阶段强制 SSE 端到端延迟回归 | 客户使用其他云厂商 Ingress 时仍需手动调优 — 在 release notes "运维须知"提示 |
| RISK-007 | 成本 / 兼容性 | Azure Blob 单一 provider，无 fallback；客户私有部署场景如无 Azure 公网访问能力，对象存储无法工作（Local 不支持多副本） | 私有部署的 REQ-002 头像功能 | 1. `IObjectStorage` 接口已抽象，vNext 加 S3 / OSS provider 仅需新实现，不影响 H5 落地；2. 私有部署在 release notes "运维须知"明确写"暂仅支持 Azure Blob 或单副本 Local 模式"；3. 私有部署如急需多副本，临时方案 = NFS 挂载 + Local provider（H3 留 NFS 挂载文档） | 私有部署多副本仍需 NFS 容错；vNext S3 provider 排期未定 |
| RISK-008 | 可用性 / 数据一致性 | 多副本 K8s 部署下，开发者错把 `Persistence.Provider=InMemory` 配到生产，等于不同副本看到不同数据 | 全部 REQ；用户行为像随机 | Helm chart 默认值校验：`values.yaml` 中 `persistence.provider` ∈ `{SqlServer, PostgreSQL}`，否则 chart render 失败；后端启动时如检测 `InMemory + 副本数 > 1` 主动 throw；CI release build smoke test 同 RISK-004 | 单副本误用 InMemory 仍可能（用户重启数据丢失）—— 文档警示 + 默认 PG |
| ~~RISK-009~~ | — | **2026-05-07 已并入 RISK-001**：[OQ-A-003 关闭](./open-questions-arch.md) 后包名已固化为 `Microsoft.Agents.AI.Hosting.AGUI.AspNetCore` + `Microsoft.Agents.AI.AGUI`；H5 编码任务卡可正常给"允许修改文件"清单 | — | 已转化为 RISK-001 Preview 状态跟踪 | — |

## 风险概览（按类别）

| 类别 | 编号 | 数 |
| --- | --- | --- |
| 兼容性 / 外部依赖 | RISK-001、RISK-002、RISK-007 | 3 |
| 安全 / 合规 | RISK-004、RISK-005 | 2 |
| 性能 / 可用性 | RISK-006、RISK-008 | 2 |
| 团队能力 / 交付周期 | RISK-003 | 1 |
| 已并入 / 关闭 | ~~RISK-009~~（并入 RISK-001） | 1 |

## 与 H1 风险的衔接

H1 已识别的 R1~R8（[requirements.md 第 12.1 节](../01-requirements/requirements.md#121-风险)）由 H2 视角延展：

| H1 风险 | H2 视角延展 |
| --- | --- |
| R1 范围扩大 | 已被 OQ-021 = R-Accept 吸收，H2 不重复登记 |
| R2 非技术用户 UX | UI 层由 [ui-spec.md](../01-requirements/ui-spec.md) 锁定；H2 不引入额外 UX 风险 |
| R3 不做内容审核 | 用户已知接受；H2 不变 |
| R4 分享快照 | vNext 议题；H2 不涉及 |
| R5 知识库地域驻留 | vNext / pending；H2 不涉及 |
| R6 导入 schema 兼容 | vNext；H2 不涉及 |
| R7 登录硬依赖 | **延展为 RISK-004**（dev mock 错误带到生产） |
| R8 第三方搜索合规 | **延展为 RISK-005**（Mock → 真实切换契约风险） |

## 残余风险接受

按 H2-ArchitectAdvisor 规范第 7 节，所有标"残余风险"列的内容均需要项目负责人在 H2 评审会签字接受；未签字前 H2 不视作 approved。

| 残余风险 | 接受签字 |
| --- | --- |
| RISK-001 MAF AG-UI hosting 包 Preview → GA 期间可能 breaking | 待签 |
| RISK-002 MAF 1.x→2.x major 升级风险 | 待签 |
| RISK-003 M1 漏抽 / 首次切 provider 出问题 | 待签 |
| RISK-004 OIDC 选定前生产不能上线 | 待签 |
| RISK-005 真实搜索服务商 SLA 待评估 | 待签 |
| RISK-006 客户用其他 Ingress 需手动调优 | 待签 |
| RISK-007 私有部署多副本需 NFS / vNext S3 | 待签 |
| RISK-008 单副本 InMemory 误用 | 待签 |
| ~~RISK-009~~ | 已并入 RISK-001 |
