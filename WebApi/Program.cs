using MsSentinel.MockApi.WebApi;
using MsSentinel.MockApi.WebApi.Api;
using MsSentinel.MockApi.WebApi.Application;
using MsSentinel.MockApi.WebApi.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options => {
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
    options.JsonSerializerOptions.WriteIndented = true;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
    options.JsonSerializerOptions.Converters.Add(new DateTimeOffsetJsonConverter());
}
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(options =>
{
    options.Title = "Microsoft Sentinel synthetic endpoints";
    options.Description = "Provides synthetic data for testing Microsoft Sentinel connectors.";
});

builder.Services.AddSingleton<ISyntheticS1Controller,ServiceControllerImplementation>();
builder.Services.AddSingleton<ISyntheticDSController,SyntheticDSImplementation>();
builder.Services.AddSingleton<IAdminController,AdminControllerImplementation>();
builder.Services.AddSingleton<FailureModes>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseOpenApi();
app.UseSwaggerUi();

var runningincontainer = Convert.ToBoolean(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") ?? "false");

if (!runningincontainer)
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
