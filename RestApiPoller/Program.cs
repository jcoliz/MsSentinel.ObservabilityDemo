using System.Diagnostics;
using MsSentinel.ObservabilityDemo.RestApiPoller;

var builder = Host.CreateApplicationBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton(
    new ActivitySource("MsSentinel.ObservabilityDemo.RestApiPoller", "1.0.0"));

var host = builder.Build();
await host.RunAsync();
