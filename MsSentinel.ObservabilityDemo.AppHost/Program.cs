var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.MsSentinel_ObservabilityDemo_ApiService>("apiservice");

builder.AddProject<Projects.MsSentinel_ObservabilityDemo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
