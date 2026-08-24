# Inkwell

[English](README.md) | 简体中文

> [!WARNING]
> Inkwell 当前处于活跃开发阶段，尚未发布稳定版本。功能、配置、数据库结构和公开接口都可能发生 Breaking Change，请勿将 `main` 分支视为稳定兼容基线。

Inkwell 是一个基于 [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) 打造的智能体工作空间。团队可以把它部署给内部成员协作使用，个人及 OPC（One Person Company，单人公司）也可以独立部署给自己使用。

## 核心能力

- **Agent 创作**：配置基础信息、Instructions、模型参数、Tools 与 Skills，支持草稿保存和试运行。
- **版本与协作**：发布 Agent 版本、对比版本历史、回滚到历史版本、共享给团队或复制为独立 Agent。
- **持久化会话**：新建、切换、清空和删除会话历史，并保持每个会话与对应 Agent 版本绑定。
- **可靠的对话运行**：停止运行、错误恢复、锁屏后恢复，并在回复旁查看 Tool 与 Skill 活动。
- **模型接入**：发现和管理 LiteLLM 模型，选择 Chat 与 Embedding 模型并测试连通性。
- **资源管理**：管理 Agent Skills、查看只读工具目录，以及维护用户账号。
- **本地开发**：通过 .NET Aspire 启动完整环境、数据库迁移和内置可观测性服务。

## 技术栈

- **客户端**：Electron · React · TypeScript
- **后端**：.NET · ASP.NET Core · Microsoft Agent Framework
- **数据与基础设施**：EF Core · PostgreSQL / SQL Server · Qdrant · Redis · MinIO / Azure Blob
- **开发与部署**：.NET Aspire · Docker · Kubernetes · Helm
- **可观测性**：OpenTelemetry · Grafana · Prometheus · Tempo · Loki
- **测试**：MSTest · Vitest · Playwright · Testcontainers

## 本地启动

本机需安装 [.NET 10 SDK](global.json)、Node.js（含 npm）和 Docker Desktop。克隆仓库后，按 lockfile 安装 Desktop 与视觉原型依赖，然后启动 Aspire AppHost：

```bash
git clone https://github.com/shuaihuadu/inkwell.git
cd inkwell
npm --prefix src/app/desktop ci
npm --prefix prototypes/inkwell-visual-design ci
dotnet run --project src/core/Inkwell.AppHost
```

Aspire 会启动数据库、Migration、LiteLLM、可观测性服务、WebApi、Desktop 和视觉原型。LiteLLM 本地默认 master key 为 `sk-local`。如需在不修改已跟踪配置的情况下覆盖它，请在启动 Aspire 前写入 AppHost User Secrets：

```bash
dotnet user-secrets --project src/core/Inkwell.AppHost set "Parameters:litellm-master-key" "<local-litellm-key>"
```

打开 LiteLLM Portal（<http://localhost:6804/ui>），使用用户名 `admin` 和 LiteLLM master key 登录，然后添加模型和供应商凭据。Inkwell 会自动发现这些模型。AppHost 也可以根据 `src/core/Inkwell.AppHost/appsettings.json` 中的 `LiteLLM:BootstrapModels` 生成 LiteLLM bootstrap 配置；示例模型默认不启用。

Inkwell 默认管理员账号和密码为 `admin` / `admin`，首次登录后必须修改密码。

常用本地地址：

- Aspire Dashboard：<https://localhost:15888>
- 视觉原型：<http://localhost:6800>
- WebApi：<http://localhost:6801>
- LiteLLM Portal：<http://localhost:6804/ui>
- Grafana：<http://localhost:6805>
- Prometheus：<http://localhost:6806>
- Tempo：<http://localhost:6807>
- Loki：<http://localhost:6808>

端口配置位于 `src/core/Inkwell.AppHost/appsettings.json`。

## Desktop 发布

Desktop 当前基础版本为 `0.0.1-alpha`。`alpha` 表示功能、配置、数据库结构和公开接口尚未稳定，后续版本可能包含 Breaking Change。

每次涉及 Desktop 的提交进入 `main` 后，GitHub Actions 都会自动创建 prerelease。工作流使用基础版本和七位提交号自动创建 Tag，例如 `v0.0.1-alpha.8075619`，并生成以下安装包：

- Windows x64：NSIS `.exe`、MSI `.msi`
- macOS Universal：`.dmg`、`.zip`
- Linux x64：`.AppImage`、`.deb`

修改 Desktop 文件且目标为 `main` 的 Pull Request 会运行相同的三平台打包矩阵，并将安装包保留为工作流 Artifact，但不会创建 Tag 或 GitHub Release。手动运行工作流同样只生成 Artifact。

About 窗口和安装包文件名使用自动生成的完整发布版本；构建号来自 GitHub Actions run number 与 run attempt，提交号来自发布工作流对应的 Git commit。安装包内部使用纯数字平台构建版本 `0.0.<run_number>.<run_attempt>`，满足 macOS `CFBundleVersion` 与 Windows `FileVersion` 的格式要求。

当前安装包尚未接入 Windows 代码签名和 macOS notarization，仅用于 alpha 阶段测试；Desktop 仍需连接可用的 Inkwell WebApi。

## Roadmap

- ✅ Agent 创建、完整配置、草稿保存与试运行
- ✅ Agent 发布、版本对比与回滚、团队共享与复制
- ✅ 持久化会话历史、运行恢复与工具活动展示
- ✅ LiteLLM 模型发现、模型管理与连通性测试
- ✅ Agent Skills 管理、只读工具目录与用户管理
- ✅ Aspire 本地编排、PostgreSQL / SQL Server 双数据库迁移与 Agent 遥测
- 🚧 更完整的协作治理
- 🚧 知识库、长期记忆、多模态、调试与评测
- 🚧 对外协议兼容与生产部署

## 关注公众号

如果你也关注 AI Agent 的工程化落地、Microsoft Agent Framework 与 .NET AI 开发，欢迎扫码关注「全栈哥」。项目进展、架构思考和实践记录会持续分享。

![全栈哥公众号二维码](src/app/desktop/public/quanzhange.jpg)

## 许可证

[MIT License](LICENSE)
