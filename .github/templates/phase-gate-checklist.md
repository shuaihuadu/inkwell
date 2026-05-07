# 阶段门禁检查清单

> 每个阶段末尾的"完成后下一步"小节给出**全部勾完之后**该跑的动作。
> 任何一条仍是 `[ ]` 状态的，按 `agents/_shared/io-contracts.md` 第 5 节"阻塞返回"处理：把缺项登记到 `docs/06-tasks/task-board.md` 第 2 节"等待人工决策"，不要凭印象绕过。

## H1：需求、UI 与交互原型

- [ ] 需求背景清楚
- [ ] 用户角色明确
- [ ] 核心场景完整
- [ ] 功能范围明确
- [ ] 不做范围明确
- [ ] UI 页面清单完整
- [ ] 页面状态完整
- [ ] 异常提示明确
- [ ] 权限边界明确
- [ ] 验收标准可验证
- [ ] 可交互原型已评审
- [ ] 评审记录已保存

> **完成后下一步**：
>
> 1. 把 `docs/01-requirements/requirements.md` / `ui-spec.md` / `user-flow.md` / `acceptance-criteria.md` 四份文档的 frontmatter `status` 改 `draft → reviewed`、`reviewers:` 加一行（参见 `.he/HANDBOOK.md` 1.1 节"签字位回写"）。
> 2. 切到 `H1-RepoImpactMapper` 产出 `docs/01-requirements/repo-impact-map.md`——这是 H2 / H3 的硬性输入，不是可选。
> 3. 切到 `H2-ArchitectAdvisor` 起草 `docs/03-architecture/`。

## H2：技术架构选型

- [ ] 总体架构已说明
- [ ] 前端架构已说明
- [ ] 后端架构已说明
- [ ] 数据库选型已说明
- [ ] 鉴权和权限模型已说明
- [ ] 部署方式已说明
- [ ] 可观测性方案已说明
- [ ] 性能目标已说明
- [ ] 替代方案已比较
- [ ] 技术风险已记录
- [ ] 缓解方案已明确
- [ ] 评审记录已保存

> **完成后下一步**：
>
> 1. `docs/03-architecture/architecture.md` / `tech-selection.md` / `risk-analysis.md` 三份文档的 `status` 改 `draft → reviewed`，`adr/` 下每条 ADR 也按同样规则升级。
> 2. 把 H2 决策反写到根目录 `AGENTS.md` 第 4 节"模块边界 / 禁区"，把跨模块允许 / 禁止规则写清楚——这是 `H3-DesignReviewer` / `H5-CodingExecutor` 的边界依据。
> 3. 找一个或多个最小 feature 切到 H3，人手起草 `docs/04-detailed-design/<feature>/HD-NNN.md`。

## H3：详细设计

- [ ] 数据库表、字段、索引、约束已明确
- [ ] API 请求、响应、错误码已明确
- [ ] 服务、进程、任务已明确
- [ ] 目录职责已明确
- [ ] 程序文件职责已明确
- [ ] 配置文件字段已明确
- [ ] 日志文件用途和格式已明确
- [ ] 进程通信方式已明确
- [ ] 监控指标已明确
- [ ] 部署和回滚已明确
- [ ] 性能边界已明确
- [ ] 安全边界已明确
- [ ] 已识别"禁止修改"的范围（H5 任务卡可直接引用）
- [ ] 关键风险已记录且有缓解方案，开发开工前无遗留阻塞
- [ ] 在现有仓库结构下的落点已确认（参见 RepoImpactMapper 输出）
- [ ] 评审记录已保存

> **完成后下一步**：
>
> 1. `docs/04-detailed-design/<feature>/HD-NNN.md`（以及配套 `database-design.md` / `api-design.md` 等）`status` 改 `draft → reviewed`，`reviewers:` 添一行。
> 2. 切到 `H4-TestCaseAuthor`，从 REQ + HD 反推 `docs/05-test-design/test-cases.md`，每条 `TC-NNN` 必须能机械判定。
> 3. 如果 H3 重写了 `repo-impact-map.md`（v0.2 落具体路径），把根目录 `AGENTS.md` 第 5 节"文档入口"中"仓库影响图"行的链接 / 状态描述同步过来。

## H4：测试用例设计

- [ ] 每个关键程序文件都有测试用例
- [ ] 正常路径已覆盖
- [ ] 异常路径已覆盖
- [ ] 边界条件已覆盖
- [ ] 权限场景已覆盖
- [ ] 数据一致性已覆盖
- [ ] 并发或重试已覆盖
- [ ] Mock 边界已明确
- [ ] 测试通过标准已明确
- [ ] 测试矩阵已建立
- [ ] 评审记录已保存

> **完成后下一步**：
>
> 1. `docs/05-test-design/test-cases.md` / `test-matrix.md` 的 `status` 改 `draft → reviewed`。
> 2. 在 Copilot Chat 输入 `/new-task` 起一张 H5 任务卡（首次运行会自动建 `docs/06-tasks/task-board.md`），任务卡每条字段填法见 `templates/ai-task-brief.md`。
> 3. 任务卡草稿评审通过后，把 `docs/06-tasks/task-board.md` 第 1 节对应行的 `状态` 改成 `ready`，然后切到 `H5-CodingExecutor` 执行。

## H5：AI 编码与自验证

- [ ] 每次任务只有一个明确编码单元
- [ ] AI 输入包含需求、设计和测试用例
- [ ] 代码实现符合详细设计
- [ ] 测试代码已同步完成
- [ ] 改动前已记录基线（测试 / Lint / 构建报告留档，便于事后比对）
- [ ] 验收命令已运行
- [ ] 测试失败已修复
- [ ] 与基线对比：新增失败、警告或违规已修复或显式说明（不接受"历史遗留"作为豁免理由）
- [ ] 未引入未评审功能
- [ ] 未修改禁止修改的文件
- [ ] 提交信息包含设计编号和测试编号
- [ ] 提交记录已保存

> **完成后下一步**：
>
> 1. 切到 `H5-CommitAuditor` 校验 commit message 六字段（`Design / Tests / Verify / Docs / Risk / Task`），不合格回上游补凭证后再提交。
> 2. 提交后把 `docs/06-tasks/task-board.md` 的对应行从第 1 节"在跑任务"迁到第 3 节"已交付任务"，回填 `发布说明` / `追溯矩阵` 两列（暂无 release 时先填 `pending`）。
> 3. 累计若干个已交付任务后，切到 H6 节奏跑发布。

## H6：运行验证与文档回写

- [ ] 系统已成功运行
- [ ] 部署流程已验证
- [ ] 测试报告已生成
- [ ] 实际 API 行为已回写
- [ ] 实际数据库结构已回写
- [ ] 实际配置项已回写
- [ ] 实际监控指标已回写
- [ ] 运维说明已完成
- [ ] 已知问题已记录
- [ ] 发布说明已完成
- [ ] 最终文档已评审

> **完成后下一步**：
>
> 1. 切到 `H6-ReleaseNoteWriter`，从 commit 范围抽取 `docs/08-releases/v<X.Y.Z>.md`，破坏性变更单独成节（必须给迁移指引）。
> 2. 回写 `docs/08-releases/traceability-matrix.md`（REQ ↔ HD ↔ TC ↔ Task ↔ Commit），让本次发布的每条变更都能反向追溯。
> 3. 跑一次 `Hx-DocGardener`（横切，不阻塞发布），把已腐化或与代码偏离的文档标 `status: deprecated`——不要物理删除，保留追溯链。
