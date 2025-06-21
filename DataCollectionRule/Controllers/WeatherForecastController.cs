using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MsSentinel.ObservabilityDemo.DataCollectionRule.Controllers;

[ApiController]
[Route("[controller]")]
public partial class IngestController(ActivitySource activitySource, ILogger<IngestController> logger) : ControllerBase
{
    [HttpPost(Name = "PostToTable")]
    public async Task Post(string table, string[] strings)
    {
        try
        {
            using (var activity = activitySource.StartActivity("Transform", ActivityKind.Consumer))
            {
                await Task.Delay(TimeSpan.FromSeconds(0.2));
            }

            using (var activity = activitySource.StartActivity("Store", ActivityKind.Consumer))
            {
                activity?.SetTag("DataCollectionRule.Count", strings.Length);
                activity?.SetTag("DataCollectionRule.Table", table);

                await Task.Delay(TimeSpan.FromSeconds(0.3));

                LogIngestedItemsOk(strings.Length, table);
            }
        }
        catch (Exception ex)
        {
            LogIngestedItemsError(ex);
            throw;
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "OK. Ingested {Count} items to {Table}")]
    private partial void LogIngestedItemsOk(int count, string table);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Error while ingesting data")]
    private partial void LogIngestedItemsError(Exception exception);
}
