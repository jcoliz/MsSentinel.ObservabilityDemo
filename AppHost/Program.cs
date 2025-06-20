var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.MsSentinel_ObservabilityDemo_ApiService>("apiservice")
    .WithEnvironment("Logging__Console__FormatterName","systemd");

builder.AddProject<Projects.MsSentinel_ObservabilityDemo_RestApiPoller>("RestApiPoller")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("Logging__Console__FormatterName","systemd");

#if false
builder.AddProject<Projects.MsSentinel_ObservabilityDemo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);
#endif

builder.Build().Run();
