# GitHub Copilot 集成模板

本目录提供把 [Harness Engineering 配套 Agent](../../README.md) 接入 **GitHub Copilot**（VS Code / GitHub.com / Copilot CLI）的一组**模板**。

> **重要**：本目录下的所有文件**不是**本仓库自身在使用的 Copilot 配置——它们是给采用方复制到自己项目的样板。Copilot 只识别仓库根目录下的 `.github/`，不会自动加载本目录的内容。

## 1. 文件清单与落地位置

| 模板文件                                                                                                       | 复制到采用方仓库的位置                               | 何时启用                   |
| -------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------- | -------------------------- |
| [`install.ps1`](./install.ps1)                                                                                 | 不复制（直接调用）                                   | Windows / 跨平台一键安装   |
| [`install.sh`](./install.sh)                                                                                   | 不复制（直接调用）                                   | Linux / macOS 一键安装     |
| [`copilot-instructions.template.md`](./copilot-instructions.template.md)                                       | `.github/copilot-instructions.md`                    | 所有 Copilot 会话自动加载  |
| [`instructions/commit-format.instructions.template.md`](./instructions/commit-format.instructions.template.md) | `.github/instructions/commit-format.instructions.md` | 按 `applyTo` 自动加载      |
| [`instructions/docs-style.instructions.template.md`](./instructions/docs-style.instructions.template.md)       | `.github/instructions/docs-style.instructions.md`    | 按 `applyTo` 自动加载      |
| [`instructions/coding-style.instructions.template.md`](./instructions/coding-style.instructions.template.md)   | `.github/instructions/coding-style.instructions.md`  | 按 `applyTo` 自动加载      |
| [`chatmodes/commit-auditor.chatmode.template.md`](./chatmodes/commit-auditor.chatmode.template.md)             | `.github/chatmodes/commit-auditor.chatmode.md`       | 用户在 Chat 中**手动**切换 |
| [`chatmodes/design-reviewer.chatmode.template.md`](./chatmodes/design-reviewer.chatmode.template.md)           | `.github/chatmodes/design-reviewer.chatmode.md`      | 用户在 Chat 中**手动**切换 |
| [`chatmodes/test-case-author.chatmode.template.md`](./chatmodes/test-case-author.chatmode.template.md)         | `.github/chatmodes/test-case-author.chatmode.md`     | 用户在 Chat 中**手动**切换 |

> 本仓库根目录下旧版的 [`chatmode.md.template`](./chatmode.md.template) 保留为通用占位，可直接基于它派生其他 5 个 Agent（RequirementsInterviewer、RepoImpactMapper、CodingExecutor、ReleaseNoteWriter、DocGardener）的 chatmode。

## 2. 一键同步（推荐仓库根 install 脚本）

推荐使用**仓库根** [`install.ps1`](../../../install.ps1) / [`install.sh`](../../../install.sh)，它们可同时为多个目标工具（copilot / claude-code / codex）同步配置。本目录下的 [`install.ps1`](./install.ps1) / [`install.sh`](./install.sh) 是**向后兼容的薄 wrapper**，只转发到根脚本。

脚本是**幂等**的：源未变化时再次运行不写任何文件、不发任何提示。源更新（修改 / 新增 / 删除）时，会自动检测并按交互策略处理。

脚本完成三件事（默认全部开启，开箱即用）：

1. **渲染 + 复制**：把 `*.template.md` 替换占位符后落到目标仓库 `.github/` 下
2. **Vendor 规范文档**：默认把 `agents/`、`docs/`、`templates/`、`README.md` 同步到目标仓库 `.harness-engineering/`（与安装清单 manifest.json 合住一个隐藏目录），让 chatmode/instructions 里的链接开箱即可点；render 出的 `{{HARNESS_REPO_REF}}` 默认也指向该路径
3. **冲突 / 孤儿处理**：交互提示用户选择，可被参数覆盖

> 特别提醒：采用方仓库之间不要互相拷贝 `.github/` 目录。每个新项目都要从 harness-engineering 仓库重新跑安装脚本，这样 `{{HARNESS_REPO_REF}}` 才会被渲染成该项目自己的 vendor 路径。

### 2.1 交互式（首次安装、缺什么问什么）

```powershell
./install.ps1 -TargetRepo D:\Path\To\YourRepo
```

```bash
./install.sh --target-repo /path/to/your/repo
```

### 2.2 参数式（CI 友好）

```powershell
./install.ps1 `
    -TargetRepo D:\Github\shuaihuadu\Inkwell `
    -ProjectName Inkwell `
    -ProjectOneLiner '基于 Microsoft Agent Framework 的 AI 内容平台' `
    -PrimaryLanguage 'C#' `
    -TechStack '.NET 10 + ASP.NET Core' `
    -TestCommand 'dotnet test' `
    -LintCommand 'dotnet format --verify-no-changes' `
    -Force -NoDelete
```

> 不传 `-VendorHarnessTo` / `-HarnessRepoRef` 也能跑。vendor 默认落在 `.harness-engineering`（与安装清单合住），`HARNESS_REPO_REF` 默认与之同路径。你只有在想改 vendor 位置或改成远程引用时才需要传。

同时装多个工具（当 claude-code / codex 模板准备就绪后）：

```powershell
./install.ps1 -TargetRepo <your-repo> -Targets copilot,claude-code,codex ...
```

### 2.3 同步语义（幂等 + 冲突 + 孤儿）

| 场景                                                                            | 行为                                                        |
| ------------------------------------------------------------------------------- | ----------------------------------------------------------- |
| 目标文件不存在                                                                  | 写入                                                        |
| 目标文件存在且内容一致                                                          | 静默跳过（`skip ... unchanged`）                            |
| 目标文件存在但内容不一致（**冲突**）                                            | 交互提示 `[O]verwrite / [K]eep / [A]ll-overwrite / a[B]ort` |
| 源文件已删除但目标仍存在（**孤儿**，仅 vendor + chatmodes 的 `all` 模式下检测） | 交互提示 `[D]elete / [K]eep / [A]ll-delete / a[B]ort`       |

### 2.4 选项

| 选项                                                                 | 说明                                                                                                                       |
| -------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| `-VendorHarnessTo <path>` / `--vendor-harness-to <path>`             | 改变 vendor 目录。默认 `.harness-engineering`（与安装清单同目录）                                              |
| `-NoVendor` / `--no-vendor`                                          | 不 vendor。必须配合 `-HarnessRepoRef` 传外部 URL（或自定义路径），否则交互询问                                             |
| `-HarnessRepoRef <path-or-url>` / `--harness-repo-ref <path-or-url>` | 覆写 `{{HARNESS_REPO_REF}}` 替换值。不传时：vendor 模式下与 `-VendorHarnessTo` 一致；`-NoVendor` 下默认为 GitHub 在线链接  |
| `-Chatmodes <list>` / `--chatmodes <list>`                           | 选择安装哪些 chatmode；**默认空集**（不安装任何 chatmode）；填具体 stem（如 `commit-auditor,design-reviewer`）安装指定项；填 `all` 全装并启用 chatmode 孤儿检测 |
| `-Force` / `--force`                                                 | 全自动：所有冲突直接覆盖，所有孤儿直接删除；不弹任何提示                                                                   |
| `-NoDelete` / `--no-delete`                                          | 一律不删除孤儿（即便 `-Force` 也不删）；CI 升级推荐配合此选项                                                              |
| `-DryRun` / `--dry-run`                                              | 只打印动作不写盘                                                                                                           |

### 2.5 升级流程

源仓库（harness-engineering）有更新后，到采用方仓库重新跑同样命令，加 `-Force` / `--force` 自动接受所有变更，或手动逐项确认：

```powershell
# 推荐做法：先 DryRun 看一眼会变什么
./install.ps1 -TargetRepo <your-repo> ... -DryRun

# 觉得 OK 再正式跑
./install.ps1 -TargetRepo <your-repo> ... -Force
# 或不带 -Force 走交互式逐项确认
```

安装结束后用下面的命令确认无残留占位符：

```powershell
Select-String '\{\{' <your-repo>/.github -Recurse
```

```bash
grep -rn '{{' <your-repo>/.github/
```

## 3. 手动复制（兜底）

如果不想用脚本，按 §1 的文件对照表手动 `cp`，然后逐文件替换占位符。落地命令大致如下（占位符 `<your-repo>` 换成你自己的仓库根）：

```text
mkdir -p <your-repo>/.github/instructions <your-repo>/.github/chatmodes
cp copilot-instructions.template.md       <your-repo>/.github/copilot-instructions.md
cp instructions/*.instructions.template.md <your-repo>/.github/instructions/
cp chatmodes/commit-auditor.chatmode.template.md   <your-repo>/.github/chatmodes/commit-auditor.chatmode.md
cp chatmodes/design-reviewer.chatmode.template.md  <your-repo>/.github/chatmodes/design-reviewer.chatmode.md
cp chatmodes/test-case-author.chatmode.template.md <your-repo>/.github/chatmodes/test-case-author.chatmode.md
# 复制后逐文件去掉文件名中的 ".template" 段，并按 §4 的表替换 {{...}} 占位符
```

## 4. 占位符清单

模板内全部使用 `{{...}}` 双花括号占位。脚本会一次性替换；手动复制时按下表逐个处理：

| 占位符                  | 含义               | 示例                                                                                                         |
| ----------------------- | ------------------ | ------------------------------------------------------------------------------------------------------------ |
| `{{PROJECT_NAME}}`      | 项目名（中英不限） | `Inkwell`                                                                                                    |
| `{{PROJECT_ONE_LINER}}` | 项目一句话定位     | `基于 Microsoft Agent Framework 的 AI 内容平台`                                                              |
| `{{PRIMARY_LANGUAGE}}`  | 主语言             | `C#` / `TypeScript`                                                                                          |
| `{{TECH_STACK}}`        | 技术栈             | `.NET 10 + ASP.NET Core` / `Node.js 20 + React 18`                                                           |
| `{{TEST_COMMAND}}`      | 验收测试命令       | `dotnet test`                                                                                                |
| `{{LINT_COMMAND}}`      | 代码风格检查命令   | `dotnet format --verify-no-changes`                                                                          |
| `{{HARNESS_REPO_REF}}`  | 引用本规范的方式   | `.harness-engineering`（已 vendor）或 `https://github.com/<owner>/harness-engineering`（外链） |

> 推荐做法：用脚本的 `-VendorHarnessTo` / `--vendor-harness-to` 把本规范文档复制进采用方仓库（默认 `.harness-engineering/`），这样 Copilot 可以本地解析路径引用。如果不 vendor，引用退化为外链（仍可用，只是 Copilot 无法读取原文）。

## 5. 启用 / 禁用建议

- **指令文件（`*.instructions.md`）**：自动生效，无需用户操作。建议保持开启。
- **chatmode**：默认所有 chatmode 都会出现在 Chat 模式选择器里。如果团队成员不愿被打扰，可在 VS Code 用户级 `settings.json` 中设置：
  ```jsonc
  {
    "chat.modeFilesLocations": {
      ".github/chatmodes": false
    }
  }
  ```
  团队级建议保持开启（默认行为）。

## 6. 不在范围内

- **硬质量门禁**：Copilot code review 是建议性评论，不是 status check。真正的拦截器是 GitHub Actions + Branch protection（采用方自管），不在本目录提供模板。
- **MCP server 配置**：与 Agent 包装无关，由采用方按需在 `.vscode/mcp.json` / `~/.config/copilot/mcp.json` 自管。
- **模型选择 / 计费 / 扩展安装**：采用方自处理。

## 7. 同步策略

当 [Harness Engineering 规范](../../README.md) 或具体 Agent 的 `AGENT.md` / `prompt.md` 更新时：

1. 优先改源（`agents/<name>/AGENT.md` / `prompt.md`），而不是模板
2. 模板只承担"工具特定的包装"职责，不重复源内容
3. 采用方升级时重新跑一次 `install.ps1` / `install.sh`（加 `-Force` / `--force`）即可
