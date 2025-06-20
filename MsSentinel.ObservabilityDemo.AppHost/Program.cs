var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.MsSentinel_ObservabilityDemo_ApiService>("apiservice");

#if false
builder.AddProject<Projects.MsSentinel_ObservabilityDemo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);
#endif

builder.Build().Run();
