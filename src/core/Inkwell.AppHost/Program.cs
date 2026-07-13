// Copyright (c) ShuaiHua Du. All rights reserved.

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
int webApiPort = GetPort(builder.Configuration["Ports:WebApi"], "Ports:WebApi", 6801);
int pgAdminPort = GetPort(builder.Configuration["Ports:PgAdmin"], "Ports:PgAdmin", 6802);
int sqlServerPort = GetPort(builder.Configuration["Ports:SqlServer"], "Ports:SqlServer", 6803);
int liteLLMPort = GetPort(builder.Configuration["Ports:LiteLLM"], "Ports:LiteLLM", 6804);
int grafanaPort = GetPort(builder.Configuration["Ports:Grafana"], "Ports:Grafana", 6805);
int prometheusPort = GetPort(builder.Configuration["Ports:Prometheus"], "Ports:Prometheus", 6806);
int tempoPort = GetPort(builder.Configuration["Ports:Tempo"], "Ports:Tempo", 6807);
int lokiPort = GetPort(builder.Configuration["Ports:Loki"], "Ports:Loki", 6808);

IResourceBuilder<ContainerResource> tempo = builder
    .AddContainer("tempo", "grafana/tempo", "2.8.2")
    .WithHttpEndpoint(port: tempoPort, targetPort: 3200, name: "http")
    .WithBindMount(
        Path.Combine(builder.AppHostDirectory, "observability/tempo.yaml"),
        "/etc/tempo.yaml",
        isReadOnly: true)
    .WithArgs("-config.file=/etc/tempo.yaml");

IResourceBuilder<ContainerResource> loki = builder
    .AddContainer("loki", "grafana/loki", "3.5.3")
    .WithHttpEndpoint(port: lokiPort, targetPort: 3100, name: "http")
    .WithBindMount(
        Path.Combine(builder.AppHostDirectory, "observability/loki.yaml"),
        "/etc/loki/local-config.yaml",
        isReadOnly: true)
    .WithArgs("-config.file=/etc/loki/local-config.yaml");

IResourceBuilder<ContainerResource> collector = builder
    .AddContainer("otel-collector", "otel/opentelemetry-collector-contrib", "0.132.0")
    .WithEndpoint(targetPort: 4317, name: "otlp-grpc", scheme: "http")
    .WithBindMount(
        Path.Combine(builder.AppHostDirectory, "observability/collector-config.yaml"),
        "/etc/otelcol-contrib/config.yaml",
        isReadOnly: true)
    .WithArgs("--config=/etc/otelcol-contrib/config.yaml")
    .WaitFor(tempo)
    .WaitFor(loki);

IResourceBuilder<ContainerResource> prometheus = builder
    .AddContainer("prometheus", "prom/prometheus", "v3.5.0")
    .WithHttpEndpoint(port: prometheusPort, targetPort: 9090, name: "http")
    .WithBindMount(
        Path.Combine(builder.AppHostDirectory, "observability/prometheus.yaml"),
        "/etc/prometheus/prometheus.yml",
        isReadOnly: true)
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml")
    .WaitFor(collector);

builder.AddContainer("grafana", "grafana/grafana", "12.1.1")
    .WithHttpEndpoint(port: grafanaPort, targetPort: 3000, name: "http")
    .WithBindMount(
        Path.Combine(builder.AppHostDirectory, "observability/grafana-datasources.yaml"),
        "/etc/grafana/provisioning/datasources/datasources.yaml",
        isReadOnly: true)
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
    .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true")
    .WaitFor(prometheus)
    .WaitFor(tempo)
    .WaitFor(loki);

IResourceBuilder<ParameterResource> liteLLMMasterKey = builder.AddParameter("litellm-master-key", secret: true);
IResourceBuilder<ContainerResource> liteLLM = builder
    .AddContainer("litellm", "docker.litellm.ai/berriai/litellm", "latest")
    .WithHttpEndpoint(port: liteLLMPort, targetPort: 4000, name: "http")
    .WithBindMount(
        Path.Combine(builder.AppHostDirectory, "litellm-config.yaml"),
        "/app/config.yaml",
        isReadOnly: true)
    .WithEnvironment("LITELLM_MASTER_KEY", liteLLMMasterKey)
    .WithArgs("--config", "/app/config.yaml");

IResourceBuilder<PostgresServerResource> postgres = builder
    .AddPostgres("postgres")
    .WithImageTag("17")
    .WithPgAdmin(pgAdmin => pgAdmin
        .WithImageTag("9.16")
        .WithHostPort(pgAdminPort));
IResourceBuilder<PostgresDatabaseResource> database = postgres.AddDatabase("postgres-database", "Inkwell");

IResourceBuilder<SqlServerServerResource> sqlServer = builder
    .AddSqlServer("sqlserver", port: sqlServerPort)
    .WithImageTag("2025-latest");
IResourceBuilder<SqlServerDatabaseResource> sqlServerDatabase = sqlServer.AddDatabase("sqlserver-database", "Inkwell");

IResourceBuilder<ProjectResource> postgresMigrator = builder
    .AddProject<Projects.Inkwell_Migrator>("migrator-postgres")
    .WithReference(database)
    .WithEnvironment("ConnectionStrings__Inkwell", database.Resource.ConnectionStringExpression)
    .WithEnvironment("Inkwell__Persistence__Provider", "Postgres")
    .WaitFor(database);

IResourceBuilder<ProjectResource> sqlServerMigrator = builder
    .AddProject<Projects.Inkwell_Migrator>("migrator-sqlserver")
    .WithEnvironment("ConnectionStrings__Inkwell", sqlServerDatabase.Resource.ConnectionStringExpression)
    .WithEnvironment("Inkwell__Persistence__Provider", "SqlServer")
    .WaitFor(sqlServerDatabase);

builder.AddProject<Projects.Inkwell_WebApi>("webapi")
    .WithReference(database)
    .WithEnvironment("ConnectionStrings__Inkwell", database.Resource.ConnectionStringExpression)
    .WithEnvironment("Inkwell__LiteLLM__Endpoint", liteLLM.GetEndpoint("http"))
    .WithEnvironment("Inkwell__LiteLLM__ApiKey", liteLLMMasterKey)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", collector.GetEndpoint("otlp-grpc"))
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint(port: webApiPort, name: "http")
    .WaitFor(liteLLM)
    .WaitFor(collector)
    .WaitForCompletion(postgresMigrator)
    .WaitForCompletion(sqlServerMigrator);

builder.AddProject<Projects.Inkwell_Worker>("worker")
    .WithReference(database)
    .WithEnvironment("ConnectionStrings__Inkwell", database.Resource.ConnectionStringExpression)
    .WithEnvironment("Inkwell__LiteLLM__Endpoint", liteLLM.GetEndpoint("http"))
    .WithEnvironment("Inkwell__LiteLLM__ApiKey", liteLLMMasterKey)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", collector.GetEndpoint("otlp-grpc"))
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
    .WaitFor(liteLLM)
    .WaitFor(collector)
    .WaitForCompletion(postgresMigrator)
    .WaitForCompletion(sqlServerMigrator);

builder.Build().Run();

static int GetPort(string? configuredPort, string key, int defaultPort)
{
    return string.IsNullOrWhiteSpace(configuredPort)
        ? defaultPort
        : int.TryParse(configuredPort, out int parsedPort) && parsedPort is > 0 and <= 65535
            ? parsedPort
            : throw new InvalidOperationException($"{key} must be a valid TCP port between 1 and 65535.");
}