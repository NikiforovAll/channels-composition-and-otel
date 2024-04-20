var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.API>("api");

builder.Build().Run();
