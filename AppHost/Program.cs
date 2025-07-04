var builder = DistributedApplication.CreateBuilder(args);

var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one")
    .WithHttpEndpoint(16686, targetPort: 16686, name: "jaegerPortal")
    .WithHttpEndpoint(4317, targetPort: 4317, name: "jaegerEndpoint");

var apiService = builder.AddProject<Projects.MsSentinel_ObservabilityDemo_ApiService>("ApiService")
    .WithEnvironment("Logging__Console__FormatterName", "systemd")
    .WaitFor(jaeger);

var dcr = builder.AddProject<Projects.MsSentinel_ObservabilityDemo_DataCollectionRule>("DataCollectionRule")
    .WithEnvironment("Logging__Console__FormatterName","systemd")
    .WaitFor(jaeger);

var mockApi = builder.AddProject<Projects.MsSentinel_MockApi_WebApi>("MockApi")
    .WithEnvironment("Logging__Console__FormatterName","systemd")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
    .WaitFor(jaeger);

builder.AddProject<Projects.MsSentinel_ObservabilityDemo_RestApiPoller>("RestApiPoller")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(mockApi)
    .WaitFor(mockApi)
    .WithReference(dcr)
    .WaitFor(dcr)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317")
    .WithEnvironment("Logging__Console__FormatterName","systemd");

#if false
builder.AddProject<Projects.MsSentinel_ObservabilityDemo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);
#endif

// Add Jaeger container for distributed tracing
#if false
builder.AddContainer("jaeger", "jaegertracing/all-in-one:1.53")
    .WithEndpoint(port: 16686, name: "jaeger-ui", targetPort:16686, isExternal: true)
    .WithEndpoint(port: 6831, name: "jaeger-udp", targetPort:6831)
    .WithEnvironment("COLLECTOR_ZIPKIN_HOST_PORT", "9411");
#endif

await builder.Build().RunAsync();
