using System.Diagnostics;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using MsSentinel.ObservabilityDemo.DataCollectionRule.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddTomlFile("config.toml", optional: true, reloadOnChange: true);

// Add services to the container.

builder.Services.Configure<IdentityOptions>(builder.Configuration.GetSection(IdentityOptions.Section));
builder.Services.Configure<LogIngestionOptions>(builder.Configuration.GetSection(LogIngestionOptions.Section));

builder.Services.AddControllers();

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddSingleton(
    new ActivitySource("MsSentinel.ObservabilityDemo.DataCollectionRule", "1.0.0"));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(options =>
{
    options.Title = "MsSentinel.ObservabilityDemo.DataCollectionRule";
    options.Description = "Synthetic DCR for the MsSentinel Observability Demo";
});

builder.Services.AddAzureClients(clientBuilder =>
{
    // Add a log ingestion client, using endpoint from configuration
    LogIngestionOptions logOptions = new();
    builder.Configuration.Bind(LogIngestionOptions.Section, logOptions);
    clientBuilder.AddLogsIngestionClient(logOptions.EndpointUri);

    // Add a credential for the log ingestion client, using identity options from configuration
    IdentityOptions identityOptions = new();
    builder.Configuration.Bind(IdentityOptions.Section, identityOptions);
    clientBuilder.AddLogsIngestionClient(logOptions.EndpointUri);

    var credential = new ClientSecretCredential
    (
        tenantId: identityOptions.TenantId.ToString(),
        clientId: identityOptions.AppId.ToString(),
        clientSecret: identityOptions.AppSecret
    );

    clientBuilder.UseCredential(credential);
});

var app = builder.Build();

// Add swagger UI
app.UseOpenApi();
app.UseSwaggerUi();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
