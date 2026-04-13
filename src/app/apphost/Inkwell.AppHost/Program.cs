IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ProjectResource> webapi = builder.AddProject<Projects.Inkwell_WebApi>("webapi");

builder.Build().Run();
