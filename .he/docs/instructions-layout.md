---
title: instructions 分层与多语言代码风格
parent: ../README.md
---

# instructions 分层与多语言代码风格

本文回答两个问题：

1. **多语言项目里**，前端 / 后端 / 数据库 / 脚本各自的代码风格该如何对接 AI 工具？
2. Harness 自带的 `coding-discipline.instructions.md` 与你项目自己加的 `<lang>.instructions.md` 边界在哪？

> 命名提示：本规范早期版本曾用 `coding-style.instructions.md` 命名"流程纪律"载体，
> 试行期间已改为 `coding-discipline.instructions.md`，让 "style" 一词留给项目自己加的语言风格文件。
> 旧文件会在重装时被识别为孤儿，按提示删除即可。

## 1. 分层原则：用 `applyTo` 把规则按路径切开

GitHub Copilot Custom Instructions 的 frontmatter 支持 `applyTo` 字段，接 glob，**只对**命中的路径生效。Harness 推荐如下分层：

```text
.github/instructions/
├── coding-discipline.instructions.md     applyTo: 'src/**'                 ← Harness 装：H5 流程纪律
├── commit-format.instructions.md         applyTo: '**'                     ← Harness 装：六字段提交
├── docs-style.instructions.md            applyTo: '**/*.md'                ← Harness 装：文档风格
│
├── csharp.instructions.md                applyTo: 'src/**/*.cs'            ← 项目自己加（C# 后端）
├── typescript.instructions.md            applyTo: 'web/**/*.{ts,tsx}'      ← 项目自己加（前端）
├── javascript.instructions.md            applyTo: 'web/**/*.{js,jsx,mjs}'  ← 项目自己加
├── python.instructions.md                applyTo: 'src/**/*.py'            ← 项目自己加
├── shell.instructions.md                 applyTo: 'scripts/**/*.{sh,bash}' ← 项目自己加
├── go.instructions.md                    applyTo: '**/*.go'                ← 项目自己加
├── rust.instructions.md                  applyTo: 'src/**/*.rs'            ← 项目自己加
└── java.instructions.md                  applyTo: 'src/**/*.java'          ← 项目自己加
```

Copilot Chat 改 `src/Foo.cs` 时**自动叠加加载**：

- `coding-discipline.instructions.md`（命中 `src/**`）
- `csharp.instructions.md`（命中 `src/**/*.cs`）
- `commit-format.instructions.md`（命中 `**`）

而改 `web/App.tsx` 时只会加载 `typescript.instructions.md`，**不会污染** C# 上下文。这是 Copilot 的原生机制，Harness 只是利用它。

## 2. 边界判定：哪些写 instructions、哪些写 Linter / `.editorconfig`

用一句反问检查每条规则：**"如果删了这一行，工具还会照做吗？"** 如果会，删，让强制层做。

| 规则类型               | 例子                                                       | 该写在                                                                                                          |
| ---------------------- | ---------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| 流程纪律               | "改代码前先找 docs/06-tasks/ 任务"、"跑完 Verify 才算完成" | Harness `coding-discipline.instructions.md`                                                                     |
| 跨语言通用纪律         | 反模式（杂烩会话 / 反复纠错 / 无界探索）                   | Harness `coding-discipline.instructions.md`                                                                     |
| 提交字段               | 六字段提交                                                 | Harness `commit-format.instructions.md`                                                                         |
| 模块边界               | "Core 不得引用 WebApi"                                     | 项目根 `AGENTS.md` §3                                                                                           |
| 命名 / 缩进 / 引号     | "C# 用 PascalCase"、"TS 用单引号"                          | **`.editorconfig` + Linter / Formatter**（Harness 不写、`<lang>.instructions.md` 也只在 Linter 不能强制时才写） |
| 异常 / 异步 / 测试套路 | "async 方法名后缀 Async"、"xUnit 三段式"                   | 项目自己加的 `<lang>.instructions.md`（同时配 Roslyn analyzer 双层兜底）                                        |
| 业务架构               | 分层、依赖注入策略                                         | `docs/03-architecture/` 与 ADR                                                                                  |

> Harness 规范 [`docs/repo-layout.md` §10.1](repo-layout.md#101-agentsmd-的使用约定) 的总规则：
> **如果一条规则可以用 Lint / Hooks / CI 强制执行，就不要只在 markdown 文件里说它**——
> 文档只能"建议"，工具才能"强制"。

## 3. 与 `AGENTS.md` 的关系：指针，不重复

仓库根 `AGENTS.md` 是项目身份卡，**不写**任何代码风格细节，只放一行指针：

```markdown
## 5. 给 AI 工具的通用指令

- 代码 / 提交 / 文档约束：见 `.github/instructions/`（按文件路径自动加载）
```

`AGENTS.md` 的硬约束只有两条：项目身份（§1）与模块边界（§3），其他都是指针。详见 [`templates/AGENTS.template.md`](../templates/AGENTS.template.md)。

## 4. 多语言项目的标准布局示意

```text
project-root/
├── AGENTS.md                              # 项目身份 + 模块边界（人手签字）
├── .editorconfig                          # 跨编辑器格式化（Linter 强制层）
├── .github/
│   ├── copilot-instructions.md            # Copilot 实施细节（Harness 装）
│   ├── instructions/
│   │   ├── coding-discipline.instructions.md   # Harness 装：流程纪律
│   │   ├── commit-format.instructions.md       # Harness 装
│   │   ├── docs-style.instructions.md          # Harness 装
│   │   ├── csharp.instructions.md              # 项目加
│   │   ├── typescript.instructions.md          # 项目加
│   │   └── ...
│   └── templates/
│       └── instructions-examples/         # 参考样例（不会被自动加载）
│           ├── csharp.instructions.example.md
│           ├── typescript.instructions.example.md
│           └── ...
├── src/                                   # 后端源码
├── web/                                   # 前端源码
├── scripts/                               # 脚本
└── db/                                    # SQL / 迁移
```

## 5. 8 种语言入口

Harness 在仓库源头 `templates/instructions-examples/` 维护 8 份语言样例骨架，**仅供参考**；安装时由 [`agents/_integrations/copilot/target.json`](../agents/_integrations/copilot/target.json) 同步到用户仓库的 `.github/templates/instructions-examples/`。复制到 `.github/instructions/<lang>.instructions.md` 后由项目自行裁剪：

- `csharp.instructions.example.md`
- `typescript.instructions.example.md`
- `javascript.instructions.example.md`
- `python.instructions.example.md`
- `shell.instructions.example.md`
- `go.instructions.example.md`
- `rust.instructions.example.md`
- `java.instructions.example.md`

每份统一五段式：命名 / 错误处理 / 并发或异步 / 测试 / 不在此文件强制（指向对应 Linter 配置）。

两条落地路径，任选一条：

### 路径 A（推荐）：调用 `code-style-bootstrapper` Skill 半自动起步

在 Copilot Chat / CLI 里说一句：

```text
给项目加 csharp 代码规范
```

Skill 会探栈（读 `*.csproj` / `package.json` / `pyproject.toml` 等）、问 `applyTo` glob、复制样例、按探到的结果回填 `[ 待填 ]`。**它只生成 markdown 草稿，不替你装 Linter / 改 CI**。详见 [`agents/_skills/code-style-bootstrapper/SKILL.md`](../agents/_skills/code-style-bootstrapper/SKILL.md)。

### 路径 B：纯手工复制

```bash
# 1. 把样例复制到生效目录，去 .example 后缀
cp .github/templates/instructions-examples/csharp.instructions.example.md \
   .github/instructions/csharp.instructions.md

# 2. 编辑文件：填掉 [ 待填 ]、按项目栈裁剪
# 3. 装 Linter（C# 例：Roslyn analyzer + .editorconfig + dotnet format），
#    把能强制的事从 instructions 移到 Linter
# 4. 提交本次变更，commit message 引用 commit-format 规范
```

两条路径的边界一致：Harness 只装 markdown，强制层（`.editorconfig` + analyzer + CI）始终由项目自己接。

## 6. CodingExecutor 的"开工前报家门"约束

`H5-CodingExecutor` 跑代码任务时会在响应开头执行第 0 步：

1. 列出本次将修改 / 新增的文件，按后缀推断语言
2. 检测 `.github/instructions/` 是否有对应 `<lang>.instructions.md`
   - **找到** → 响应开头声明 "本次按 `<lang>.instructions.md` 写 `<lang>` 代码"
   - **找不到** → 弹 picker（无 default、无 recommended）：
     - **A**：调用 [`code-style-bootstrapper`](../agents/_skills/code-style-bootstrapper/SKILL.md) Skill 半自动落地规范（Skill 探栈、写草稿；完后用户手动回 §0 二次报家门）
     - **B**：本次由 Agent 自行组织 `<lang>` 风格（commit message 的 `Risk` 字段必须记一句"未使用项目代码规范，按 Agent 自定 `<lang>` 风格"）
3. 同一对话内同语言只问一次；新语言再次走第 0 步

详见 [`agents/coding-executor/AGENT.md`](../agents/coding-executor/AGENT.md) §0。

## 7. 不在范围内

- 评判某门语言"该不该用 PascalCase / 单引号"——这是项目品味，Harness 不站队
- 提供详尽的语言风格规范——查 Microsoft / Google / PEP / Effective Java / Rust API Guidelines 等权威来源
- 替项目装 Linter / Formatter——Harness 只装"提示 AI 的 markdown"，强制层由项目自己接 Lint / Hooks / CI
- 维护各语言样例文件的内容深度——样例只给骨架，每个项目按栈细化
