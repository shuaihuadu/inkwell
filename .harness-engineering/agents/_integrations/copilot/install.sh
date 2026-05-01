#!/usr/bin/env bash
# [DEPRECATED] 子目录入口，已合并至仓库根 install.sh。
# 请改用：./install.sh --target-repo <path> --targets copilot ...
# 本脚本所有参数转发到根脚本。

set -euo pipefail

echo "提示：本子目录脚本已弃用，请改用仓库根 ./install.sh（等价并支持多目标）。" >&2

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
exec "$REPO_ROOT/install.sh" --targets copilot "$@"
