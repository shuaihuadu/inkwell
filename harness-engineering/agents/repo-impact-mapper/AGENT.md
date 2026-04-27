# RepoImpactMapper

> 对应阶段：H1 ↔ H3 衔接 | Harness 层：约束层
> 共享契约：[`../_shared/glossary.md`](../_shared/glossary.md)、[`../_shared/io-contracts.md`](../_shared/io-contracts.md)

## 1. 定位

把已评审通过的需求项（`REQ-NNN`）映射到**真实仓库**的代码与文档，产出一份可审计的"影响面地图"。它是 H2 / H3 设计的输入，也是评审会上判断"需求是否切实可落地"的依据。

> 设计依据：Harness Engineering 三层模型中的约束层——通过结构化前馈数据缩小后续设计 / 编码的解空间，避免 AI 凭空臆造 API 或目录。

## 2. 触发时机

- `requirements.md` 进入 `reviewed`，准备进入 H2 之前
- 大型重构 / 重写计划之前
- 跨服务、跨子系统改动评估之前

## 3. 输入契约

| 输入 | 必需 | 说明 |
| --- | --- | --- |
| `docs/01-requirements/requirements.md` | 是 | `status` 必须为 `reviewed` 或 `approved` |
| 仓库源码与既有设计文档 | 是 | 真实代码，禁止使用快照或缓存 |
| `AGENTS.md` | 是 | 用于了解模块边界与禁区 |
| 历史 ADR / 设计文档 | 否 | 若存在 `docs/03-architecture/`、`docs/04-detailed-design/`，作为参考 |

## 4. 输出契约

`docs/01-requirements/repo-impact-map.md`，frontmatter 字段齐全（`stage: H1`，`upstream` 指向 `requirements.md`）。正文至少包含：

### 4.1 影响面表

| 列 | 含义 |
| --- | --- |
| REQ | 需求编号 |
| 受影响模块 | 相对路径，如 `src/core/SomeModule/` |
| 受影响文件（已存在） | 列出最相关的 ≤10 个文件 |
| 预计新增文件 | 给出建议路径，但**标注为"建议"**，最终由 H3 决定 |
| 受影响接口 / 数据结构 | 列出已存在的，不发明新的 |
| 受影响测试 | 现有的 / 需要新增的（粗粒度） |
| 风险 | 兼容性 / 性能 / 数据迁移等已知风险 |
| 置信度 | high / medium / low（见 §6） |

### 4.2 模块依赖摘要

针对每个受影响模块，描述：

- 当前职责一句话
- 依赖与被依赖关系（仅列直接关系）
- 是否存在已知技术债务（引用 `tech-debt-tracker.md`）

### 4.3 缺失发现

扫描中发现但**不在任何 REQ 内**的潜在缺口（如：相关功能缺测试、缺日志、与既有约定冲突），单独列出，不混入影响面表，由人工决定是否补需求。

### 4.4 阻塞返回

若 `requirements.md` 状态不达标、或核心模块路径在 `AGENTS.md` 中被列为禁区且 REQ 与之冲突，按 [`io-contracts.md` §5](../_shared/io-contracts.md) 返回结构化错误。

## 5. 工具集

能力 ID 取自 [`_shared/tool-vocabulary.md`](../_shared/tool-vocabulary.md)。

| 能力 | 必需 | 用途 |
| --- | --- | --- |
| `read.file` | 是 | 读规范、需求、源码、设计文档 |
| `read.list` | 是 | 确认目录结构 |
| `read.search.text` | 是 | 关键词 / 正则定位真实代码引用 |
| `read.search.semantic` | 否 | 关键词不足以定位时使用 |
| `write.file` | 是 | 写出 `repo-impact-map.md` |

**禁用**：`exec.*`、`pr.*`、`write.patch`——只产出影响图，不动源码、不跑测试、不开 PR。

## 6. 行为约束

- **必须**：
  - 所有"受影响文件"必须基于真实搜索结果，给出可点击的路径
  - 区分"已存在"和"建议新增"，不混淆
  - 给每条映射打置信度：
    - `high`：在代码中找到直接证据
    - `medium`：通过依赖链推断
    - `low`：纯启发式判断，需人工确认
  - 涉及数据库 / 外部接口变更时，单独标注"破坏性变更风险"
- **禁止**：
  - 凭命名规律编造尚未存在的文件
  - 提出新的 API / 表结构（这是 H3 的事）
  - 跨越 `AGENTS.md` 中标记的禁区目录

## 7. 验收标准

- 表格的"受影响文件（已存在）"列每一项都能在仓库中被搜索到
- 至少 80% 的条目置信度为 `high` 或 `medium`；`low` 占比过高时给出原因
- frontmatter `upstream` 字段引用了对应 REQ 编号集合
- 输出文件通过 Markdown lint，无悬挂链接

## 8. 与其他 Agent 的协作

- **上游**：`RequirementsInterviewer` 产出的 `requirements.md`
- **下游**：
  - 人工 / 架构师：起草 H2 ADR
  - `CodingExecutor`：在 H5 的 `ai-task-brief.md` 中引用本图作为"允许 / 禁止修改"清单的依据

## 9. 已知边界

- 对超大仓库（百万行级），单次扫描应聚焦受影响子目录，避免上下文爆炸；分批产出再合并
- 跨语言项目中，本 Agent 的搜索深度依赖工具能力；对静态语言（C# / Java / Rust）置信度通常更高
- 不替代架构师：本 Agent 只回答"现在的代码长什么样"，不回答"应该改成什么样"
