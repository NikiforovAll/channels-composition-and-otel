var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.API>("api");
builder.AddProject<Projects.FileProcessing>("file-processor");

builder.Build().Run();
