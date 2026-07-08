---
id: HD-020
title: Inkwell.Core.Skills 详细设计 — Skill 目录元数据 / 绑定解析（不含运行期激活与执行）
stage: H3
status: draft
reviewers: []
upstream:
  - REQ-008
  - ADR-010
  - ADR-017
  - ADR-023
  - HD-001
  - HD-002
  - HD-015
---

<!-- markdownlint-disable MD060 -->
<!-- 中文 + 英文混排长表格在 markdownlint 列宽计算下字面对齐 ≠ 视觉对齐（详 /memories/markdown-lint.md，与 HD-004 / HD-005 / HD-006 / HD-007 / HD-014 / HD-015 / HD-016 / HD-017 / HD-018 / HD-019 同处理方式），表格仍按 docs-style §3 视觉对齐维护，机械 MD060 不予执行。 -->

> **本 HD 是 H3 第七张业务命名空间（`Inkwell.Core.*`）详细设计**，在起草顺序上紧接 [HD-019 `Inkwell.Core.Models`](HD-019-Inkwell.Core.Models.md) 之后（本次提交时 HD-019 frontmatter 仍为 `status: draft`，本 HD 不对该状态做任何断言或修改）。
>
> **治理声明**：本文件全文不包含任何"已用 `vscode_askQuestions` 向 Owner 真实确认"的表述——本次起草会话未发起任何 `vscode_askQuestions` 交互。全部标注"作者判断"的条目均为作者基于现有证据链的判断；存在真实产品 / 技术含义分歧、无法从现有文档判定的问题，原样列入 [§8](#8-需要-owner-确认的问题)，不代答、不假装已确认。
>
> **范围核实结论（非臆造，逐条附证据）**：
>
> - **REQ-008（Agent Skills）在范围内，但仅"Skill 目录元数据管理（上传 / 解析 / 查询）+ Agent 侧绑定内容解析"两个子能力**——[requirements.md §4.4](../../01-requirements/requirements.md) 字面"从 Skill 库中选择若干 Skill 挂到 Agent；Skill 采用 [agentskills.io](https://agentskills.io/home) 的 `SKILL.md` 文件夹格式（仅 `SKILL.md` + `references/` + `assets/`，v1 禁用 `scripts/`）"；[ui-spec.md §4.3.5](../../01-requirements/ui-spec.md)"列表展示已上传到 Skill 库的 Skill；每项可勾选挂到 Agent"+"顶部'上传 Skill'...后端二次校验：上传成功 = 文件结构合规且 SKILL.md 可解析"——均描述"上传 + 只读浏览 + 绑定"，**没有任何一条**描述编辑 / 删除已入库 Skill 的界面或流程。本 HD 据此把范围收窄为：**只读目录查询（`ListAvailableSkillsAsync`/`GetSkillAsync`）+ 上传注册（`UploadSkillAsync`，唯一写路径）+ 绑定内容解析（`ISkillContentResolver.ResolveAsync`，供 [HD-015 `AgentInvocationService`](HD-015-Inkwell.Core.Agents.md#34-agentsiagentinvocationservicecs) 消费）**；**不**提供 `UpdateSkill`/`DeleteSkill`（同 [HD-016 `Inkwell.Core.Tools` §1.3 Q2](HD-016-Inkwell.Core.Tools.md#13-关键决策摘要)"不发明未被要求的能力"先例，[HD-014 §1.3 Q7](HD-014-Inkwell.Core.Auth.md#13-关键决策摘要) 同源）。
> - **`scripts/` 拒收的落地方式与 [ADR-010](../../03-architecture/adr/ADR-010-skill-loading-static-only-v1.md) 决策正文的措辞存在一处需要调和的表述差异，本 HD 采信证据更具体的 AC / ui-spec / user-flow，逐条列出理由**：ADR-010"决策"小节原文"SCRIPT block 处理：解析时识别但不执行；UI Skill 详情页显示'该 Skill 含可执行脚本，v1 暂不支持' banner"，字面暗示"允许上传但屏蔽执行"；但 [acceptance-criteria.md AC-029](../../01-requirements/acceptance-criteria.md)"上传含 `scripts/` 的 Skill 文件夹 / zip，前端**前置**拒收...**文件不进入 Skill 库**"+ [ui-spec.md §4.3.5](../../01-requirements/ui-spec.md)"发现 `scripts/` 目录立即弹错...**禁止提交**"+ [user-flow.md UF-006](../../01-requirements/user-flow.md)"客户端**前置**扫描结构：发现 `scripts/` 目录立即弹错...**阻断上传**"三处文档三次重复且完全一致地要求"整体拒收、不入库"，而非"入库但屏蔽执行"。本 HD 采信 AC/ui-spec/user-flow 的"整体拒收"结论（三处独立文档一致优于 ADR-010 单处探索性表述），**不**实现"识别 SCRIPT block 并在内容中剥离"的解析逻辑，改为**后端二次校验时若检测到 `scripts/` 前缀的包内条目，整体拒绝该次上传**（[§1.3 Q1](#13-关键决策摘要)）。该表述差异已在 [§8 Q&A-A](#8-需要-owner-确认的问题) 列出供 Owner 复核是否需要对 ADR-010 发起 errata（本 HD 不擅自修改 ADR-010 正文）。
> - **Discovery / Activation / Execution 三阶段与 `Inkwell.Core.AgentRuntime` 的边界**——参考 [`microsoft/agent-framework` 的 `AgentSkillsProvider`](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI/Skills/AgentSkillsProvider.cs) 真实实现（该类型是 `AIContextProvider`，采用"渐进式披露"：advertise 阶段仅把 name+description 注入系统提示；模型需要时通过 `load_skill(name)` / `read_skill_resource(name, resource)` 两个 `AIFunction` 工具按需拉取正文与资源），确认"是否命中当前对话上下文并决定注入"这一**运行时决策**天然是 MAF 函数调用协议的一部分，与 [HD-016 `Inkwell.Core.Tools` §1.4](HD-016-Inkwell.Core.Tools.md#14-与-inkwellcoreagentruntime-的边界声明工具定义-vs-工具执行)"工具定义 vs 工具执行"的边界判定标准（"是否触碰 MAF 类型/协议"）完全同构。本 HD（**Discovery** 的"目录管理"部分 + **绑定内容解析**）严格保持 MAF-free；**Activation（运行时命中判定 + 渐进式披露工具注册）与 Execution（v1 恒不支持）均不在本 HD**，归未起草的 `Inkwell.Core.AgentRuntime`（[AGENTS.md §3.2](../../../AGENTS.md) 唯一允许 `using Microsoft.Agents.AI.*` 的命名空间）。
> - **AC-031"trace 中能看到 Discovery / Activation / Execution 三阶段命中状态"不在本 HD 范围**——trace 记录 / 可视化归未起草的 `Inkwell.Core.Traces`；本 HD 仅保证 Discovery（目录查询 + 绑定解析）阶段产出可被上游记录的结果（[§3.4](#34-skillsiskillcontentresolvercs) `SkillResolutionResult`），不自行写 trace。
> - **REQ-005/REQ-006/REQ-007/REQ-015/REQ-017 均不在本 HD 范围**——`SkillDefinition` 不持有 Agent 归属 / 版本 / 共享字段，同 [HD-016 §1.2](HD-016-Inkwell.Core.Tools.md#12-范围) 先例。
>
> **依赖规则遵循**（[AGENTS.md §3.2](../../../AGENTS.md)）：`Inkwell.Core.Skills` 只依赖 `Inkwell.Abstractions` + BCL；**不** `using` 任何 Provider 包，**不** `using Microsoft.Agents.AI.*`；持久化经 `IPersistenceProvider.GetRepository<ISkillRepository>()`（[HD-002 §13.3 Q1=A2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）；本 HD 不写审计（[NFR-004](../../01-requirements/requirements.md) 审计事件清单未列"Skill 目录上传/查询"，仅列"Skill 与工具的挂载变更"——该事件已由已 reviewed 的 [HD-015 `AgentService.UpdateAgentAsync`](HD-015-Inkwell.Core.Agents.md#39-inkwellcoreagentsagentservicecs)`ActionType="agent_skill_bindings_changed"` 覆盖，同 [HD-016 §1.3 Q1](HD-016-Inkwell.Core.Tools.md#13-关键决策摘要) 判断依据）。
>
> **上传的实际文件存储（`references/`/`assets/` 二进制内容）不在本 HD**——本 HD 假设调用方（未起草的 `Inkwell.WebApi`）已把这些文件经 [HD-003 `IFileStorageProvider.UploadAsync`](../Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) 上传完毕并取得 `Uri`，本 HD 的 `SkillUploadRequest`（[§3.5](#35-skillsskilluploadrequestcs)）只接收已上传完成的 `Uri` 列表，不处理 multipart / 二进制流（同 [HD-015 `AgentDefinition.AvatarUri`](HD-015-Inkwell.Core.Agents.md#31-persistenceagentsagentdefinitioncs) 引用模式，避免本 HD 越权处理 HTTP 层职责）。

## 1. 模块概述

### 1.1 职责

`Inkwell.Core.Skills` 承担：

- Skill 目录的上传注册（[REQ-008](../../01-requirements/requirements.md) / [AC-030](../../01-requirements/acceptance-criteria.md)）：解析 `SKILL.md` frontmatter（`name`/`description`）+ 正文，校验包结构合规（拒收 `scripts/`），持久化为 `SkillDefinition`
- Skill 目录的只读查询（[AC-025](../../01-requirements/acceptance-criteria.md) 类比场景，供 UI Skill 库列表展示）
- 把 [HD-015 `AgentDefinition.SkillBindings`](HD-015-Inkwell.Core.Agents.md#31-persistenceagentsagentdefinitioncs)（`IReadOnlyList<AgentSkillBinding>`）解析为可注入对话上下文的 `SkillContent` 列表（[§3.4](#34-skillsiskillcontentresolvercs)），供未起草的 `Inkwell.Core.AgentRuntime` 消费；解析时对"绑定引用的 Skill 已不存在"采用**尽力而为、不中断**策略（[§1.3 Q3](#13-关键决策摘要)，直接对应 [EX-008](../../01-requirements/requirements.md)"对话不阻断"要求）

`ISkillCatalogService` / `ISkillContentResolver` 是本模块**业务对外接口**，落在 `Inkwell.Abstractions/Skills/`（[HD-001 §5.1](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#51-命名) `I<Module>Service` 命名约定；`ISkillContentResolver` 定位同 [HD-016 `IToolBindingResolver`](HD-016-Inkwell.Core.Tools.md#34-toolsitoolbindingresolvercs)"翻译 + 衔接"型业务端口，作者判断，非 Owner 拍板）。

### 1.2 范围

**在内**：

| 类别            | 文件（`Inkwell.Abstractions/`）                                    |
| --------------- | -------------------------------------------------------------------- |
| 业务 Model      | `Persistence/Skills/SkillDefinition.cs`                              |
| 具名 Repository | `Persistence/Skills/ISkillRepository.cs`                             |
| 业务对外接口    | `Skills/ISkillCatalogService.cs` / `Skills/ISkillContentResolver.cs` |
| 业务 DTO        | `Skills/SkillUploadRequest.cs`（含 `SkillPackageEntry`） / `Skills/SkillContent.cs` |
| Options         | `Skills/SkillOptions.cs` + `Skills/SkillOptionsValidator.cs`         |

| 类别    | 文件（`Inkwell.Core/Skills/`）                                                                          |
| ------- | ---------------------------------------------------------------------------------------------------------- |
| 实现    | `SkillCatalogService.cs`（`ISkillCatalogService` 唯一实现）/ `SkillContentResolver.cs`（`ISkillContentResolver` 唯一实现） |
| DI 装配 | `SkillsBuilderExtensions.cs`（`UseDefaultSkillService()`，风格对齐 [HD-016 `ToolsBuilderExtensions.cs`](HD-016-Inkwell.Core.Tools.md)） |

**不在内**（明确排除，逐条附去向）：

- Skill 内容的运行时激活判定（是否命中当前对话上下文）+ 渐进式披露工具（`load_skill`/`read_skill_resource` 等 `AIFunction`）的注册与调用——归未起草的 `Inkwell.Core.AgentRuntime`（详见顶部范围核实结论第三条 + [§1.4](#14-与-inkwellcoreagentruntime-的边界声明skill-目录--解析-vs-skill-激活)）
- Skill 脚本的执行——**v1 全局不支持**（[ADR-010](../../03-architecture/adr/ADR-010-skill-loading-static-only-v1.md) + [AGENTS.md §3.3](../../../AGENTS.md)"不实现 Skill Execution，不预留 `ISkillExecutor` 接口"），本 HD 通过"整体拒收含 `scripts/` 的上传"从源头消除该场景，不声明任何执行相关接口
- `UpdateSkill`/`DeleteSkill`——v1 无编辑 / 删除 Skill 的界面或流程（[§1.2 顶部范围核实](#12-范围)已附证据），`ISkillRepository` 不声明这两个方法
- Agent 侧 `AgentUpsertRequest.SkillBindings` 的接线校验触点（"`Inkwell.WebApi` 在调用 `IAgentService.CreateAgentAsync`/`UpdateAgentAsync` 前循环调用本 HD `ISkillCatalogService.GetSkillAsync` 校验存在性"这一编排步骤）——留给未起草的 `Inkwell.WebApi` HD，同 [HD-016 §1.2](HD-016-Inkwell.Core.Tools.md#12-范围)"不在内"第四条先例；已 reviewed 的 [HD-015 `AgentService`](HD-015-Inkwell.Core.Agents.md#39-inkwellcoreagentsagentservicecs) 本身**不**校验 `SkillBinding`，本 HD 不改写该结论
- Skill 上传的 multipart / zip 解包、`references/`/`assets/` 二进制文件的实际存储——归未起草的 `Inkwell.WebApi`（经 [HD-003 `IFileStorageProvider`](../Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md)），详见顶部 callout
- 审计日志的写入——同顶部范围核实结论，本 HD 不注入 `IAuditLogger`

### 1.3 关键决策摘要

> 以下全部为**作者判断，非 Owner 拍板**；有真实产品含义分歧的条目已单独列入 [§8](#8-需要-owner-确认的问题)，不在此重复。

| #   | 决策                                                                                                                            | 理由                                                                                                                                                                                                                                                                                                                                                                                                                    |
| --- | ------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Q1  | `UploadSkillAsync` 遇到 `PackageEntries` 中任一条目 `RelativePath` 以 `scripts/`（大小写不敏感）为前缀时，**整体拒绝本次上传**，不做"剥离 SCRIPT 后仍入库"处理 | [AC-029](../../01-requirements/acceptance-criteria.md) + [ui-spec.md §4.3.5](../../01-requirements/ui-spec.md) + [user-flow.md UF-006](../../01-requirements/user-flow.md) 三处文档一致要求"阻断上传 / 文件不进入 Skill 库"，与 [ADR-010](../../03-architecture/adr/ADR-010-skill-loading-static-only-v1.md) 决策正文"识别但不执行 + banner"表述不一致；本 HD 采信三处独立、具体、相互印证的验收 / 交互文档，详见顶部 callout + [§8 Q&A-A](#8-需要-owner-确认的问题) |
| Q2  | `SkillDefinition` 不实现 `IHasOwner` / `IHasRowVersion`                                                                          | Skill 库是全体成员共享的目录（[ui-spec.md §4.3.5](../../01-requirements/ui-spec.md) 未展示"上传者"字段），且无运行期 Update 场景（[§1.2](#12-范围)），同 [HD-016 `ToolDefinition` §1.3 Q3](HD-016-Inkwell.Core.Tools.md#13-关键决策摘要) 先例                                                                                                                                                                          |
| Q3  | `ISkillContentResolver.ResolveAsync` 对"绑定引用的 `SkillId` 在目录中不存在"**不抛异常**，而是归入返回结果的 `MissingSkillIds` 集合，跳过该项继续解析其余绑定 | 与 [HD-016 `IToolBindingResolver` §1.3 Q5](HD-016-Inkwell.Core.Tools.md#13-关键决策摘要)"配置错误应尽早显式失败"**刻意不同**——[EX-008](../../01-requirements/requirements.md)明确要求"Skill 在对话期被后端判定缺失...对话不阻断，本轮回答未包含该 Skill 上下文"，若整体抛异常中断解析会导致对话彻底失败，与 EX-008 字面矛盾；工具绑定没有对应的"允许部分失败"验收要求，故两个 Resolver 采用不同容错策略是有意为之，非疏漏 |
| Q4  | `SkillDefinition.Name` **不**加唯一索引 / 唯一性校验                                                                             | Skill 库允许多个成员各自上传，与 [HD-015 `AgentDefinition.Name`](HD-015-Inkwell.Core.Agents.md#31-persistenceagentsagentdefinitioncs)"不加唯一约束（多用户可同名）"同理；不同于 [HD-016 `ToolDefinition.Name`](HD-016-Inkwell.Core.Tools.md#31-persistencetoolstooldefinitioncs) 唯一索引（后者是单一管理员通过 `InkwellSeeder` 维护的系统级目录，天然不会冲突）                                                          |
| Q5  | 不提供独立的 `ValidateSkillBindingAsync`（对比 [HD-016 `IToolCatalogService.ValidateToolBindingAsync`](HD-016-Inkwell.Core.Tools.md#33-toolsitoolcatalogservicecs)） | [HD-015 `AgentSkillBinding(Guid SkillId)`](HD-015-Inkwell.Core.Agents.md#31-persistenceagentsagentdefinitioncs) 只有 `SkillId` 一个字段、无参数需要必填校验（不同于 `AgentToolBinding.ParametersJson`），存在性校验已由 `GetSkillAsync`（未命中抛 `KeyNotFoundException`）覆盖，单独声明一个语义重复的校验方法是不必要的抽象（YAGNI）                                                                          |
| Q6  | `ListAvailableSkillsAsync` 循环分页拉取直至取尽（不暴露 `Pagination` 参数给调用方）                                              | 同 [HD-016 §3.7 2026-07-08 errata](HD-016-Inkwell.Core.Tools.md#37-inkwellcoretoolstoolcatalogservicecs)已修复的 `Pagination(1, 1000)` 越界教训（本仓库仓库记忆已记录该反复出现的缺陷），本 HD 从起草之初直接采用循环模式，不引入相同缺陷                                                                                                                                                                              |
| Q7  | `SkillUploadRequest.PackageEntries` 中每条目须归类为 `references/` 或 `assets/` 前缀之一，否则视为结构不合规                     | [requirements.md §4.4](../../01-requirements/requirements.md)"仅 `SKILL.md` + `references/` + `assets/`"是封闭枚举式的允许列表，非"至少排除 `scripts/`"的黑名单语义；本 HD 采用白名单校验（更贴近字面"仅...+...+..."），未归类条目 → `ArgumentException`                                                                                                                                                             |

### 1.4 与 `Inkwell.Core.AgentRuntime` 的边界声明（Skill 目录 / 解析 vs Skill 激活）

参考 [`microsoft/agent-framework` 的 `AgentSkillsProvider`](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI/Skills/AgentSkillsProvider.cs)（`AIContextProvider` 实现，采用 advertise → load → read-resource 三层渐进式披露）真实源码确认的边界，与 [HD-016 §1.4](HD-016-Inkwell.Core.Tools.md#14-与-inkwellcoreagentruntime-的边界声明工具定义-vs-工具执行)"工具定义 vs 工具执行"同构：

**`Inkwell.Core.Skills`（本 HD）负责**：

1. Skill 目录元数据的存储与查询（`SkillDefinition` / `ISkillRepository` / `ISkillCatalogService`）——纯数据与解析逻辑，零 MAF 依赖
2. `SKILL.md` frontmatter（`name`/`description`）+ 正文的解析、包结构合规校验（拒收 `scripts/`）——纯字符串 / 正则解析，零 MAF 依赖
3. `AgentSkillBinding` → `SkillContent` 的**形态翻译**（`ISkillContentResolver.ResolveAsync`）——构造纯 BCL DTO 列表，不涉及任何 MAF 类型

**`Inkwell.Core.AgentRuntime`（未起草，接口形态未锁定）专属负责**：

1. 渐进式披露的运行时机制——把 `SkillContent` 列表中的 `Name`/`Description` 注入系统提示（对应 `AgentSkillsProvider` 的 advertise 阶段），并注册 `load_skill`/`read_skill_resource` 等 `AIFunction` 供模型按需调用（对应 load / read-resource 阶段）
2. Activation"命中"判定——决定某个已挂载 Skill 是否在当前对话轮次真正被采纳（具体判定算法未决，不属于本 HD，也不由本 HD 预判）
3. Execution——v1 恒不支持（[ADR-010](../../03-architecture/adr/ADR-010-skill-loading-static-only-v1.md)），`Inkwell.Core.AgentRuntime` 若发现某 Skill 声明了脚本能力应直接忽略（本 HD 通过"整体拒收含 `scripts/` 的上传"已从源头保证目录中不存在此类 Skill，`Inkwell.Core.AgentRuntime` 侧不应遇到这种情况）

**判定依据**：与 [HD-016 §1.4](HD-016-Inkwell.Core.Tools.md#14-与-inkwellcoreagentruntime-的边界声明工具定义-vs-工具执行) 相同标准——本 HD 全部代码路径可脱离 [Microsoft Agent Framework](../../../../../microsoft/agent-framework/) 独立编译运行；一旦涉及"当前对话是否应该采纳某 Skill"这一实时决策，或涉及把 Skill 内容包装成 MAF 函数调用协议对象，就越界进入 `Inkwell.Core.AgentRuntime` 专属范围。

## 2. 文件结构

```text
src/core/Inkwell.Abstractions/
  Persistence/
    Skills/                               # 新增子目录（HD-020）
      SkillDefinition.cs                  # 业务 Model
      ISkillRepository.cs                 # 具名 Repository（3 方法：AddSkill/GetSkill/ListSkills）
  Skills/                                 # 新增子目录（HD-020）
    ISkillCatalogService.cs               # 顶层业务门面（只读查询 + 上传注册）
    ISkillContentResolver.cs              # AgentSkillBinding → SkillContent 翻译（含 SkillResolutionResult 同文件共置）
    SkillUploadRequest.cs                 # 上传请求 DTO（含 SkillPackageEntry 同文件共置）
    SkillContent.cs                       # 解析结果内容 DTO
    SkillOptions.cs                       # EnableSensitiveDataLogging
    SkillOptionsValidator.cs              # IValidateOptions<SkillOptions>

src/core/Inkwell.Core/
  Skills/                                 # 新增子目录（HD-020）
    SkillCatalogService.cs                # 唯一 ISkillCatalogService 实现
    SkillContentResolver.cs               # 唯一 ISkillContentResolver 实现
    SkillsBuilderExtensions.cs            # UseDefaultSkillService()
```

**文件计数**：`Persistence/Skills/` 新增 2 个 + `Skills/`（Abstractions）新增 6 个，合计 8 个；Abstractions csproj 累计 90（HD-001~HD-019，[HD-019 §2 文件计数](HD-019-Inkwell.Core.Models.md#2-文件结构)）+ 8（HD-020）= **98** 个 `*.cs` + 1 个 `.csproj`。`Inkwell.Core.csproj` 在 `Skills/` 新增 3 个，累计 21（HD-014~HD-019）+ 3（HD-020）= **24** 个 `*.cs` + 1 个 `.csproj`。

## 3. 程序文件设计（10 字段 × 8 文件）

### 3.1 `Persistence/Skills/SkillDefinition.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                    |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Persistence/Skills/SkillDefinition.cs`                                                                                                                                                                                                                                                                                                   |
| 职责         | Skill 目录业务 Model；撞名降级 `SkillDefinition`（[database-design.md 已知降级清单](../database-design.md)预先列出的成员，[HD-002 §4.1.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 命名规则）；承载 [REQ-008](../../01-requirements/requirements.md) 全部 Skill 目录字段                                                             |
| 对外接口     | `public sealed record class SkillDefinition : IHasTimestamps { public required Guid Id { get; init; } public required string Name { get; init; } public required string Description { get; init; } public required string ContentMarkdown { get; init; } public IReadOnlyList<Uri> ReferenceFileUris { get; init; } = Array.Empty<Uri>(); public IReadOnlyList<Uri> AssetFileUris { get; init; } = Array.Empty<Uri>(); public DateTimeOffset CreatedTime { get; init; } public DateTimeOffset UpdatedTime { get; init; } }` |
| 内部函数或类 | 无内部方法（纯数据 record）；不实现 `IHasOwner`/`IHasRowVersion`（[§1.3 Q2](#13-关键决策摘要)）                                                                                                                                                                                                                                                                        |
| 输入数据     | 由 `SkillCatalogService`（`Inkwell.Core.Skills`）在 `UploadSkillAsync` 内组装                                                                                                                                                                                                                                                                                            |
| 输出数据     | `SkillDefinition` 实例                                                                                                                                                                                                                                                                                                                                                  |
| 依赖模块     | `Persistence/Mixins/IHasTimestamps.cs`（[HD-002](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）                                                                                                                                                                                                                                             |
| 错误处理     | Model 自身不做业务校验；`Name`/`Description`/`ContentMarkdown` 解析失败的校验在 `SkillCatalogService.UploadSkillAsync` 内完成（详 §3.7）                                                                                                                                                                                                                              |
| 日志要求     | Model 自身不做日志；`ContentMarkdown`/`ReferenceFileUris`/`AssetFileUris` 原文**不得**进入任何 OTel 字段（可能含业务敏感内容，同 [HD-006 §7.2](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) PII 提示）                                                                                                                                    |
| 测试要求     | `SkillDefinitionTests.cs`：(1) 全部字段可正常构造；(2) `ReferenceFileUris`/`AssetFileUris` 默认空集合；(3) record equality                                                                                                                                                                                                                                             |

### 3.2 `Persistence/Skills/ISkillRepository.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                        |
| ------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Persistence/Skills/ISkillRepository.cs`                                                                                                                                                                                                                                                                                       |
| 职责         | 具名 Repository；继承 [HD-002 §3.2 `IRepository<TModel, TKey>`](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#32-persistenceirepositorycs) marker；3 个具名动词方法（`Add`/`Get`/`List`，不含 `Update`/`Delete`，[§1.2](#12-范围)）                                                                                              |
| 对外接口     | `public interface ISkillRepository : IRepository<SkillDefinition, Guid> { Task<SkillDefinition> AddSkill(SkillDefinition skill, CancellationToken ct = default); Task<SkillDefinition> GetSkill(Guid id, CancellationToken ct = default); Task<PagedResult<SkillDefinition>> ListSkills(Pagination pagination, SortOrder sort, CancellationToken ct = default); }` |
| 内部函数或类 | 接口本身；实现由未来 `providers/Inkwell.Persistence.EFCore` errata 追加（同 [HD-016](HD-016-Inkwell.Core.Tools.md) 遗留契约缺口处理方式，本 HD 不改写已 reviewed 的 [HD-009](../Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)）                                                                                                     |
| 输入数据     | `SkillDefinition` 实例（`AddSkill`） / `Guid id`（`GetSkill`） / `Pagination`+`SortOrder`（`ListSkills`）                                                                                                                                                                                                                                                    |
| 输出数据     | `Task<SkillDefinition>`（`AddSkill`/`GetSkill`） / `Task<PagedResult<SkillDefinition>>`（`ListSkills`）                                                                                                                                                                                                                                                      |
| 依赖模块     | `SkillDefinition.cs` / `Common/Pagination.cs` / `Common/SortOrder.cs` / `PagedResult.cs`（均 [HD-001](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md) / [HD-002](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 已锁）                                                                                        |
| 错误处理     | `GetSkill` 找不到 → `KeyNotFoundException`；命令超时 → `TimeoutException`；`AddSkill` 无唯一约束冲突场景（[§1.3 Q4](#13-关键决策摘要) `Name` 不唯一）                                                                                                                                                                                                        |
| 日志要求     | 实现层（未来 errata）写 OTel span `db.repository.skill.<verb>`（同 [HD-002 §3.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) 既定模式）                                                                                                                                                                                        |
| 测试要求     | `ISkillRepositoryContractTests.cs`：契约测试（接口形态锁定）；行为测试留待 EFCore 实现 errata 追加时补齐                                                                                                                                                                                                                                                    |

### 3.3 `Skills/ISkillCatalogService.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                             |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Skills/ISkillCatalogService.cs`                                                                                                                                                                                                                                                                                                                                   |
| 职责         | 顶层业务门面；Skill 目录只读查询 + 上传注册（[REQ-008](../../01-requirements/requirements.md) / [AC-030](../../01-requirements/acceptance-criteria.md)）；全部签名走裸 `Task<T>` + BCL 异常（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)）                                                                                                        |
| 对外接口     | `public interface ISkillCatalogService { Task<IReadOnlyList<SkillDefinition>> ListAvailableSkillsAsync(CancellationToken ct = default); Task<SkillDefinition> GetSkillAsync(Guid skillId, CancellationToken ct = default); Task<SkillDefinition> UploadSkillAsync(SkillUploadRequest request, CancellationToken ct = default); }`                                                             |
| 内部函数或类 | 接口本身；实现由 `Inkwell.Core.Skills.SkillCatalogService` 提供（唯一实现）                                                                                                                                                                                                                                                                                                                     |
| 输入数据     | `Guid skillId` / `SkillUploadRequest request` / `CancellationToken`                                                                                                                                                                                                                                                                                                                              |
| 输出数据     | `Task<IReadOnlyList<SkillDefinition>>`（`ListAvailableSkillsAsync`） / `Task<SkillDefinition>`（`GetSkillAsync`/`UploadSkillAsync`）                                                                                                                                                                                                                                                             |
| 依赖模块     | `Persistence/Skills/SkillDefinition.cs` / `Skills/SkillUploadRequest.cs`                                                                                                                                                                                                                                                                                                                         |
| 错误处理     | `GetSkillAsync` 的 `skillId` 不存在 → `KeyNotFoundException`；`UploadSkillAsync`——`request.SkillMdContent` 为空/frontmatter 无法解析 → `ArgumentException`（message 前缀 `"SKILL.md frontmatter is missing or invalid"`）；`PackageEntries` 含 `scripts/` 前缀条目 → `ArgumentException`（message 前缀 `"Skill package contains disallowed 'scripts/' entry"`，[§1.3 Q1](#13-关键决策摘要)）；`PackageEntries` 含未归类为 `references/`/`assets/` 前缀的条目 → `ArgumentException`（message 前缀 `"Unrecognized skill package entry"`，[§1.3 Q7](#13-关键决策摘要)）；取消 → `OperationCanceledException` |
| 日志要求     | 实现层写 OTel span `skill.<verb>`（`list_available`/`get`/`upload`），字段 `skill.id`（`get`/`upload`） / `skill.operation_outcome` + 5 个 `exception.*`（详 §4.3）；`request.SkillMdContent` 原文**不得**进 OTel                                                                                                                                                                                |
| 测试要求     | `tests/core/Inkwell.Abstractions.Tests/Skills/ISkillCatalogServiceContractTests.cs`：契约测试（ABI 锁定）；行为测试由 `Inkwell.Core` 独立测试项目覆盖                                                                                                                                                                                                                                          |

### 3.4 `Skills/ISkillContentResolver.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Skills/ISkillContentResolver.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| 职责         | 把 [HD-015 `AgentSkillBinding`](HD-015-Inkwell.Core.Agents.md#31-persistenceagentsagentdefinitioncs) 列表解析为 `SkillContent` 列表；本 HD 的核心交付物，预期消费方为未起草的 `Inkwell.Core.AgentRuntime`（经已 reviewed 的 [HD-015 `AgentInvocationService`](HD-015-Inkwell.Core.Agents.md#34-agentsiagentinvocationservicecs) 转发，详见 [§7](#7-跨-hd-已知缺口消费方尚无接线点) 已知缺口）；对缺失 `SkillId` 采用不中断策略（[§1.3 Q3](#13-关键决策摘要)）                                                                                            |
| 对外接口     | `public interface ISkillContentResolver { Task<SkillResolutionResult> ResolveAsync(IReadOnlyList<AgentSkillBinding> bindings, CancellationToken ct = default); }`；同文件追加 `public sealed record SkillResolutionResult(IReadOnlyList<SkillContent> ResolvedSkills, IReadOnlyList<Guid> MissingSkillIds);`（小 DTO 与接口紧密相关同文件共置，同 [HD-006 `JsonDelegateAIFunction.cs`](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#37-agentruntimejsondelegateaifunctioncs--agenttoolcallrecordcs) 共置惯例）                |
| 内部函数或类 | 接口本身；实现（`Inkwell.Core.Skills.SkillContentResolver`）内部：(1) `bindings` 为空集合 → 直接返回 `new SkillResolutionResult(Array.Empty<SkillContent>(), Array.Empty<Guid>())`；(2) 对每个 `binding` 尝试经 `IPersistenceProvider.GetRepository<ISkillRepository>().GetSkill(binding.SkillId)` 取目录元数据；(3) 命中 → 映射为 `SkillContent`加入 `resolvedSkills`；(4) 抛 `KeyNotFoundException` → 捕获后把 `binding.SkillId` 加入 `missingSkillIds`，**不重新抛出**，继续处理下一个绑定（[§1.3 Q3](#13-关键决策摘要)，直接对应 [EX-008](../../01-requirements/requirements.md)） |
| 输入数据     | `IReadOnlyList<AgentSkillBinding> bindings`（[HD-015](HD-015-Inkwell.Core.Agents.md) 已锁定类型，仅 `Guid SkillId` 一个字段）                                                                                                                                                                                                                                                                                                                                                                                                                              |
| 输出数据     | `Task<SkillResolutionResult>`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| 依赖模块     | `Persistence/Skills/ISkillRepository.cs` / `Skills/SkillContent.cs` / `Inkwell.Abstractions.Persistence.Agents.AgentSkillBinding`（[HD-015](HD-015-Inkwell.Core.Agents.md)）                                                                                                                                                                                                                                                                                                                                                                              |
| 错误处理     | `binding.SkillId` 不存在于目录 → **不抛出**，归入 `MissingSkillIds`（[§1.3 Q3](#13-关键决策摘要)）；`IPersistenceProvider` 本身的基础设施异常（`IOException`/`TimeoutException`）原样上抛（不属于"业务性缺失"，与"目录中确实没有这条记录"的 `KeyNotFoundException` 分流处理）；取消 → `OperationCanceledException`                                                                                                                                                                                                                                       |
| 日志要求     | OTel span `skill.resolve_bindings`，字段 `skill.binding_count`/`skill.resolved_count`/`skill.missing_count`；`ContentMarkdown` 原文**不得**进 OTel                                                                                                                                                                                                                                                                                                                                                                                                        |
| 测试要求     | `ISkillContentResolverContractTests.cs`：契约测试（ABI 锁定）；行为测试（含"空绑定列表返回空结果"、"部分 `SkillId` 缺失时其余正常解析且缺失项归入 `MissingSkillIds`、不抛异常"、"全部缺失时 `ResolvedSkills` 为空但方法不抛异常"、"字段 1:1 映射正确性"）在 `Inkwell.Core` 独立测试项目                                                                                                                                                                                                                                                              |

### 3.5 `Skills/SkillUploadRequest.cs`

| 字段         | 内容                                                                                                                                                                                                                                                        |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Skills/SkillUploadRequest.cs`                                                                                                                                                                                                |
| 职责         | `UploadSkillAsync` 的请求 DTO；覆盖 [ui-spec.md §4.3.5](../../01-requirements/ui-spec.md)"上传 Skill"流程所需的输入形状（假设调用方已完成文件存储，详顶部 callout）                                                                                        |
| 对外接口     | `public sealed record SkillUploadRequest { public required string SkillMdContent { get; init; } public required IReadOnlyList<SkillPackageEntry> PackageEntries { get; init; } = Array.Empty<SkillPackageEntry>(); }`；同文件追加 `public sealed record SkillPackageEntry(string RelativePath, Uri StorageUri);` |
| 内部函数或类 | 无内部方法；`PackageEntries` 可为空集合（Skill 只含 `SKILL.md`，无 `references/`/`assets/` 附件时合法）                                                                                                                                                    |
| 输入数据     | 由 `Inkwell.WebApi`（未起草）在完成 multipart 解包 + 经 [HD-003 `IFileStorageProvider.UploadAsync`](../Inkwell.Abstractions/HD-003-Inkwell.Abstractions-file-storage-port.md) 上传各附件后组装传入                                                          |
| 输出数据     | `SkillUploadRequest` 实例                                                                                                                                                                                                                                    |
| 依赖模块     | `System.*`（`Uri`）                                                                                                                                                                                                                                          |
| 错误处理     | DTO 自身不做校验；结构合规校验在 `SkillCatalogService.UploadSkillAsync` 内完成（[§3.3](#33-skillsiskillcatalogservicecs) 错误处理 + [§1.3 Q1/Q7](#13-关键决策摘要)）                                                                                        |
| 日志要求     | DTO 自身不做日志；`SkillMdContent` 原文**不得**进 OTel                                                                                                                                                                                                       |
| 测试要求     | `SkillUploadRequestTests.cs`：(1) `PackageEntries` 为空集合合法；(2) 全部字段可正常构造；(3) record equality                                                                                                                                                |

### 3.6 `Skills/SkillContent.cs`

| 字段         | 内容                                                                                                                                                                                                                                    |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 文件路径     | `src/core/Inkwell.Abstractions/Skills/SkillContent.cs`                                                                                                                                                                                  |
| 职责         | `ISkillContentResolver.ResolveAsync` 单条解析结果 DTO；供未起草的 `Inkwell.Core.AgentRuntime` 组装渐进式披露的 advertise 元数据（`Name`/`Description`）与按需加载正文（`ContentMarkdown`/附件 `Uri`）                                    |
| 对外接口     | `public sealed record SkillContent(Guid SkillId, string Name, string Description, string ContentMarkdown, IReadOnlyList<Uri> ReferenceFileUris, IReadOnlyList<Uri> AssetFileUris);`                                                     |
| 内部函数或类 | 无内部方法（纯数据 record）                                                                                                                                                                                                              |
| 输入数据     | 由 `SkillContentResolver.ResolveAsync` 从 `SkillDefinition` 映射构造                                                                                                                                                                     |
| 输出数据     | `SkillContent` 实例                                                                                                                                                                                                                      |
| 依赖模块     | `System.*`（`Uri`）                                                                                                                                                                                                                      |
| 错误处理     | DTO 自身不产生异常                                                                                                                                                                                                                       |
| 日志要求     | `ContentMarkdown` 原文**不得**进 OTel                                                                                                                                                                                                     |
| 测试要求     | `SkillContentTests.cs`：(1) 全部字段可正常构造；(2) record equality                                                                                                                                                                     |

### 3.7 `Skills/SkillOptions.cs`

| 字段         | 内容                                                                                                                                                                                                                             |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Skills/SkillOptions.cs`                                                                                                                                                                          |
| 职责         | Skills 模块详细配置；从 `appsettings.json` `"Inkwell:Skills"` 段绑定                                                                                                                                                            |
| 对外接口     | `public sealed class SkillOptions { public bool EnableSensitiveDataLogging { get; init; } = false; }`                                                                                                                          |
| 内部函数或类 | 无跨字段约束；本 HD 未发现需要暴露的其他配置项（[ui-spec.md](../../01-requirements/ui-spec.md)/[acceptance-criteria.md](../../01-requirements/acceptance-criteria.md) 均未给出 Skill 相关的数值上限，同 [§1.2](#12-范围) 不发明未被要求的能力原则，最小化 Options 表面） |
| 输入数据     | 由 `IConfiguration` 绑定                                                                                                                                                                                                        |
| 输出数据     | `SkillOptions` 实例（DI 通过 `IOptions<SkillOptions>` 注入）                                                                                                                                                                    |
| 依赖模块     | 无（不含 DataAnnotations 属性，字段本身是 `bool`，无需校验特性）                                                                                                                                                                |
| 错误处理     | 无校验失败场景（当前仅一个 `bool` 字段，无约束）                                                                                                                                                                                |
| 日志要求     | 无                                                                                                                                                                                                                                |
| 测试要求     | `SkillOptionsTests.cs`：(1) 默认值 `false`；(2) `appsettings.json` 绑定生效                                                                                                                                                     |

### 3.8 `Skills/SkillOptionsValidator.cs`

| 字段         | 内容                                                                                                                                                          |
| ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Abstractions/Skills/SkillOptionsValidator.cs`                                                                                              |
| 职责         | `IValidateOptions<SkillOptions>` 实现；保持与其余模块 Options 校验器结构一致（即使当前无实质校验规则，便于未来新增字段时不必重新搭建校验管线）                |
| 对外接口     | `internal sealed class SkillOptionsValidator : IValidateOptions<SkillOptions> { public ValidateOptionsResult Validate(string? name, SkillOptions options); }` |
| 内部函数或类 | `Validator.TryValidateObject` DataAnnotations（当前无 `[Range]`/`[Required]` 等特性可校验，恒为 `Success`，为未来扩展预留结构）                                |
| 输入数据     | `SkillOptions` 实例                                                                                                                                           |
| 输出数据     | `ValidateOptionsResult.Success`（当前恒定）                                                                                                                  |
| 依赖模块     | `Microsoft.Extensions.Options` / `System.ComponentModel.DataAnnotations`                                                                                     |
| 错误处理     | 同 [HD-001 §3.12](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md)，当前无失败路径                                                        |
| 日志要求     | 无                                                                                                                                                             |
| 测试要求     | `SkillOptionsValidatorTests.cs`：(1) 默认值恒通过校验                                                                                                        |

### 3.9 `Inkwell.Core/Skills/SkillCatalogService.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Core/Skills/SkillCatalogService.cs`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| 职责         | `ISkillCatalogService` 唯一实现；只读查询 + 上传解析与持久化的完整业务逻辑                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| 对外接口     | `internal sealed class SkillCatalogService : ISkillCatalogService { public SkillCatalogService(IPersistenceProvider persistence); /* 3 个接口方法实现 */ }`                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| 内部函数或类 | `private static bool TryParseFrontmatter(string skillMdContent, out string name, out string description, out string contentMarkdown)`：用简单的 `^---\s*$...^---\s*$` 正则（同 [`AgentFileSkillsSource` 的 `s_frontmatterRegex`](../../../../../microsoft/agent-framework/dotnet/src/Microsoft.Agents.AI/Skills/File/AgentFileSkillsSource.cs) 思路，本 HD 独立实现、不引用 MAF 程序集）提取 `---` 分隔的 YAML 块，逐行解析 `name:`/`description:` 键值对；`contentMarkdown` = frontmatter 块之后的正文（`Trim()`）；任一必填键缺失或分隔符不匹配 → 返回 `false`；`private static void ValidatePackageStructure(IReadOnlyList<SkillPackageEntry> entries, out IReadOnlyList<Uri> referenceUris, out IReadOnlyList<Uri> assetUris)`：遍历 `entries`，`RelativePath`（统一转正斜杠、去首尾空白后）以 `scripts/`（忽略大小写）为前缀 → 抛 `ArgumentException`（[§1.3 Q1](#13-关键决策摘要)）；以 `references/` 为前缀 → 归入 `referenceUris`；以 `assets/` 为前缀 → 归入 `assetUris`；两者都不是 → 抛 `ArgumentException`（[§1.3 Q7](#13-关键决策摘要)） |
| 输入数据     | `IPersistenceProvider`（[HD-002](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| 输出数据     | 见 §3.3 `ISkillCatalogService` 各方法签名                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| 依赖模块     | `Inkwell.Abstractions.{Persistence.IPersistenceProvider,Persistence.Skills.ISkillRepository,Skills.ISkillCatalogService}` / `System.Text.RegularExpressions`（BCL）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| 错误处理     | `ListAvailableSkillsAsync`——**循环分页拉取直至取尽**（同 [HD-016 §3.7](HD-016-Inkwell.Core.Tools.md#37-inkwellcoretoolstoolcatalogservicecs) 已定型模式）：`page` 从 `1` 起，每次以 `persistence.GetRepository<ISkillRepository>().ListSkills(new Pagination(page, Pagination.MaxPageSize), ...)` 拉取并累加 `Items`，直至 `PagedResult<SkillDefinition>.HasNextPage` 为 `false`（[§1.3 Q6](#13-关键决策摘要)）；`GetSkillAsync`——`GetSkill` 找不到 → `KeyNotFoundException` 原样上抛；`UploadSkillAsync`——`TryParseFrontmatter` 失败 → `ArgumentException`（message 前缀 `"SKILL.md frontmatter is missing or invalid"`）；`ValidatePackageStructure` 内部抛出的 `ArgumentException` 原样透传；校验通过后经 `persistence.ExecuteInTransactionAsync` 调 `uow.GetRepository<ISkillRepository>().AddSkill(new SkillDefinition { Id = Guid.CreateVersion7(), ... })`（同 [HD-015 §3.9](HD-015-Inkwell.Core.Agents.md#39-inkwellcoreagentsagentservicecs) `CreateAgentAsync` 事务模式）                                    |
| 日志要求     | 见 §3.3 日志要求（OTel span `skill.<verb>`）                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| 测试要求     | `SkillCatalogServiceTests.cs`（`Inkwell.Core.Tests`）：(1) `ListAvailableSkillsAsync`/`GetSkillAsync` 成功路径；(2) `GetSkillAsync` 未知 `skillId` 抛 `KeyNotFoundException`；(3) `UploadSkillAsync` 合法 SKILL.md（含/不含附件）成功入库；(4) `UploadSkillAsync` frontmatter 缺失 `name`/`description` 抛 `ArgumentException`；(5) `UploadSkillAsync` `PackageEntries` 含 `scripts/` 前缀条目（大小写变体）抛 `ArgumentException` 且不写入数据库；(6) `UploadSkillAsync` `PackageEntries` 含未归类条目抛 `ArgumentException`；(7) `ListAvailableSkillsAsync` 在目录条目数超过 `Pagination.MaxPageSize` 时仍能正确取尽全部结果（循环分页回归测试，同 HD-016 §3.7 教训）                                                                                    |

### 3.10 `Inkwell.Core/Skills/SkillContentResolver.cs`

| 字段         | 内容                                                                                                                                                                                                                                                                                                                                                    |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Core/Skills/SkillContentResolver.cs`                                                                                                                                                                                                                                                                                                  |
| 职责         | `ISkillContentResolver` 唯一实现；`AgentSkillBinding` → `SkillContent` 的翻译 + 缺失项容错逻辑（详 §3.4 内部逻辑描述）                                                                                                                                                                                                                                  |
| 对外接口     | `internal sealed class SkillContentResolver : ISkillContentResolver { public SkillContentResolver(IPersistenceProvider persistence); public Task<SkillResolutionResult> ResolveAsync(IReadOnlyList<AgentSkillBinding> bindings, CancellationToken ct = default); }`                                                                                   |
| 内部函数或类 | `private static SkillContent ToSkillContent(SkillDefinition skill) => new(skill.Id, skill.Name, skill.Description, skill.ContentMarkdown, skill.ReferenceFileUris, skill.AssetFileUris);`                                                                                                                                                             |
| 输入数据     | `IPersistenceProvider`（[HD-002](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md)）                                                                                                                                                                                                                                             |
| 输出数据     | 见 §3.4                                                                                                                                                                                                                                                                                                                                                  |
| 依赖模块     | `Inkwell.Abstractions.{Persistence.IPersistenceProvider,Persistence.Skills.ISkillRepository,Skills.{ISkillContentResolver,SkillContent,SkillResolutionResult},Persistence.Agents.AgentSkillBinding}`（[HD-015](HD-015-Inkwell.Core.Agents.md)）                                                                                                        |
| 错误处理     | 见 §3.4（`KeyNotFoundException` 捕获后归入 `MissingSkillIds`，不透传；其余异常原样透传）                                                                                                                                                                                                                                                                |
| 日志要求     | 见 §3.4                                                                                                                                                                                                                                                                                                                                                  |
| 测试要求     | `SkillContentResolverTests.cs`（`Inkwell.Core.Tests`）：(1) 全部绑定命中时 `MissingSkillIds` 为空；(2) 部分绑定缺失时对应 `SkillId` 出现在 `MissingSkillIds` 且不抛异常，其余正常解析；(3) 空绑定列表返回空结果且不查询仓储；(4) `IPersistenceProvider` 基础设施异常（如 `TimeoutException`）原样透传，不被误吞并归入 `MissingSkillIds`                |

### 3.11 `Inkwell.Core/Skills/SkillsBuilderExtensions.cs`

| 字段         | 内容                                                                                                                                                                                                       |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 文件路径     | `src/core/Inkwell.Core/Skills/SkillsBuilderExtensions.cs`                                                                                                                                                 |
| 职责         | Builder DSL 扩展方法，注册 `ISkillCatalogService`/`ISkillContentResolver` 默认实现（风格对齐 [HD-016 `ToolsBuilderExtensions.cs`](HD-016-Inkwell.Core.Tools.md)）                                          |
| 对外接口     | `public static class SkillsBuilderExtensions { public static IInkwellBuilder UseDefaultSkillService(this IInkwellBuilder builder, Action<SkillOptions>? configure = null); }`                             |
| 内部函数或类 | (1) `ArgumentNullException.ThrowIfNull(builder)`；(2) `builder.Services.AddScoped<ISkillCatalogService, SkillCatalogService>()` + `AddScoped<ISkillContentResolver, SkillContentResolver>()`（依赖 `IPersistenceProvider`，Scoped 生命周期，同 [HD-015](HD-015-Inkwell.Core.Agents.md)/[HD-016](HD-016-Inkwell.Core.Tools.md) 先例，避免仓库记忆记录的"Singleton 消费 Scoped"反模式）；(3) 注册 `IValidateOptions<SkillOptions>` + 绑定 `"Inkwell:Skills"` 段 + `configure`；(4) 返回 `builder` |
| 输入数据     | `IInkwellBuilder builder` / `Action<SkillOptions>? configure`                                                                                                                                             |
| 输出数据     | `IInkwellBuilder`（支持链式调用）                                                                                                                                                                          |
| 依赖模块     | `Inkwell.Abstractions.Builder.IInkwellBuilder` / `Microsoft.Extensions.DependencyInjection`                                                                                                              |
| 错误处理     | `builder` 为 `null` → `ArgumentNullException`                                                                                                                                                             |
| 日志要求     | 无（DI 装配期不产生运行时日志）                                                                                                                                                                            |
| 测试要求     | `SkillsBuilderExtensionsTests.cs`：(1) 调用后 `ISkillCatalogService`/`ISkillContentResolver` 可从 `IServiceProvider` 解析；(2) `configure` 回调生效；(3) `builder` 为 `null` 抛异常                       |

## 4. BCL 异常与日志（补充 HD-001 §4 / HD-002 §4）

### 4.1 错误码

本模块**不分配** `INK-SKILLS-NNN` 错误码，与全仓其余 HD 一致（[ADR-023 errata·01](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md#2026-05-11-errata01废错误码机制改走-net-bcl-异常类型分流)）。

### 4.2 BCL 异常分类（业务失败 vs 程序错误）

- **业务失败 / 预期错误**：`KeyNotFoundException`（`GetSkillAsync` 未命中） / `ArgumentException`（frontmatter 不可解析、包结构含 `scripts/`、包结构含未归类条目）
- **不视为异常的"缺失"场景（[§1.3 Q3](#13-关键决策摘要) 特例）**：`ISkillContentResolver.ResolveAsync` 遇到绑定引用的 `SkillId` 不存在时**不抛异常**，归入 `SkillResolutionResult.MissingSkillIds`
- **程序错误 / 基础设施故障**：`IOException`/`TimeoutException`（`IPersistenceProvider` 底层故障，原样透传）
- **取消**：`OperationCanceledException`（[HD-001 §4.3](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#43-取消传播)）

### 4.3 OTel span / 字段

- `skill.list_available` / `skill.get` / `skill.upload`（`SkillCatalogService`）
- `skill.resolve_bindings`（`SkillContentResolver`）

**Inkwell 私有字段**：`skill.id`（`get`/`upload`） / `skill.binding_count`/`skill.resolved_count`/`skill.missing_count`（`resolve_bindings`） / `skill.operation_outcome`

**OTel 标准字段**：同 [HD-006 §4.3](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#43-otel-span--字段) 5 个 `exception.*` 字段

> **PII 提示**：`SkillDefinition.ContentMarkdown` / `SkillUploadRequest.SkillMdContent` / `SkillContent.ContentMarkdown` 原文**不得**进入任何 OTel 字段。

命名沿用 [HD-019 §4.3](HD-019-Inkwell.Core.Models.md#43-otel-span--字段) 已修正的"单数域名词前缀"惯例（`skill.*`，非 `skills.*`，避免重复 [HD-016 §24 C-5](../design-review-report.md) 指出的 `tools.*` 复数不一致问题）。

## 5. 公共约定继承（HD-001）

- 命名：`ISkillCatalogService`/`ISkillContentResolver` ↔ [HD-001 §5.1](../Inkwell.Abstractions/HD-001-Inkwell.Abstractions-foundation.md#51-命名) `I<Module>Service`；`SkillDefinition` ↔ [HD-002 §4.1.2](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#412-model-类命名规则2026-05-11-errataf6--adr-022) 撞名降级；`ISkillRepository` ↔ [HD-002 §4.1.1](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md) `I<TypeName>Repository`
- 签名：全部方法裸 `Task<T>` + BCL 异常（[ADR-023](../../03-architecture/adr/ADR-023-port-signature-bare-task-with-exceptions.md)）；`CancellationToken ct = default` 全方法必填
- Repository 具名动词：`Add`/`Get`/`List` 三选一开头，无 `Async` 后缀（[HD-002 §4.1.3](../Inkwell.Abstractions/HD-002-Inkwell.Abstractions-persistence-port.md#413-repository-方法动词白名单2026-05-11-errataf6--adr-022)）

## 6. 数据库设计增量（追加至 `database-design.md`）

### 表 `skills`（[REQ-008](../../01-requirements/requirements.md) + [ADR-010](../../03-architecture/adr/ADR-010-skill-loading-static-only-v1.md)）

- `Id`：`Guid` v7，主键
- `Name`：`string`，长度上限 100（作者判断，非 Owner 拍板，需求未指定具体上限，同 [HD-016 `ToolDefinition.Name`](HD-016-Inkwell.Core.Tools.md) 上限值先例），**不加唯一约束**（[§1.3 Q4](#13-关键决策摘要)）
- `Description`：`string`，无长度上限
- `ContentMarkdown`：`string`，无长度上限（数据库侧用 Provider 允许的最大文本类型；三 Provider 最小公倍数按 [database-design.md 总体设计原则](../database-design.md)"禁用 Provider-specific 类型"处理为通用 `string`）
- `ReferenceFileUrisJson`：`string`，默认 `"[]"`（`IReadOnlyList<Uri>` 序列化存储，JSON 列 + `JsonSerializer` value converter）
- `AssetFileUrisJson`：`string`，默认 `"[]"`
- `CreatedTime` / `UpdatedTime`：`IHasTimestamps`

**索引**：无（Skill 库按目录整体拉取展示，[§1.3 Q4](#13-关键决策摘要) `Name` 无唯一索引，暂无其他过滤维度需要索引）。**不**包含 `RowVersion`（[§1.3 Q2](#13-关键决策摘要)，v1 无运行期 Update 场景）；**不**包含 `OwnerUserId`（Skill 库是全体成员共享的目录，[§1.3 Q2](#13-关键决策摘要)）。

**Entity / Mapping / Repository 实现物理位置**：`providers/Inkwell.Persistence.EFCore/{Entities,Mapping,Repositories}/`（[ADR-021](../03-architecture/adr/ADR-021-efcore-persistence-shared-base-and-provider-csproj-layout.md) + [ADR-022](../03-architecture/adr/ADR-022-entity-domain-mapper-selection.md) 锁定物理位置）——**本节仅记录契约缺口**，具体实现需通过 errata 追加到已 reviewed 的 [HD-009](Inkwell.Persistence.EFCore/HD-009-Inkwell.Persistence.EFCore-base.md)，本次提交不改写 HD-009。

## 7. 跨 HD 已知缺口（消费方尚无接线点）

- **`AgentRunRequest`（[HD-006](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md#32-agentruntimeagentrunrequestcs)，已 reviewed）当前没有承载"已解析 Skill 内容"的字段**——该 DTO 现有字段为 `RunId`/`AgentId`/`ConversationId`/`Messages`/`Instructions`/`ModelId`/`ModelParameters`/`Tools`，无 `Skills` 或等价字段；[HD-015 `AgentInvocationService`](HD-015-Inkwell.Core.Agents.md#34-agentsiagentinvocationservicecs)（已 reviewed）当前也**未**注入本 HD 的 `ISkillContentResolver`，无法在组装 `AgentRunRequest` 时调用 `ResolveAsync`。这与 [HD-016 §3.4](HD-016-Inkwell.Core.Tools.md#34-toolsitoolbindingresolvercs)"已知缺口"当初"`AgentRunRequest.Tools` 恒为 `null`，待 HD-015 消费 HD-016 后经 errata 补齐"是**同类缺口**——本 HD 不擅自修改已 reviewed 的 [HD-006](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md) / [HD-015](HD-015-Inkwell.Core.Agents.md) 正文，该缺口需要未来一次专门的 errata（形态可能是：`AgentRunRequest` 新增 `IReadOnlyList<SkillContent>? Skills` 字段 + `AgentInvocationService` 构造函数新增 `ISkillContentResolver` 依赖，与 [HD-015 2026-07-07 errata](HD-015-Inkwell.Core.Agents.md) 消费 HD-016 的模式完全一致），本次提交仅记录缺口，不代为决策或修改。

## 8. 需要 Owner 确认的问题

- **Q&A-A（未解决）**：本 HD 判定"上传含 `scripts/` 的 Skill 应整体拒收，不入库"（[§1.3 Q1](#13-关键决策摘要)），依据是 [AC-029](../../01-requirements/acceptance-criteria.md)/[ui-spec.md §4.3.5](../../01-requirements/ui-spec.md)/[user-flow.md UF-006](../../01-requirements/user-flow.md) 三处一致的"阻断上传"表述；但已 reviewed 的 [ADR-010](../../03-architecture/adr/ADR-010-skill-loading-static-only-v1.md)"决策"正文字面写的是"SCRIPT block 处理：解析时识别但不执行；UI 显示 banner"，暗示"允许入库但屏蔽执行"。本 HD 判断三处更具体的验收 / 交互文档应优先于 ADR 决策正文的探索性措辞，但**本 HD 无权修改已 reviewed 的 ADR-010 正文**——请 Owner 确认：(a) 维持本 HD"整体拒收"的实现（当前默认假设，[§1.3 Q1](#13-关键决策摘要)），并对 ADR-010 发起 errata 更正该处表述以消除文档间矛盾；或 (b) 本 HD 应改为"识别 SCRIPT 相关文件但仍允许其余内容入库 + banner 提示"，此时需要重新设计 `UploadSkillAsync`（不整体拒收，而是剥离/标记后继续入库），并与 AC-029/ui-spec/user-flow 三处文档同步发起 errata。本 HD 当前实现选 (a)，因为它与三处独立文档更一致，但明确列出以待 Owner 裁定，不视为已解决。
- **Q&A-B（未解决）**：[§7](#7-跨-hd-已知缺口消费方尚无接线点) 记录的 `AgentRunRequest`/`AgentInvocationService` 缺少 Skill 接线点——是否现在就对已 reviewed 的 [HD-006](../Inkwell.Abstractions/HD-006-Inkwell.Abstractions-agent-runtime-port.md)/[HD-015](HD-015-Inkwell.Core.Agents.md) 发起 errata 补齐（同 HD-016 当初的处理方式），还是延后到 `Inkwell.Core.AgentRuntime` HD 起草时一并处理？本 HD 不擅自决定，仅记录缺口。
