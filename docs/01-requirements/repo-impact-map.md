---
id: repo-impact-map-custom-agent
stage: H1
status: reviewed
authors:
  - name: H1-RepoImpactMapper
    role: agent
reviewers: []
created: 2026-05-07
updated: 2026-05-07
upstream:
  - requirements-custom-agent
  - REQ-001
  - REQ-002
  - REQ-003
  - REQ-004
  - REQ-010
  - REQ-011
  - REQ-012
downstream: []
---

# 自定义 Agent 功能 · H1↔H3 仓库影响面地图

## 0. 前置说明

本图回答 **唯一一个问题**：要落地 [requirements.md](./requirements.md) 第 5 节的 MVP REQ，仓库里现有的哪些代码、文件、接口、测试会被牵动？

### 0.1 仓库基线（2026-05-07 扫描结果）

| 维度 | 状态 | 证据 |
| --- | --- | --- |
| 产品源码 | **无** | `**/*.{cs,csproj,sln,ts,tsx,json}` 仅命中 [.harness-engineering/manifest.json](../../.harness-engineering/manifest.json)（元配置） |
| 仓库根 `README.md` | **无** | `**/README.md` 仅命中 [.harness-engineering/README.md](../../.harness-engineering/README.md)（工程骨架文档） |
| 仓库根 `AGENTS.md` | **无** | `**/AGENTS.md` 0 命中 |
| 既有 ADR | **无** | `docs/03-architecture/` 不存在 |
| 既有详细设计 | **无** | `docs/04-detailed-design/` 不存在 |
| 已有原型 | 有 | [prototypes/custom-agent/](../../prototypes/custom-agent/) 5 页 + assets，**仅 H1 评审用**，**不是**产品代码 |
| Harness 工程骨架 | 有 | [.harness-engineering/](../../.harness-engineering/) `harness_version: 0.0.1`，targets=copilot |

### 0.2 验收豁免说明

按 H1-RepoImpactMapper Agent 第 7 节验收，"≥80% 条目置信度 high/medium"在 greenfield 仓库下不可达——本图所有条目置信度均为 `low`，原因是**没有任何既有产品代码可作为高置信度证据**。该豁免理由在本节显式记录，不视作质量缺陷。

### 0.3 与 H2 的边界

本图**不**替 H2-ArchitectAdvisor 做技术选型；**不**给出具体扩展名 / 框架名 / 包名 / 文件名的建议路径。"预计新增模块"列只描述功能簇，落点（前端工程根、后端 API 工程根、数据访问层、契约层、测试根）以占位符 `<...>` 表示，待 H2 技术选型 + H3 详细设计后由 H3 阶段落具体路径。

> 用户在 2026-05-07 已提交 6 项技术偏好（前端 React+TS+AntD、后端 ASP.NET Core+EF Core+MAF、数据库三选 InMemory/SQL Server/Postgres、队列与缓存二选 InMemory/Redis、对象存储多选、对话协议 AGUI）。**这些偏好属 H2 输入信号，不构成本图的落点依据**——避免在 H2 ADR 未出之前把偏好默认成事实。

## 1. 影响面表（4.1）

> 列含义：
>
> - **REQ**：来源 [requirements.md 第 5 节](./requirements.md#5-功能范围)
> - **受影响模块（已存在）**：仓库内已存在、本特性会改动到的模块；greenfield 下全部填"无"
> - **受影响文件（已存在）**：同上
> - **预计新增模块（建议）**：本期需要新建的功能簇；**仅描述功能**，路径占位
> - **受影响接口 / 数据结构**：列出已存在的；**不**发明新的
> - **受影响测试**：现有的 / 需要新增的（粗粒度）
> - **风险**：兼容性 / 性能 / 数据迁移 / 外部依赖等
> - **置信度**：见 0.2 节豁免说明

| REQ | 受影响模块（已存在） | 受影响文件（已存在） | 预计新增模块（建议） | 受影响接口 / 数据结构 | 受影响测试 | 风险 | 置信度 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| [REQ-001](./requirements.md#req-001) Agent 生命周期 | 无 | 无 | `<frontend>/我的 Agent 列表页`、`<backend>/Agent CRUD 服务`、`<dal>/Agent 持久化（含软删除 7 天）`、`<frontend>/路由守卫` | 无既有接口；新接口由 H3 定义 | 新增：列表 / 创建 / 复制 / 重命名 / 删除 / 软删除恢复 / 跨用户隔离 单测与集成测；端到端见 [F1 / F4](./user-flow.md) | R7 登录子系统硬依赖（[requirements.md R7](./requirements.md#121-风险)） | low |
| [REQ-002](./requirements.md#req-002) Agent 基础元数据 | 无 | 无 | `<frontend>/Agent 编辑页 · 基本信息子区`、`<backend>/Agent 元数据校验`、`<object-storage>/头像存储` | 无 | 新增：名称 / 头像 / 描述 校验单测；头像上传集成测 | 头像存储后端选型未定（用户偏好"本地 + 云存储多 provider"，待 H2 决策） | low |
| [REQ-003](./requirements.md#req-003-instructions系统指令-mvp) Instructions | 无 | 无 | `<frontend>/Agent 编辑页 · Instructions 子区`、`<backend>/Instructions 持久化` | 无 | 新增：长文本字符数提示 / dirty 状态 / 取消二次确认 单测 | NFR-001 不暴露开发者概念（无后端审核） | low |
| [REQ-004](./requirements.md#req-004-在线试运行--对话调试-mvp) 试运行 / 对话调试 | 无 | 无 | `<frontend>/Agent 编辑页 · 试运行下半屏`、`<backend>/对话编排服务（MAF）`、`<chat-protocol>/AGUI 服务端`、`<frontend>/AGUI 客户端` | 无 | 新增：流式对话 / 重试 3 次 / Tool 调用串接 / E6 首次启用须知 集成测 | OQ-030 落地形态待定（同页组件 vs iframe）；AGUI 协议成熟度未在仓库内验证；MAF 商业支持状态需 H2 核 | low |
| [REQ-010](./requirements.md#req-010-skill-管理agentskillsio-标准-mvp) Skill 管理 | 无 | 无 | `<frontend>/Skill 编辑器抽屉 P3`、`<backend>/SKILL.md 解析器`、`<dal>/用户级 Skill 库 + Agent-Skill 引用关系（[DB-006](./requirements.md#8-数据边界)）` | agentskills.io 规范（[外部规范](https://agentskills.io/specification)） | 新增：YAML frontmatter 解析 / name 正则 / 字段缺失诊断（[E7](./requirements.md#9-异常场景)）单测；导入 .md 集成测 | 外部规范跟随成本（规范若变更需追跟） | low |
| [REQ-011](./requirements.md#req-011-内置-tool-启用-mvp) 内置 Tool 启用 | 无 | 无 | `<frontend>/Agent 编辑页 · Tool tab`、`<backend>/Tool 启用配置（DB-007）`、`<backend>/Tool T-1 联网搜索适配器`、`<backend>/Tool T-3 当前日期适配器` | 无；首批仅 T-1 / T-3（[ND-013](./requirements.md#11-不做范围)） | 新增：T-1 联网搜索调用失败重试 3 次 / E6 首次启用须知 / Tool 启用配置 单测 | R8 第三方 Tool 合规与可用性外发依赖（[requirements.md R8](./requirements.md#121-风险)） | low |
| [REQ-012](./requirements.md#req-012-mcp-server-集成-mvp-ui-占位--vnext-后端) MCP UI 占位 | 无 | 无 | `<frontend>/Agent 编辑页 · MCP tab（disabled + Empty）` | 无；后端 MCP 调用本期不做（[ND-010](./requirements.md#11-不做范围)） | 新增：占位 tab disabled 渲染 / Empty 文案 单测 | vNext 后端启用时不能因 MVP UI 占位而锁死 contract（H3 留扩展点） | low |

### 1.1 vNext / 撤销 REQ 不在本图范围

下列 REQ 在本期不进入开发，**不**纳入影响面表，避免误导 H3 判断 MVP 落点：

- REQ-005 模型选择与运行参数 —— vNext
- REQ-006 知识库挂载 —— vNext / pending（OQ-019 仍 pending）
- REQ-007 版本管理与导入 / 导出 —— vNext
- REQ-008 共享与权限（私有 / 公开）—— vNext
- REQ-009 —— 已撤销并入 [ND-007](./requirements.md#11-不做范围)

## 2. 模块依赖摘要（4.2）

> Greenfield 仓库下，"模块依赖"退化为"按 H1 文档拆出的功能簇 + 它们之间的依赖方向"。所有模块均**待新建**，没有"当前职责"。

### 2.1 功能簇与依赖方向（按依赖深度排序）

```
<frontend>
  ├── 路由守卫 ── 依赖 ──> 平台登录子系统（外部，R7 硬依赖）
  ├── 我的 Agent 列表页 P1 ── 调 ──> <backend>/Agent CRUD 服务
  ├── Agent 编辑页 P2
  │   ├── 基本信息子区 ── 调 ──> <backend>/Agent 元数据 + <object-storage>/头像
  │   ├── Instructions 子区 ── 调 ──> <backend>/Instructions 持久化
  │   ├── Skill tab + P3 抽屉 ── 调 ──> <backend>/SKILL.md 解析器 + 用户级 Skill 库
  │   ├── Tool tab ── 调 ──> <backend>/Tool 启用配置
  │   ├── MCP tab（占位） ── 不调用后端
  │   └── 试运行下半屏 ── 调 ──> <chat-protocol>/AGUI 服务端
  └── 删除确认弹层 P4 ── 调 ──> <backend>/Agent 软删除（DB-004）

<backend>
  ├── Agent CRUD 服务 ── 用 ──> <dal>/Agent 持久化
  ├── Agent 元数据校验 ── 用 ──> <dal>/Agent 持久化
  ├── Instructions 持久化 ── 用 ──> <dal>/Agent 持久化
  ├── SKILL.md 解析器 ── 用 ──> <dal>/用户级 Skill 库 + Agent-Skill 引用
  ├── Tool 启用配置 ── 用 ──> <dal>/Agent 持久化（DB-007）
  ├── Tool T-1 联网搜索适配器 ── 调 ──> 第三方搜索服务（R8）
  ├── Tool T-3 当前日期适配器 ── 无外部依赖
  └── 对话编排服务（MAF） ── 调 ──> 模型网关 + Tool 适配器集合 + Skill 注入

<chat-protocol>
  └── AGUI 服务端 ── 调 ──> <backend>/对话编排服务

<dal> （持久化层，多 provider 待 H2 决策）
  ├── Agent 持久化（含软删除 7 天，硬删除）
  ├── 用户级 Skill 库
  ├── Agent-Skill 引用关系
  └── User 表 ── **依赖 ──> 平台登录子系统的用户主键** （R7 + D2）

<object-storage> （多 provider 待 H2 决策）
  └── 头像存储

<外部依赖>
  ├── 平台登录子系统（R7）—— 仓库内无证据
  ├── 模型网关 / gpt-4.1 网关（D1）—— 仓库内无证据
  ├── 第三方联网搜索服务（R8）—— 仓库内无证据
  └── agentskills.io 规范（REQ-010）—— 外部 spec
```

### 2.2 已知技术债务

仓库内无既有代码，**无技术债务可登记**。Harness 工程骨架内[`docs/tech-debt-tracker.md`](../../.harness-engineering/docs/tech-debt-gc.md) 提供了未来登记入口，本期暂无内容。

## 3. 缺失发现（4.3）

> 扫描中发现但**不在任何 REQ 内**的潜在缺口。**不**直接补到 REQ，由项目负责人判断是否补需求或留 H2 处理。

| 编号 | 缺失项 | 影响 | 建议处理者 / 时机 |
| --- | --- | --- | --- |
| GAP-001 | 仓库根 `AGENTS.md` 不存在 | 模块边界与禁区无显式声明；本图无法核对"REQ 与禁区冲突"；H5 编码任务卡的"允许 / 禁止修改"清单缺权威依据 | 项目负责人在 H2 启动前补；至少声明"当前无禁区，按空白处理" |
| GAP-002 | 仓库根 `README.md` 不存在 | 项目身份未声明；[.github/copilot-instructions.md](../../.github/copilot-instructions.md#L5) "项目身份与技术栈以仓库根 README.md / AGENTS.md 为准"无锚点 | 项目负责人在 H2 选型完成后补 |
| GAP-003 | R7 登录子系统在仓库内无任何证据 | REQ-001 ~ REQ-004 全部依赖"已登录用户"输入；若平台无 SSO/OIDC，本特性无法上线 | H2-ArchitectAdvisor 启动时第一件事确认平台登录方案；写入 ADR-?-平台鉴权依赖 |
| GAP-004 | 模型网关 / gpt-4.1 接入在仓库内无证据 | REQ-004 试运行 / 对话调试无法验证 | H2 启动时与 GAP-003 一并核 |
| GAP-005 | 第三方联网搜索（T-1）服务商未指定 | REQ-011 试运行无法跑通；R8 SLA 缺数 | H2 选型时指定具体服务商（如 Bing Search / Google CSE / 自建） |
| GAP-006 | `docs/03-architecture/` 与 `docs/04-detailed-design/` 目录不存在 | H2 / H3 启动后第一动作就是创建这些目录 | H2-ArchitectAdvisor 首次产出时创建 |
| GAP-007 | 5 份 H1 文档自身不引用任何具体源码路径（H1 阶段是正确的不越界） | 本图缺"现成的落点假设可以验证" | 不视作缺陷；H2 选型完成后由本图重写 v0.2 |

## 4. 与下游的交付物

### 4.1 给 H2-ArchitectAdvisor

- 第 1 节影响面表的"预计新增模块"列里所有 `<占位符>` 都需要在 H2 ADR 中给出明确的工程根 / 包结构 / 持久层抽象 / 协议适配点
- 第 2 节依赖图里所有"外部依赖"都需要在 H2 ADR 中明确接入方案（登录、模型网关、联网搜索、对象存储、AGUI 协议、MAF 框架）
- 第 3 节 GAP-001 ~ GAP-005 需要在 H2 启动会议上当面回答；不答完不进入选型评审
- 用户已提交的 6 项技术偏好（见 0.3 节）**仅作为输入信号**进入 H2 反问轮，不替代备选项打分

### 4.2 给 H5-CodingExecutor（间接，经 H3）

- 因为本图全部条目置信度 `low`，H5 的 `ai-task-brief.md` "允许 / 禁止修改文件"清单**不能**直接引用本图——需等 H2 ADR + H3 详细设计落点后由 H3 重写本图为 v0.2，再供 H5 引用。

## 5. 自检（按 Agent 第 6 步）

- [x] 表里所有"已存在"路径在仓库中真实存在 —— 全部"无"，等价于"无错误声称"
- [x] 是否有条目仅看文件名就下结论？—— 无（greenfield 没有可看的文件名）
- [x] 是否在表里悄悄列了新接口名 / 新表名？—— 无；"受影响接口"列对所有 REQ 填"无既有接口"
- [x] 跨语言 / 跨栈下置信度 `low` 是否说明？—— 0.2 节豁免说明
- [x] frontmatter 完整？—— `id` / `stage` / `status` / `authors` / `upstream` / `downstream` 齐备

## 6. 变更记录

| 版本 | 日期       | 变更人              | 变更内容                                                                                |
| ---- | ---------- | ------------------- | --------------------------------------------------------------------------------------- |
| 0.1  | 2026-05-07 | H1-RepoImpactMapper | 首版。Greenfield 仓库基线确认；7 条 MVP REQ 影响面表；7 条缺失发现；与 H2 / H5 交付物边界。 |
