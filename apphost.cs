#:sdk Aspire.AppHost.Sdk@13.6.5
#:property AspireUseCliBundle=true
#:package Aspire.Hosting.JavaScript@13.6.5

var builder = DistributedApplication.CreateBuilder(args);

var root = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? builder.AppHostDirectory;

var api = builder.AddProject("api", Path.Combine(root, "src", "EventScheduler.Api", "EventScheduler.Api.csproj"))
    .WithHttpHealthCheck("/health");

// Vite proxies /api to the API using the services__api__* variables injected by WithReference.
builder.AddViteApp("web", Path.Combine(root, "src", "web"))
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();
