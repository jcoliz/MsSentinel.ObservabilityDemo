var builder = DistributedApplication.CreateBuilder(args);

var mockApi = builder.AddProject<Projects.MsSentinel_MockApi_WebApi>("MockApi")
    .WithEnvironment("Logging__Console__FormatterName","systemd");

builder.AddProject<Projects.MsSentinel_ObservabilityDemo_RestApiPoller>("RestApiPoller")
    .WithReference(mockApi)
    .WaitFor(mockApi)
    .WithEnvironment("Logging__Console__FormatterName","systemd");

#if false
builder.AddProject<Projects.MsSentinel_ObservabilityDemo_Web>("WebFrontEnd")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);
#endif

await builder.Build().RunAsync();
