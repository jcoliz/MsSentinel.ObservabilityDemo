var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.MsSentinel_ObservabilityDemo_ApiService>("ApiService")
    .WithEnvironment("Logging__Console__FormatterName","systemd");

var dcr = builder.AddProject<Projects.MsSentinel_ObservabilityDemo_DataCollectionRule>("DataCollectionRule")
    .WithEnvironment("Logging__Console__FormatterName","systemd");

builder.AddProject<Projects.MsSentinel_ObservabilityDemo_RestApiPoller>("RestApiPoller")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(dcr)
    .WaitFor(dcr)
    .WithEnvironment("Logging__Console__FormatterName","systemd");

#if false
builder.AddProject<Projects.MsSentinel_ObservabilityDemo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);
#endif

await builder.Build().RunAsync();
