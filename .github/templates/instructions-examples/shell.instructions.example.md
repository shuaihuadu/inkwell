---
applyTo: 'scripts/**/*.{sh,bash}'
---

<!--
这是一份 Shell（bash / sh / zsh）代码规范的**参考样例**，不是 Harness Engineering 规范的一部分。

使用方式：
1. 把本文件复制到 .github/instructions/shell.instructions.md（去掉 .example 后缀）
2. 按你项目的实际栈裁剪每一节，删除不适用的条目
3. 真正的硬约束应放进 shellcheck + shfmt，让 CI 强制
   instructions 文件只对 AI 生效，不对人/CI 生效——能让 Linter 干的事不要写在这里
4. 完成后由项目负责人评审，与 AGENTS.md 的项目身份签字位同源

Harness 不维护本样例的内容深度；详细风格请查 Google Shell Style Guide、
Bash Hackers Wiki、shellcheck 各错误码说明等权威来源。

注意：PowerShell 脚本（`.ps1`）请另建 `powershell.instructions.example.md`，
本文件只覆盖 POSIX shell / bash / zsh。
-->

# Shell 编码规范

> 与 [`coding-discipline.instructions.md`](./coding-discipline.instructions.md)（流程纪律）叠加生效。
> 风格层面的硬约束以 `shellcheck` + `shfmt` 为准，本文件只补充 Linter 不便强制的部分。

## 1. 命名

- 函数：[ 待填，建议 lower_snake_case ]
- 局部变量：[ 待填，建议 lower_snake_case，必须 `local` 声明 ]
- 全局变量 / 环境变量：[ 待填，建议 UPPER_SNAKE_CASE ]
- 常量：[ 待填，建议 UPPER_SNAKE_CASE 顶部声明 + `readonly` ]
- 文件：[ 待填，建议小写带短横杠如 `install-deps.sh`，不带空格 ]

## 2. 错误处理与健壮性

- **必须**：脚本顶部加 `set -euo pipefail`（bash）或等价 trap，let errors fail loudly
- **必须**：所有变量引用加双引号 `"$var"`，避免分词与 glob 展开
- **必须**：所有外部命令检查返回码；用 `||` / `if !` 显式分支
- 临时文件：用 `mktemp`，不裸 `/tmp/foo`；trap EXIT 清理
- 不裸 `cd`：必须 `cd "$dir" || exit`
- 不解析 `ls`：用 `find -print0 | xargs -0` 或 glob

## 3. 可移植性与脚本结构

- shebang：[ 待填，建议 `#!/usr/bin/env bash`（项目允许 bash）或 `#!/bin/sh`（要求 POSIX 兼容） ]
- 默认 shell：[ 待填，bash 4+ / zsh / dash 选一，决定能用哪些数组语法、`[[ ]]` 等 ]
- 函数返回：用 `return <code>`，标准输出留给调用方解析
- 长选项：CLI 工具优先长选项（`--option`）便于阅读
- ShellCheck disable：必须配注释说明为什么 disable（如 `# shellcheck disable=SC2086 # 故意不加引号 让通配符展开`）

## 4. 测试

- 测试框架：[ 待填，bats-core / shunit2 / 自写 + diff 选一 ]
- 测试目录：[ 待填，建议 `tests/` 与脚本同级 ]
- 集成测试：用 `mktemp -d` 沙箱目录跑端到端，trap 清理
- CI 必跑：`shellcheck scripts/**/*.sh` + `shfmt -d scripts/`

## 5. 不在此文件强制

下列项交给自动化工具强制，不写在 instructions 里：

- 缩进 / 引号风格 → `shfmt -i 4 -ci`
- 常见错误（未引用变量 / 错用 backticks / 未检查命令返回码）→ `shellcheck`
- 兼容性：`shellcheck -s bash` / `-s sh` / `-s dash`
- POSIX 严格模式 → `shellcheck -o all`（按需开）
