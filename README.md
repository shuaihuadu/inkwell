# Inkwell

Inkwell 是一个基于 [Microsoft Agent Framework](https://github.com/microsoft/agent-framework) 打造的智能体工作空间：团队可以把它部署给内部成员协作使用，个人及 OPC（One Person Company，单人公司）也可以独立部署给自己使用。

Inkwell is an agent workspace built on the [Microsoft Agent Framework](https://github.com/microsoft/agent-framework): teams can deploy it for internal collaborative use, and individuals or OPCs (One Person Companies) can self-host it for personal use.

## 核心能力

- **Agent 创作**：配置基础信息、Instructions、模型参数、Tools 与 Skills，支持草稿保存和试运行
- **发布与协作**：发布 Agent 版本、查看版本历史、共享给团队或复制为独立 Agent
- **Agent 对话**：使用已发布版本持续对话，也可在编辑器中试运行草稿
- **模型接入**：统一管理并选用不同模型
- **Skills 与工具**：管理 Agent Skills，查看可用工具目录
- **账号管理**：管理员维护用户账号，用户可修改自己的密码
- **本地运行**：使用 Aspire 启动完整开发环境，并通过内置指南快速上手

## 技术栈

- **客户端**：Electron · React · TypeScript
- **后端**：.NET · ASP.NET Core · Microsoft Agent Framework
- **数据与基础设施**：EF Core · PostgreSQL / SQL Server · Qdrant · Redis · MinIO / Azure Blob
- **开发与部署**：Aspire · Docker · Kubernetes · Helm
- **可观测性**：OpenTelemetry · Grafana · Prometheus · Tempo · Loki
- **测试**：MSTest · Vitest · Playwright · Testcontainers

## 本地启动

本机需安装 [.NET 10 SDK](global.json)、Node.js（含 npm）和 Docker Desktop。克隆仓库后，先按 lockfile 安装 Desktop 与视觉原型依赖，再使用 AppHost User Secrets 配置 LiteLLM Portal 的本地管理员密钥（必须以 `sk-` 开头），最后启动 Aspire AppHost：

```bash
git clone https://github.com/shuaihuadu/inkwell.git
cd inkwell
npm --prefix src/app/desktop ci
npm --prefix prototypes/inkwell-visual-design ci
dotnet user-secrets --project src/core/Inkwell.AppHost set "Parameters:litellm-master-key" "<local-litellm-key>"
dotnet run --project src/core/Inkwell.AppHost
```

启动后访问 LiteLLM Portal（<http://localhost:6804/ui>），使用用户名 `admin` 和上面配置的管理员密钥登录，然后在 Portal 中添加模型和供应商凭据。Portal 模型保存在独立的 LiteLLM PostgreSQL 数据库中，Inkwell 会自动发现并允许其用于基础对话；未配置能力覆盖的模型默认不声明视觉、工具调用或结构化输出能力。

启动后可通过 Aspire Dashboard 查看和管理各项本地服务。Inkwell 默认管理员账号和密码均为 `admin`，首次登录后必须修改密码。

常用本地地址：

- Aspire Dashboard：<https://localhost:15888>
- 视觉原型设计：<http://localhost:6800>
- WebApi：<http://localhost:6801>
- LiteLLM Portal：<http://localhost:6804/ui>
- Grafana：<http://localhost:6805>
- Prometheus：<http://localhost:6806>
- Tempo：<http://localhost:6807>
- Loki：<http://localhost:6808>

端口配置位于 `src/core/Inkwell.AppHost/appsettings.json`。

## Roadmap

- ✅ Agent 创建、完整配置、草稿保存与试运行
- ✅ Agent 发布、版本历史、团队共享与复制
- ✅ LiteLLM 模型发现、模型管理与基础对话
- ✅ Agent Skills 管理与只读工具目录
- ✅ 用户账号管理与密码修改
- ✅ Aspire 本地编排与 PostgreSQL / SQL Server 双数据库迁移
- 🚧 Agent 版本回滚的桌面操作与更完整的协作治理
- 🚧 知识库、长期记忆、多模态、调试与评测
- 🚧 对外协议兼容与生产部署

## 关注公众号

如果你也关注 AI Agent 的工程化落地、Microsoft Agent Framework 与 .NET AI 开发，欢迎扫码关注「全栈哥」。项目进展、架构思考和实践记录会持续分享。

![全栈哥公众号二维码](src/app/desktop/public/quanzhange.jpg)

## 许可证

[MIT License](LICENSE)
