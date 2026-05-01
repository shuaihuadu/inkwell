# 工具能力词表

各 Agent 的 `AGENT.md` §工具集 必须从本词表中选用条目。这样落到具体工具时，[`_integrations/`](../_integrations/README.md) 的 `{{TOOL_LIST}}` 占位符可以做一次性映射，避免命名漂移。

> 词表只规定**能力维度**，不绑定具体厂商接口。同一条目在不同 IDE 里可能对应不同实现。

## 1. 词表

| 能力 ID | 含义 | 典型映射 |
| --- | --- | --- |
| `read.file` | 读单个文件 | `read_file` / `View` |
| `read.list` | 列目录 | `list_dir` / `LS` |
| `read.search.text` | 文本 / 正则搜索 | `grep` / `ripgrep` |
| `read.search.semantic` | 语义代码搜索 | `semantic_search` / `Embeddings` |
| `read.git.log` | 查看 git 历史 | `git log` |
| `read.git.diff` | 查看 diff | `git diff` |
| `read.git.blame` | 行级溯源 | `git blame` |
| `read.web` | 取远程网页 | `fetch_url` / `WebFetch` |
| `write.file` | 写 / 覆盖文件 | `write_file` / `apply_patch` |
| `write.patch` | 增量补丁 | `apply_patch` |
| `exec.shell` | 执行 shell 命令 | `run_command` / `Bash` |
| `exec.tests` | 跑测试套件 | `dotnet test` / `pytest` |
| `exec.lint` | 跑代码检查 / 格式化 | `dotnet format` / `prettier` |
| `pr.read` | 读 PR 元数据与 diff | GitHub / GitLab API |
| `pr.comment` | 在 PR 上发评论 | GitHub / GitLab API |
| `pr.create` | 开 PR 或 issue | GitHub / GitLab API |
| `ask.user` | 向人类用户提问 | IDE 交互能力 |

## 2. 选用规则

- **最小授权**：每个 Agent 只声明完成职责所需的最少能力。多余的不写。
- **禁用项显式声明**：在 `AGENT.md` §工具集 末尾用"禁止"列表写出**必须**禁用的能力（如 `RequirementsInterviewer` 应显式禁 `exec.*`）。
- **风险能力默认禁用**：`exec.shell`、`pr.create`、`read.web` 默认禁用，仅在 `AGENT.md` 里有明确职责需要时才启用。

## 3. 增改流程

- 新能力必须能映射到至少一种主流工具的真实接口
- 新增条目走 [`docs/stages.md`](../../docs/stages.md) §6.1 的"契约变更"门槛，需先在 §7 登记
- 不允许把项目特有的工具名（如 `inkwell-cli`）加进来——本词表是规范级共享词表
