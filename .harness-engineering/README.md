# `.harness-engineering/`

这个目录是 [Harness Engineering](https://github.com/shuaihuadu/harness-engineering) v0.0.1 的**安装产物保管区**。

## 它装了什么

- [`HANDBOOK.md`](HANDBOOK.md) — 操作手册，10 分钟上手；什么时候切哪个 Agent / 用哪个 Prompt / 怎么改模板。**先读它。**
- [`docs/`](docs/) — Harness Engineering 设计文档（H1-H6 阶段定义、目录布局、技术债务 GC 思路）。看完手册想深读的人才需要看。
- `manifest.json` — 安装清单，记录了 install.ps1 写过的每一个文件 / 目录。`uninstall.ps1` 依赖它做精确反向清理。
- `install.log` — 每次 install / uninstall 追加一行（时间戳、harness commit、文件计数、冲突 / 孤儿数）。审计用。
- `uninstall.ps1` — 一键反向清理脚本。从 `manifest.json` 读、按记录逐个移除。

## 它的角色

**所有 Copilot 真正用到的开箱即用文件，都装在 `.github/` 下，不在这里。** 这个目录是给「人」看的（操作手册、设计文档），以及给「卸载脚本」用的（manifest）。

也就是说——**安装完成之后，你不需要再修改这个目录里的任何东西。** 你的所有定制（修指令、改模板、加 instructions）都应该发生在 `.github/` 下。

## 要不要把它加进版本库？

**两种选择，按团队偏好选**：

### 选择 A：入版本库（默认，推荐多人协作时）

- 团队任何人 `git pull` 后，直接能读 [`HANDBOOK.md`](HANDBOOK.md) 知道怎么用
- `install.log` 进版本库可以变成事实上的「Harness 版本变更记录」
- 体积约 100 KB 量级，开销可忽略

### 选择 B：忽略（在 `.gitignore` 里加 `.harness-engineering/`）

- 仓库更"干净"——主分支看不到这个目录
- 团队其他人需要自己再跑一次安装命令才能用
- 适合：单人项目 / 强洁癖的仓库 / 把 Harness Engineering 视为本地开发工具的项目

如果选 B，把这一行加到仓库根 `.gitignore`：

```gitignore
.harness-engineering/
```

⚠ **不要把 `.github/` 加进 `.gitignore`** ——那会让 Copilot 完全看不到 Agent / Skill / Prompt。

## 卸载

不想再用 Harness Engineering 了？

```powershell
pwsh -File .\.harness-engineering\uninstall.ps1
```

它会按 `manifest.json` 把 install 写过的 `.github/*`、`.harness-engineering/*` 全部清掉，**不动**你自己改过、不在 manifest 里的文件。

## 升级

想换到新版本：

```powershell
# 1. 拉新版 harness-engineering 仓库
git -C <path-to-harness-engineering> pull
# 2. 重新跑 install（会走 diff，让你逐个决定 keep / overwrite）
pwsh -File <path-to-harness-engineering>/install.ps1 -TargetRepo .
```

> 这份 README 由 install.ps1 渲染生成；如要修订其内容，去 Harness Engineering 源仓库改 `agents/_integrations/copilot/vendor-readme.template.md`。
