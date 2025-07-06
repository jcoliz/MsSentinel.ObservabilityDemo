using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one")
    .WithHttpEndpoint(16686, targetPort: 16686, name: "jaegerPortal")
    .WithHttpEndpoint(4317, targetPort: 4317, name: "jaegerEndpoint");

var mockApi = builder.AddProject<Projects.MsSentinel_MockApi_WebApi>("MockApi")
    .WaitFor(jaeger)
    .WithServiceDefaults();

builder.AddProject<Projects.MsSentinel_ObservabilityDemo_RestApiPoller>("RestApiPoller")
    .WithReference(mockApi)
    .WaitFor(mockApi)
    .WithServiceDefaults();

await builder.Build().RunAsync();
