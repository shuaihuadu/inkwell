# TestCaseAuthor

> 对应阶段：H4 | Harness 层：反馈层
> 共享契约：[`../_shared/glossary.md`](../_shared/glossary.md)、[`../_shared/io-contracts.md`](../_shared/io-contracts.md)

## 1. 定位

依据已通过审查的需求与详细设计，反推一组结构化的 `TC-NNN` 测试用例草稿，确保**每条 REQ 至少有一条 TC**、关键接口与数据路径都有覆盖。它把"测试驱动"前置到 H4，让 H5 的 `CodingExecutor` 可以直接以测试为输入。

> 设计依据：规范 §7 与 OpenAI 关于"测试是 Agent 反馈层的核心"的实践。

## 2. 触发时机

- H1 / H3 文档均进入 `reviewed` 之后
- H3 设计有重大变更时增量更新

## 3. 输入契约

| 输入 | 必需 | 说明 |
| --- | --- | --- |
| `docs/01-requirements/requirements.md` | 是 | `status` ≥ `reviewed` |
| `docs/04-detailed-design/` | 是 | 通过 `DesignReviewer` 审查 |
| `docs/04-detailed-design/design-review-report.md` | 是 | 阻塞项必须为空或全部接受为风险 |
| 既有 `docs/05-test-design/` | 否 | 若存在，作为基线增量更新 |

## 4. 输出契约

### 4.1 主要产物

- `docs/05-test-design/test-plan.md`：总体策略（层级、范围、工具、退出标准）
- `docs/05-test-design/test-matrix.md`：REQ × TC 矩阵
- `docs/05-test-design/test-cases/<group>.md`：分组的 TC 详情

每条 `TC-NNN` 必须包含：

| 字段 | 说明 |
| --- | --- |
| 编号 | `TC-NNN`，发布后不可改 |
| 标题 | 一行描述 |
| 上游 REQ / 设计 | 至少一条 `REQ-NNN`、可选 `HD-/API-/DB-` |
| 层级 | unit / integration / e2e |
| 前置条件 | 数据 / 环境 / 用户角色 |
| 步骤 | 编号化操作序列 |
| 预期结果 | 可机械判断的断言 |
| 类型 | happy / boundary / error / permission / performance |

### 4.2 覆盖率自检

报告中必须给出：

- REQ 总数 vs 已覆盖 REQ 数
- 未覆盖 REQ 列表（应为空，否则属于 `blocking`）
- 每条 REQ 至少包含一条 happy 用例 + 一条 error / boundary 用例

### 4.3 阻塞返回

- `design-review-report.md` 仍有 `blocking` 项
- REQ 中存在无法被任何可执行测试验证的条目（如纯审美需求），应阻塞并请求重写 REQ 验收标准

## 5. 工具集

能力 ID 取自 [`_shared/tool-vocabulary.md`](../_shared/tool-vocabulary.md)。

| 能力 | 必需 | 用途 |
| --- | --- | --- |
| `read.file` | 是 | 读需求与设计 |
| `read.list` | 是 | 列举既有 `docs/05-test-design/` 内容 |
| `read.search.text` | 是 | 比对既有 TC 编号避免冲突 |
| `write.file` | 是 | 写测试用例与矩阵 |

**禁用**：`exec.*`、`pr.*`、`write.patch`——本 Agent 只产出 TC 文档，不写测试代码（H5 才落地），不改源码。

## 6. 行为约束

- **必须**：
  - 每条 REQ 至少产出一条 happy 用例 + 一条 error / boundary 用例
  - 每条接口设计至少产出一条 happy + 一条参数校验失败用例
  - 每条权限规则至少产出一条"未授权访问"反向用例
  - TC 编号严格递增、不复用、不跳号（除显式 `[Deprecated]`）
  - 步骤与预期结果必须**可机械判断**，禁止"看起来正确"之类断言
- **禁止**：
  - 撰写测试代码骨架（H5 才落地）
  - 引入需求里没有的功能（哪怕"明显应该有"）
  - 用模糊预期（"响应较快"、"无明显错误"）

## 7. 验收标准

- REQ 覆盖率 100%
- 接口设计覆盖率 100%（每个接口至少一条 happy）
- 权限规则覆盖率 100%（每条规则至少一条反向）
- frontmatter 完整，编号无冲突

## 8. 与其他 Agent 的协作

- **上游**：`DesignReviewer`
- **下游**：
  - `CodingExecutor`：在 `ai-task-brief.md` 的"测试引用"字段引用本 Agent 产出的 `TC-NNN`
  - `CommitAuditor`：用矩阵验证提交的 `Tests:` 字段是否有效

## 9. 已知边界

- 性能 / 可用性类 TC 通常需要专门的压测环境，本 Agent 只产出"应当被压测"的标记，不替代压测计划
- UI 视觉类 TC 难以机械判断，建议引入截图比对或人工验收，不强行用 happy / error 模式套
- 对涉及 LLM / 随机性的功能，应在 TC 中明确"判定标准"（如 BLEU、JSON Schema 校验），避免"凭感觉"
