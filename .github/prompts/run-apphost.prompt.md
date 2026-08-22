---
name: "run-apphost"
description: "停止当前 Inkwell AppHost 实例，并在后台终端中重新启动本地 Aspire AppHost"
agent: "agent"
---

重新启动当前工作区中的 Inkwell Aspire AppHost。请直接执行操作，不要只给出命令或操作说明。

1. 找到同时包含 `Inkwell.slnx` 和 `src/core/Inkwell.AppHost/Inkwell.AppHost.csproj` 的工作区根目录，并将后续终端命令的工作目录切换到该目录。若找不到，立即报告错误并停止。
2. 使用 `ps` 检查当前仓库的 AppHost 实例。只匹配命令行中包含当前仓库绝对路径，且可执行文件位于 `src/core/Inkwell.AppHost/bin/` 下并名为 `Inkwell.AppHost` 的进程；不要使用 `pkill -f Inkwell.AppHost` 之类可能影响其他仓库的宽泛匹配。
3. 如果找到实例，先发送 `SIGTERM`。随后再次检查这些 PID；只对仍存活的原 PID 发送 `SIGKILL`。旧实例完全退出后再继续。没有实例运行时直接进入下一步，不要把“未找到进程”当作错误。
4. 使用终端工具的异步/后台模式，从工作区根目录运行：

   ```bash
   dotnet run --project src/core/Inkwell.AppHost/Inkwell.AppHost.csproj
   ```

   不要使用 F5、调试器、VS Code launch configuration，也不要在 shell 命令中使用 `nohup`、`&` 或 `disown`。AppHost 是长驻进程，启动成功后必须保留该后台终端继续运行。
5. 根据启动输出确认 AppHost 已进入运行状态。若启动失败，报告关键错误；若启动成功，简要报告已重启，并给出 Aspire Dashboard 地址 `https://localhost:15888` 和后台终端标识。