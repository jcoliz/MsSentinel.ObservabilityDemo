using System.Diagnostics;
using MsSentinel.ObservabilityDemo.DataCollectionRule;
using MsSentinel.ObservabilityDemo.MockApi;
using MsSentinel.ObservabilityDemo.RestApiPoller;

var builder = Host.CreateApplicationBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddHostedService<Worker>();

builder.Services.AddSingleton(
    new ActivitySource("MsSentinel.ObservabilityDemo.RestApiPoller", "1.0.0"));

builder.Services.AddHttpClient<MockApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new("https+http://MockApi");
    });

builder.Services.AddHttpClient<DcrApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new("https+http://DataCollectionRule");
    });

var host = builder.Build();
await host.RunAsync();
