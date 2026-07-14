// Copyright (c) ShuaiHua Du. All rights reserved.

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
int prototypePort = GetPort(builder.Configuration["Ports:Prototype"], "Ports:Prototype", 6800);
int webApiPort = GetPort(builder.Configuration["Ports:WebApi"], "Ports:WebApi", 6801);
int pgAdminPort = GetPort(builder.Configuration["Ports:PgAdmin"], "Ports:PgAdmin", 6802);
int sqlServerPort = GetPort(builder.Configuration["Ports:SqlServer"], "Ports:SqlServer", 6803);
int liteLLMPort = GetPort(builder.Configuration["Ports:LiteLLM"], "Ports:LiteLLM", 6804);
int grafanaPort = GetPort(builder.Configuration["Ports:Grafana"], "Ports:Grafana", 6805);
int prometheusPort = GetPort(builder.Configuration["Ports:Prometheus"], "Ports:Prometheus", 6806);
int tempoPort = GetPort(builder.Configuration["Ports:Tempo"], "Ports:Tempo", 6807);
int lokiPort = GetPort(builder.Configuration["Ports:Loki"], "Ports:Loki", 6808);
string otelLgtmImage = ContainerImageConfiguration.GetRequired("AppHost:OtelLgtm:Image");
string otelLgtmTag = ContainerImageConfiguration.GetRequired("AppHost:OtelLgtm:Tag");
string liteLLMImage = ContainerImageConfiguration.GetRequired("AppHost:LiteLLM:Image");
string postgresTag = ContainerImageConfiguration.GetRequired("AppHost:Postgres:Tag");
string pgAdminTag = ContainerImageConfiguration.GetRequired("AppHost:PgAdmin:Tag");
string sqlServerTag = ContainerImageConfiguration.GetRequired("AppHost:SqlServer:Tag");

string visualDesignDirectory = Path.GetFullPath(
    Path.Combine(builder.AppHostDirectory, "../../../prototypes/inkwell-visual-design"));
string desktopDirectory = Path.GetFullPath(
    Path.Combine(builder.AppHostDirectory, "../../../src/app/desktop"));

builder.AddViteApp("visual-design", visualDesignDirectory)
    .WithEndpoint("http", endpoint => endpoint.Port = prototypePort)
    .WithExternalHttpEndpoints();

IResourceBuilder<ContainerResource> observability = builder
    .AddContainer("otel-lgtm", otelLgtmImage, otelLgtmTag)
    .WithEndpoint(targetPort: 4317, name: "otlp-grpc", scheme: "http")
    .WithHttpEndpoint(port: grafanaPort, targetPort: 3000, name: "grafana")
    .WithHttpEndpoint(port: prometheusPort, targetPort: 9090, name: "prometheus")
    .WithHttpEndpoint(port: tempoPort, targetPort: 3200, name: "tempo")
    .WithHttpEndpoint(port: lokiPort, targetPort: 3100, name: "loki")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
    .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true");

IResourceBuilder<ParameterResource> liteLLMMasterKey = builder.AddParameter("litellm-master-key", secret: true);
IResourceBuilder<ParameterResource> seedAdminPassword = builder.AddParameter(
    "seed-admin-password",
    () => builder.Configuration["Inkwell:Persistence:Seed:AdminPassword"] ?? "admin",
    secret: true);
IResourceBuilder<ContainerResource> liteLLM = builder
    .AddContainer("litellm", liteLLMImage)
    .WithHttpEndpoint(port: liteLLMPort, targetPort: 4000, name: "http")
    .WithBindMount(
        Path.Combine(builder.AppHostDirectory, "litellm-config.yaml"),
        "/app/config.yaml",
        isReadOnly: true)
    .WithEnvironment("LITELLM_MASTER_KEY", liteLLMMasterKey)
    .WithArgs("--config", "/app/config.yaml");

IResourceBuilder<PostgresServerResource> postgres = builder
    .AddPostgres("postgres")
    .WithImageTag(postgresTag)
    .WithPgAdmin(pgAdmin => pgAdmin
        .WithImageTag(pgAdminTag)
        .WithHostPort(pgAdminPort));
IResourceBuilder<PostgresDatabaseResource> database = postgres.AddDatabase("postgres-database", "Inkwell");

IResourceBuilder<SqlServerServerResource> sqlServer = builder
    .AddSqlServer("sqlserver", port: sqlServerPort)
    .WithImageTag(sqlServerTag);
IResourceBuilder<SqlServerDatabaseResource> sqlServerDatabase = sqlServer.AddDatabase("sqlserver-database", "Inkwell");

IResourceBuilder<ProjectResource> postgresMigrator = builder
    .AddProject<Projects.Inkwell_Migrator>("migrator-postgres")
    .WithReference(database)
    .WithEnvironment("ConnectionStrings__Inkwell", database.Resource.ConnectionStringExpression)
    .WithEnvironment("Inkwell__Persistence__Provider", "Postgres")
    .WithEnvironment("Inkwell__Persistence__Seed__AdminPassword", seedAdminPassword)
    .WaitFor(database);

IResourceBuilder<ProjectResource> sqlServerMigrator = builder
    .AddProject<Projects.Inkwell_Migrator>("migrator-sqlserver")
    .WithEnvironment("ConnectionStrings__Inkwell", sqlServerDatabase.Resource.ConnectionStringExpression)
    .WithEnvironment("Inkwell__Persistence__Provider", "SqlServer")
    .WithEnvironment("Inkwell__Persistence__Seed__AdminPassword", seedAdminPassword)
    .WaitFor(sqlServerDatabase);

IResourceBuilder<ProjectResource> webApi = builder
    .AddProject<Projects.Inkwell_WebApi>("webapi")
    .WithReference(database)
    .WithEnvironment("ConnectionStrings__Inkwell", database.Resource.ConnectionStringExpression)
    .WithEnvironment("Inkwell__LiteLLM__Endpoint", liteLLM.GetEndpoint("http"))
    .WithEnvironment("Inkwell__LiteLLM__ApiKey", liteLLMMasterKey)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", observability.GetEndpoint("otlp-grpc"))
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpEndpoint(port: webApiPort, name: "http")
    .WaitFor(liteLLM)
    .WaitFor(observability)
    .WaitForCompletion(postgresMigrator)
    .WaitForCompletion(sqlServerMigrator);

builder.AddJavaScriptApp("desktop", desktopDirectory)
    .WithRunScript("dev")
    .WithEnvironment("INKWELL_WEBAPI_URL", webApi.GetEndpoint("http"))
    .WaitFor(webApi);

builder.AddProject<Projects.Inkwell_Worker>("worker")
    .WithReference(database)
    .WithEnvironment("ConnectionStrings__Inkwell", database.Resource.ConnectionStringExpression)
    .WithEnvironment("Inkwell__LiteLLM__Endpoint", liteLLM.GetEndpoint("http"))
    .WithEnvironment("Inkwell__LiteLLM__ApiKey", liteLLMMasterKey)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", observability.GetEndpoint("otlp-grpc"))
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "grpc")
    .WaitFor(liteLLM)
    .WaitFor(observability)
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
