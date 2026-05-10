---
name: code-style-bootstrapper
description: 把 templates/instructions-examples/<lang>.instructions.example.md 半自动落地为项目的 .github/instructions/<lang>.instructions.md。当用户说"给项目加 C# / TypeScript / Python 代码规范"、"配 <lang>.instructions"、"H5 §0 选 A 半自动落规范"、"项目还没装 <lang> 的 instructions"时主动调用。本 Skill 探栈、问 glob、复制样例、按已知信息回填 [ 待填 ]、生成 status:draft 草稿；**只生成 markdown，不替项目装 Linter / Formatter / .editorconfig**——强制层始终由项目自己接 CI。
when_to_use: |
  - 项目首次为某门语言写 instructions（之前 .github/instructions/<lang>.instructions.md 不存在）
  - CodingExecutor §0 报家门时检测到缺 <lang>.instructions.md，picker 选了"半自动落地"
  - 用户给出"给项目加 <lang> 代码规范"、"参考样例改一份"等请求
  - 现有 <lang>.instructions.md 与样例差距过大、用户想从样例重新起步
when_not_to_use: |
  - 用户在写流程纪律 / 提交规范 / 文档风格（这些归 Harness 的 coding-discipline / commit-format / docs-style，本 Skill 不动）
  - 用户在配 .editorconfig / Roslyn analyzer / ESLint / ruff 等 Linter（Harness 边界，让项目自己装）
  - 用户在 H1 / H2 / H3 阶段做需求 / 架构 / 详细设计（用对应 Agent）
  - 仅做 instructions 内容的小修小改（直接改文件即可，不必走 Skill）
---

# Skill: 代码规范启动器（Code Style Bootstrapper）

## 1. 目的与原理

Harness Engineering 把 8 种主流语言的 instructions 骨架放在仓库源头 `templates/instructions-examples/<lang>.instructions.example.md`（装机后同步到用户仓库的 `.github/templates/instructions-examples/`），同时在 `.github/instructions/` 下只装 3 份**流程层**的 instructions（`coding-discipline` / `commit-format` / `docs-style`）。**项目的语言风格由项目自己加**，但"自己加"在落地时常见 4 个痛点：

1. **要不要 cp？cp 到哪？文件名写什么？**——用户经常 cp 完忘了去 `.example`，或 glob 写错让 Copilot 加载不到
2. **样例里那一堆 `[ 待填，建议 PascalCase ]` 占位**——人工挨个填很烦，但其中部分可以从项目栈推断
3. **第 5 节"不在此文件强制"列了一堆 Linter 名字**——用户不知道当前项目里这些 Linter 装了没有
4. **写完了不知道 commit 字段怎么写**——这是 Harness 通用问题，Skill 顺手把出口指清

本 Skill 是**操作型 SOP，不是规则引擎**：负责把"复制 → 改 frontmatter → 填占位 → 删不适用条目 → 提示 Linter 装机方向 → 提示 commit 字段"这 6 步串成一次会话动作，让用户从"知道有这事"到"草稿落盘"中间不掉单。

**严守的边界**（这一段决定 Skill 不偏航）：

- ❌ **不装 Linter / Formatter**——Harness 只装 markdown，强制层（`.editorconfig` + analyzer + CI）由项目自己接
- ❌ **不动样例文件本身**——`templates/instructions-examples/*.example.md` 是只读素材，只读不改
- ❌ **探不到栈就反问，不臆测**——如果项目没有 `*.csproj`，不许凭空写"用 .NET 8"
- ❌ **不替项目把 `[ 待填 ]` 编满**——能从栈推断的填上、注明"已自动推断，请确认"；推断不出的保留 `[ 待填 ]`，不胡编
- ❌ **不替项目负责人签字**——`AGENTS.md` §1 项目身份 / §3 模块边界严禁 AI 代签（与 [`prototype-reviewer/AGENT.md`](../../prototype-reviewer/AGENT.md) 的"AI 不给自己开绿灯"原则一致）

## 2. 输入

接受三种输入：

1. **空请求**：用户说"给项目加代码规范"——Skill 走完整 7 步流程
2. **指定语言**：用户说"加 C# / TypeScript / Python 的"——Skill 跳过第 1 步直接探栈
3. **CodingExecutor §0 转入**：H5-CodingExecutor 在第 0 步检测到缺 `<lang>.instructions.md`、picker 选 A，Skill 接管半自动落地、完成后用户回 §0 二次报家门

## 3. 步骤

### 3.1 反问语言（输入未指定时）

用 picker 询问语言（参考 [`interactive-form-builder/SKILL.md`](../interactive-form-builder/SKILL.md)）：

- **8 个内置选项**：`csharp` / `typescript` / `javascript` / `python` / `shell` / `go` / `rust` / `java`
- **第 9 个选项**："其他（自行命名 + 自定义 glob）"
- **无 default、无 recommended**——避免诱导用户选错

如果用户选"其他"：反问 `<lang>` 短名（用作文件名）+ 内容大纲。Harness 不维护"其他"语言的样例，本 Skill 只能落空骨架，由用户填。

### 3.2 存在性检查（防覆盖）

```text
.github/instructions/<lang>.instructions.md                           ← 目标
.github/templates/instructions-examples/<lang>.instructions.example.md ← 装机后的样例位置
```

- **目标已存在**：阻塞返回，列出已有文件大小 / 修改时间 / 前 5 行；问用户是要"对比改某节"还是"放弃"。**绝不静默覆盖**
- **样例不存在**：阻塞返回，提示用户先手写一份，或回 Harness 上游提交补样例的请求；**绝不胡编样例**

路径备考：Harness 仓库源头是 `templates/instructions-examples/`；装机后被 [`agents/_integrations/copilot/target.json`](../../_integrations/copilot/target.json) 同步到用户仓库的 `.github/templates/instructions-examples/`。如果用户是在 Harness 仓库本地调试（从未装过），读源头 `templates/instructions-examples/` 即可。

### 3.3 探栈

按语言扫真实文件，只提取那些**能回填到样例 `[ 待填 ]` 位置的字段**。其它栈元数据（TargetFramework 、 LangVersion 、 ESM/CJS 等）属于 Linter / `.editorconfig` 领域，不填进 instructions，只用于§3.6 装机提示。

| 语言         | 探栈文件                                                          | 能回填到 `[ 待填 ]` 的字段                                                       |
| ------------ | --------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| `csharp`     | `*.csproj` PackageReference / `Directory.Packages.props`        | 测试框架（xunit / NUnit / MSTest）、Mock（Moq / NSubstitute）                  |
| `typescript` | `package.json` devDependencies                                  | 测试框架（vitest / jest / mocha）、Mock / Stub 库                              |
| `javascript` | 同 `typescript`                                                | 同上                                                                         |
| `python`     | `pyproject.toml` dev-deps / `requirements*.txt`                 | 测试框架（pytest / unittest）、Mock（unittest.mock / pytest-mock）             |
| `shell`      | `scripts/**/*.sh` shebang                                       | 默认 Shell（bash / sh / zsh）                                                |
| `go`         | `go.mod` require / `*_test.go`                                  | 测试框架（标准 testing / testify / ginkgo）                                  |
| `rust`       | `Cargo.toml` dev-dependencies                                   | 测试框架（标准 / proptest / mockall）                                       |
| `java`       | `pom.xml` / `build.gradle` test deps                            | 测试框架（JUnit5 / TestNG）、Mock（Mockito / EasyMock）                       |

**探不到→该字段保留 `[ 待填 ]`，不脱豆**。Linter 担调表（`.editorconfig` / Roslyn / ESLint / ruff / golangci-lint / clippy / Checkstyle 是否已装）另探，仅用于§3.6 生成"已检测到 ✓ / ✗"提示。

### 3.4 反问 `applyTo` glob

样例默认值（如 C# 的 `src/**/*.cs`）不一定贴合所有项目布局。用 picker：

- **A**：保留样例默认（如 `src/**/*.cs`）
- **B**：用扫到的真实路径（如发现源码在 `backend/src/` 就给 `backend/src/**/*.cs`）
- **C**：自定义（用户输入）

发现项目同时有 `src/` 和 `tests/` 等多个根，提示一句"glob 太宽会让 Copilot 在测试代码里也按这套规范写，需要的话用 `**/*.cs` 同时覆盖、或单独建 `tests` 配置"——但**不替用户决定**。

### 3.5 复制 + 替换

按以下顺序生成目标文件内容：

1. 读样例完整内容
2. **删头部 HTML 使用注释**（样例第 5–17 行那一段"这是参考样例 / 复制 / 裁剪 / 装 Linter / 评审"——目标文件不再需要这段元说明）
3. **修 frontmatter `applyTo`** 为 3.4 节确认的 glob
4. **回填 `[ 待填，建议 XXX ]`**：能从 3.3 节探到的填上，注明 `<!-- 自动推断自 <文件>，请确认 -->`；探不到的保留 `[ 待填 ]` 原样
5. **保留 `## 5. 不在此文件强制`** 整段不动——这里是写给**未来读者**看的本项目规则，不在里面插"本 Skill §X"这种跨上下文的侧注。装机状况的检测提示走§3.6 输出、不进目标文件
6. **frontmatter 不加 `status` 字段**——instructions 文件本身没有"评审中"概念（只有 `applyTo`），评审水平由 git diff + commit 评审兼底；项目负责人的签字位在 `AGENTS.md` §1 / §3

### 3.6 输出装 Linter 的"装机命令清单"（**只提示不替装**）

按语言给一段独立的"装 Linter 命令"附录，用户决定是否执行。**Skill 不直接 `run_in_terminal`**——避免把"装包"这种破坏性操作隐式背给用户。

C# 例：

```markdown
## 你需要自己执行的命令（Skill 不替你装）

> 以下命令会**修改项目配置 / 添加依赖**。Skill 只列出，由你确认后执行。

1. `.editorconfig`（如果项目没有）：复制 [Microsoft 官方 .NET .editorconfig 起点](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/code-style-rule-options)
2. Roslyn analyzer：`dotnet add package StyleCop.Analyzers`（不锁定版本号，让 NuGet 取当前稳定版）
3. CI 强制：在 `Directory.Build.props` 加 `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
4. 格式化：`dotnet format --verify-no-changes` 入 CI
```

其它 7 种语言的命令清单格式与 C# 同构（一段 markdown，4–6 条命令），按语言对应的样例文件 §5 "不在此文件强制" 段反推即可——Skill 在生成时直接 inline 输出这段，不依赖外部文件。

### 3.7 收尾提示

向用户输出：

```markdown
## 已落地

- 目标：`.github/instructions/<lang>.instructions.md`
- applyTo：`<glob>`
- 已自动回填：<列字段>
- 仍为 [ 待填 ]：<列字段>

## 下一步（你来做）

1. 打开目标文件，把剩余 `[ 待填 ]` 按项目品味改完
2. 装 Linter（命令见上方"装机清单"，Skill 不替你跑）
3. 提交：参考 `commit-message-formatter` Skill；建议字段：
   - `Design`: 通常无对应 HD（这是基础设施型变动）；可写 `N/A`
   - `Tests`: `N/A`（instructions 文件不进测试）
   - `Verify`: `dotnet format --verify-no-changes` 或对应语言的 lint 命令
   - `Risk`: `none` 或一句"刚装规范，CI 可能会因新规则报旧文件错"
4. 如果是 H5-CodingExecutor §0 picker 选 A 转入：回 CodingExecutor 重跑 §0，让它确认"按 <lang>.instructions.md 写代码"
```

## 4. 失败模式与回退

- **样例不存在**（用户选了"其他"语言或 Harness 没收录）：阻塞返回，建议用户手写一份骨架；提供 [`csharp.instructions.example.md`](../../../templates/instructions-examples/csharp.instructions.example.md) 五段式作为参照
- **探栈结果矛盾**（如 `package.json` 里 `"type": "module"`，但代码大量用 `require()`）：列出矛盾、不替用户判定、`applyTo` 用最保守的 glob
- **目标已存在且非空**：阻塞返回；提供"对比 diff" 给用户，但不自动 merge——instructions 文件冲突解决是项目品味问题
- **用户在 Harness 仓库本地调试**（还没装到任何下游项目）：直接读源头 `templates/instructions-examples/`，跳过装机后路径 `.github/templates/instructions-examples/` 的存在性检查
- **同一对话内重复触发**（用户已经为 C# 跑过一次又问 TypeScript）：完整再跑一遍——每门语言独立流程，不共用上次的探栈结果

## 5. 与其它 Skill / Agent 的边界

- **流程纪律 / 提交规范 / 文档风格**：归 Harness 装的 [`coding-discipline.instructions.md`](../../_integrations/copilot/instructions/) / `commit-format` / `docs-style`，本 Skill 不动
- **校验提交信息**：用 [`commit-message-formatter`](../commit-message-formatter/SKILL.md)；本 Skill 在 §3.7 只给"建议字段"提示，不生成完整 commit message
- **追溯链校验**：用 [`traceability-linker`](../traceability-linker/SKILL.md)；instructions 文件本身通常无 REQ/HD 追溯（基础设施型变动）
- **CodingExecutor §0 报家门**：见 [`agents/coding-executor/AGENT.md` §0](../../coding-executor/AGENT.md)；§0 picker 选 A 转入本 Skill，本 Skill 完成后用户**手动回 §0 重跑**，让 Executor 确认"按 `<lang>.instructions.md` 写代码"
- **`AGENTS.md` §1 项目身份签字**：与本 Skill 完全无关；那是项目负责人手签的事
- **`.editorconfig` / Roslyn analyzer / ESLint 装机**：项目自己装，本 Skill 在 §3.6 只列命令清单不执行

## 6. 输出物示例

> 以 C# 为例。假设项目根有 `src/MyApp.Core/MyApp.Core.csproj`，TargetFramework=net8.0，已有 `.editorconfig`，无 StyleCop。

**Skill 输出**：

```markdown
## 已落地：.github/instructions/csharp.instructions.md

frontmatter applyTo: 'src/**/*.cs'（与样例默认一致，已确认 src/ 存在）

已自动回填（5 项）：
- 公共 API：PascalCase（默认值，建议保留）
- 私有字段：_camelCase（默认值）
- 异步方法：Async 后缀（默认值）
- 测试框架：xUnit  <!-- 自动推断自 src/MyApp.Core.Tests/MyApp.Core.Tests.csproj 引用了 xunit -->
- 目标框架：net8.0  <!-- 自动推断自 MyApp.Core.csproj -->

仍为 [ 待填 ]（4 项，请项目负责人确认）：
- 异常分层（领域 vs 基础设施）的具体边界
- 取消令牌的命名规范（默认 cancellationToken）
- Mock 框架（Moq / NSubstitute）
- 测试方法命名（Method_Scenario_ExpectedBehavior vs Should_XXX_When_XXX）

## 你需要自己执行的命令

> 项目当前已检测到：.editorconfig ✓
> 项目当前未检测到：StyleCop.Analyzers ✗、TreatWarningsAsErrors ✗

1. dotnet add package StyleCop.Analyzers
2. 在 Directory.Build.props 加 <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
3. CI 加 dotnet format --verify-no-changes
```

## 7. 不在范围内

- 评判 PascalCase / camelCase 哪个对——这是项目品味，Skill 只复述样例的"建议"
- 替项目装 Linter / Formatter / 改 CI——见 §1 边界
- 维护各语言样例文件的内容深度——样例由 Harness 上游维护，本 Skill 只复制不修改
- 在 `.gitignore` / `.editorconfig` 等其它配置文件里写规则——超出 instructions 范围，归项目自己
- 替 `AGENTS.md` 第 §3 模块边界做选择——那是 H2 选型完成后人手填的硬约束
