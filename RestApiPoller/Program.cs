using System.Diagnostics;
using Azure.Identity;
using Azure.Monitor.Ingestion;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Azure;
using MsSentinel.ObservabilityDemo.DataCollectionRule;
using MsSentinel.ObservabilityDemo.DataCollectionRule.Options;
using MsSentinel.ObservabilityDemo.MockApi;
using MsSentinel.ObservabilityDemo.RestApiPoller;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddTomlFile("config.toml", optional: true, reloadOnChange: true);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.Configure<IdentityOptions>(builder.Configuration.GetSection(IdentityOptions.Section));
builder.Services.Configure<LogIngestionOptions>(builder.Configuration.GetSection(LogIngestionOptions.Section));

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

builder.Services.AddAzureClients(clientBuilder =>
{
    // Add a log ingestion client, using endpoint from configuration
    LogIngestionOptions logOptions = new();
    builder.Configuration.Bind(LogIngestionOptions.Section, logOptions);

    if (logOptions.EndpointUri is null)
    {
        return; // No log ingestion endpoint configured, skip adding the client.
    }

    clientBuilder.AddLogsIngestionClient(logOptions.EndpointUri)
    .ConfigureOptions(options =>
    {
        options.Diagnostics.IsLoggingEnabled = true;
        options.Diagnostics.IsLoggingContentEnabled = true;
        options.Diagnostics.IsDistributedTracingEnabled = true;
    });

    // Add a credential for the log ingestion client, using identity options from configuration
    IdentityOptions identityOptions = new();
    builder.Configuration.Bind(IdentityOptions.Section, identityOptions);

    var credential = new ClientSecretCredential
    (
        tenantId: identityOptions.TenantId.ToString(),
        clientId: identityOptions.AppId.ToString(),
        clientSecret: identityOptions.AppSecret
    );

    clientBuilder.UseCredential(credential);
});

var host = builder.Build();

await host.RunAsync();
