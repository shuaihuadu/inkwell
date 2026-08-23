# Inkwell

English | [简体中文](README.zh-CN.md)

> [!WARNING]
> Inkwell is under active development and has not reached a stable release. Features, configuration, database schemas, and public APIs may introduce breaking changes. Do not treat the `main` branch as a stable compatibility baseline.

Inkwell is an agent workspace built on the [Microsoft Agent Framework](https://github.com/microsoft/agent-framework). Teams can deploy it for internal collaboration, while individuals and OPCs (One Person Companies) can self-host it for personal use.

## Features

- **Agent authoring**: Configure identity, instructions, model parameters, tools, and skills, with draft saving and trial runs.
- **Versioning and collaboration**: Publish versions, compare history, roll back to an earlier version, share agents with a team, or clone them as independent agents.
- **Persistent conversations**: Create, switch, clear, and delete conversation histories while keeping each conversation bound to its agent version.
- **Resilient chat runs**: Stop active runs, recover from errors, resume after screen lock, and inspect tool and skill activity alongside responses.
- **Model access**: Discover and manage LiteLLM models, select chat and embedding models, and test connectivity.
- **Resource management**: Manage agent skills, inspect the read-only tool catalog, and administer user accounts.
- **Local development**: Start the complete environment with .NET Aspire, database migrations, and built-in observability.

## Technology

- **Desktop**: Electron · React · TypeScript
- **Backend**: .NET · ASP.NET Core · Microsoft Agent Framework
- **Data and infrastructure**: EF Core · PostgreSQL / SQL Server · Qdrant · Redis · MinIO / Azure Blob
- **Development and deployment**: .NET Aspire · Docker · Kubernetes · Helm
- **Observability**: OpenTelemetry · Grafana · Prometheus · Tempo · Loki
- **Testing**: MSTest · Vitest · Playwright · Testcontainers

## Local Development

Install the [.NET 10 SDK](global.json), Node.js with npm, and Docker Desktop. Then clone the repository, install the Desktop and visual prototype dependencies from their lockfiles, and start the Aspire AppHost:

```bash
git clone https://github.com/shuaihuadu/inkwell.git
cd inkwell
npm --prefix src/app/desktop ci
npm --prefix prototypes/inkwell-visual-design ci
dotnet run --project src/core/Inkwell.AppHost
```

Aspire starts the databases, migrations, LiteLLM, observability services, WebApi, Desktop, and visual prototype. The default local LiteLLM master key is `sk-local`. To override it without modifying tracked configuration, use AppHost User Secrets before starting Aspire:

```bash
dotnet user-secrets --project src/core/Inkwell.AppHost set "Parameters:litellm-master-key" "<local-litellm-key>"
```

Open the LiteLLM Portal at <http://localhost:6804/ui> and sign in with username `admin` and the LiteLLM master key. You can add models and provider credentials in the Portal. Inkwell discovers those models automatically. The AppHost can also generate LiteLLM bootstrap configuration from `LiteLLM:BootstrapModels` in `src/core/Inkwell.AppHost/appsettings.json`; the sample models are disabled by default.

The default Inkwell administrator credentials are `admin` / `admin`. You must change the password after the first sign-in.

Common local endpoints:

- Aspire Dashboard: <https://localhost:15888>
- Visual prototype: <http://localhost:6800>
- WebApi: <http://localhost:6801>
- LiteLLM Portal: <http://localhost:6804/ui>
- Grafana: <http://localhost:6805>
- Prometheus: <http://localhost:6806>
- Tempo: <http://localhost:6807>
- Loki: <http://localhost:6808>

Port configuration is available in `src/core/Inkwell.AppHost/appsettings.json`.

## Desktop Releases

The current Desktop version is `0.0.1-alpha`. The `alpha` label means features, configuration, database schemas, and public APIs are not stable and may introduce breaking changes.

Pushing a Git tag that matches the version in `src/app/desktop/package.json`, such as `v0.0.1-alpha`, triggers GitHub Actions to create a prerelease with these packages:

- Windows x64: NSIS `.exe` and MSI `.msi`
- macOS Universal: `.dmg` and `.zip`
- Linux x64: `.AppImage` and `.deb`

The version shown in the About window comes from `package.json`. The build number comes from the GitHub Actions run number and attempt, and the commit identifier comes from the release workflow commit. The packages use a numeric platform build version in the form `0.0.<run_number>.<run_attempt>` to satisfy macOS `CFBundleVersion` and Windows `FileVersion` requirements.

Windows code signing and macOS notarization are not configured yet, so the packages are intended for alpha testing only. The Desktop application also requires access to a running Inkwell WebApi.

## Roadmap

- ✅ Agent authoring, draft saving, and trial runs
- ✅ Publishing, version comparison and rollback, team sharing, and cloning
- ✅ Persistent conversation history, run recovery, and tool activity presentation
- ✅ LiteLLM model discovery, model management, and connectivity tests
- ✅ Agent skill management, read-only tool catalog, and user administration
- ✅ Aspire orchestration, PostgreSQL / SQL Server migrations, and agent telemetry
- 🚧 More comprehensive collaboration governance
- 🚧 Knowledge bases, long-term memory, multimodal input, debugging, and evaluation
- 🚧 External protocol compatibility and production deployment

## Follow the WeChat Official Account

Interested in the engineering behind AI agents, Microsoft Agent Framework, and .NET AI development? Scan the QR code to follow “全栈哥” for project updates, architecture notes, and practical write-ups.

![全栈哥 WeChat official account QR code](src/app/desktop/public/quanzhange.jpg)

## License

[MIT License](LICENSE)
