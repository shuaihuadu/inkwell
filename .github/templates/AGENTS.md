# <项目名>

> AI 协作单一事实源——遵循 [AGENTS.md 跨工具开放约定](https://agents.md/)。
> 本仓库采用 [Harness Engineering](.he/HANDBOOK.md) 作为工程骨架。

## 1. 项目身份

<!--
项目负责人签字位。AI / 安装脚本 / Custom Agent 都不能替写。
一句话讲清：目标用户是谁、核心价值是什么。最多 2 行。

模板示例：
> Inkwell 是给个人技术博客作者用的 AI 内容工厂：
> 输入题目 → AI 多轮迭代磨稿 → 输出可直接发布的 Markdown。

完成签字后下一步：
1. 改 docs/01-requirements/repo-impact-map.md 第 3 节，把 GAP-001 标为 "已关闭（写日期）"，
   同时把第 0.1 节 "AGENTS.md" 行从 "无" 改 "有"。
2. 提交一条 commit 记录这次签字，参考 .github/instructions/commit-format.instructions.md。
3. 第 3 节 "模块边界 / 禁区" 维持 TODO 不动——等 H2 选型完成后再回来填。
-->

> **TODO（项目负责人签字位）**：1 句话项目定位待补。

**当前阶段**：H1 / H2 / H3 / H4 / H5 / H6（手动维护，写当前主战场即可）

## 2. 技术栈

> 自动可探：直接看根目录的 `package.json` / `*.csproj` / `pyproject.toml` / `go.mod` / `Cargo.toml`。
> 这里只列**人需要知道但工具发现不了的事**（如：内部仓库地址、镜像源、私有 SDK 来源）。

- 主要语言 / 框架：[ 待填 ]
- 包管理：[ 待填 ]
- 私有依赖来源：[ 待填，没有就删本行 ]

## 3. 模块边界 / 禁区

<!--
H2 ArchitectAdvisor 完成后由项目负责人回填。在 H2 之前请保持 TODO 状态。

样例（按你项目栈替换路径）：
- src/<App>.Core/ 不得引用 src/<App>.WebApi/
- 编排逻辑只能放在 src/<App>.Hosting.AzureFunctions/
- docs/01-requirements/ 下文档由人工签字，AI 不得改 status / reviewers
- prototypes/ 不进 main 分支
-->

> **TODO（H2 完成后回填）**：模块依赖与禁区规则待补。

## 4. 文档入口

- 操作手册：[`.he/HANDBOOK.md`](.he/HANDBOOK.md)
- 阶段细则（H1–H6）：[`.he/docs/stages/`](.he/docs/stages/)
- 需求 / 设计 / 测试 / 任务 / 评审：`docs/01-requirements/` … `docs/07-reviews/`
- 模板与 Skill：`.github/templates/` 与 `.github/skills/`
- 多语言代码风格：[`.he/docs/instructions-layout.md`](.he/docs/instructions-layout.md)
- Copilot 实施细节：[`.github/copilot-instructions.md`](.github/copilot-instructions.md)

## 5. 给 AI 工具的通用指令

- **修改前先读**：上方第 3 节模块边界、对应任务的 `docs/06-tasks/T-NNN-*.md`、相关详细设计章节
- **代码 / 提交 / 文档约束**：见 [`.github/instructions/`](.github/instructions/)（按文件路径自动加载）
- **签字位**：`status: draft → reviewed`、`reviewers: []`、本文件 §1 / §3 一律由人工填，AI 不替签
- **风格与禁区违反**：阻塞返回，不要尝试绕路
