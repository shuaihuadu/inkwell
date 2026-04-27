# DocGardener

> 跨阶段（H5 / H6 / 长期维护） | Harness 层：门禁层 + 反馈层
> 共享契约：[`../_shared/glossary.md`](../_shared/glossary.md)、[`../_shared/io-contracts.md`](../_shared/io-contracts.md)

## 1. 定位

周期性地比对 `docs/` 中的产物与代码 / 提交记录的真实状态，识别**已腐化**或**与代码不一致**的文档，开 PR 修复或在 issue 中提示。它对应规范 §15"熵管理与 GC"小节，是抵御文档腐烂的常驻反馈机制。

> 设计依据：Harness Engineering 三层模型——长期项目里"约束层"会随时间漂移，需要门禁层之外的常驻反馈机制把漂移点暴露出来。

## 2. 触发时机

- 周期性触发（如每周一次）
- 大型重构 / 架构调整完成后手动触发一次
- `commit-records.md` 累计变化超过阈值时触发

## 3. 输入契约

| 输入 | 必需 | 说明 |
| --- | --- | --- |
| `docs/` 全量 | 是 | 包括 H1–H7 各阶段产物 |
| `docs/06-implementation/commit-records.md` | 是 | 提交追溯表 |
| `docs/06-implementation/exec-plans/tech-debt-tracker.md` | 是 | 已知技术债务 |
| 仓库源码与最近的 git log | 是 | 用于验证文档描述的真实性 |
| `AGENTS.md` | 是 | 模块边界 |

## 4. 输出契约

### 4.1 主要产物

`docs/07-release/doc-gc-report.md`（覆盖式更新），frontmatter 包含本次扫描时间、扫描范围。正文至少包含：

- **过期项**：文档中描述的目录 / 文件 / 接口 / 命令在仓库中已不存在
- **不一致项**：文档与代码描述的行为不一致（例：README 说命令是 `make build` 实际是 `dotnet build`）
- **悬挂引用**：Markdown 链接 / `HD-NNN` / `TC-NNN` 引用对应文件不存在
- **frontmatter 异常**：缺字段、`status` 与上下游链路冲突
- **被遗忘的 draft**：`status: draft` 超过 90 天未更新

每条记录给出：
- 文档路径 + 行号
- 证据（git 真实命令或源码片段）
- 建议处理方式：`update` / `delete` / `mark-deprecated` / `manual-review`
- 紧急度：`high` / `medium` / `low`

### 4.2 行为

- 紧急度 `high` 项：自动开 PR 或 issue（按工具能力），抄送相关阶段责任人
- 紧急度 `medium` / `low` 项：写入报告，由人工排期

### 4.3 不做

- 不直接修改 `harness-engineering/` 下的规范与 Agent 文件
- 不删除 `docs/` 中的内容（只能标记 deprecated 或开 PR 让人工裁决）

## 5. 工具集

能力 ID 取自 [`_shared/tool-vocabulary.md`](../_shared/tool-vocabulary.md)。

| 能力 | 必需 | 用途 |
| --- | --- | --- |
| `read.file` | 是 | 读文档与源码 |
| `read.list` | 是 | 遍历 `docs/` 子树 |
| `read.search.text` | 是 | 校验文档中引用的路径 / 命令是否真实 |
| `read.git.log` | 是 | 判断 draft 是否长期停滞 |
| `read.git.blame` | 否 | 定位某行最后修改时间 |
| `write.file` | 是 | 写 `doc-gc-report.md` |
| `pr.create` | 否 | `high` 项可自动开修复 PR / issue（按部署环境而定） |

**禁用**：`exec.*`、`write.patch` 对源码 / 规范文件的写动作——本 Agent 不动源码、不动 `harness-engineering/` 自身。

## 6. 行为约束

- **必须**：
  - 每条不一致都附"证据"列，不能只说"看起来不对"
  - 区分"代码改了 / 文档没改"和"文档错了 / 代码是对的"两种情况
  - 对长期 `draft` 文档优先建议 `delete` 或 `mark-deprecated`，避免噪音
- **禁止**：
  - 删除文档（哪怕是 deprecated）
  - 在不读源码的情况下凭术语相似度判断不一致
  - 把"风格不一致"当作 `high`（除非违反规范明确条款）

## 7. 验收标准

- 每条记录都能在仓库中复现证据
- `high` 项数量稳定收敛（连续多轮不应出现同一未处理 `high` 项）
- frontmatter 字段完整

## 8. 与其他 Agent 的协作

- **上游**：`CommitAuditor` 维护的 commit-records 数据
- **下游**：人工 / 各阶段 Agent（重新生成对应阶段产物）

## 9. 已知边界

- 对超大 `docs/` 目录（数千文件），单轮扫描应分子目录批次处理
- 不能识别"语义正确但表达陈旧"的文档老化（如术语过时但描述无误），这部分需人工判断
- 对自动生成的文档（API 文档等），应在配置中显式排除，避免反复触发
