using System.Diagnostics;
using System.Text.Json;
using Azure.Monitor.Ingestion;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MsSentinel.ObservabilityDemo.DataCollectionRule.Options;

namespace MsSentinel.ObservabilityDemo.DataCollectionRule.Controllers;

[ApiController]
[Route("[controller]")]
public partial class IngestController(LogsIngestionClient client, IOptions<LogIngestionOptions> logOptions, ActivitySource activitySource, ILogger<IngestController> logger) : ControllerBase
{
    [HttpPost(Name = "PostToTable")]
    public async Task Post(string table, object[] objects)
    {
        try
        {
            using (var activity = activitySource.StartActivity("Transform", ActivityKind.Consumer))
            {
                await Task.Delay(TimeSpan.FromSeconds(0.2));
            }

            using (var activity = activitySource.StartActivity("Store", ActivityKind.Consumer))
            {
                activity?.SetTag("DataCollectionRule.Count", objects.Length);
                activity?.SetTag("DataCollectionRule.Table", table);

                var olist = objects.ToList();

                var result = await client.UploadAsync
                (
                    ruleId: logOptions.Value.DcrImmutableId,
                    streamName: logOptions.Value.Stream,
                    logs: olist
                );

                activity?.SetTag("DataCollectionRule.Result.Status", result.Status);
                activity?.SetTag("DataCollectionRule.Result.ReasonPhrase", result.ReasonPhrase);
                LogIngestedItemsOk(olist.Count, table);
            }
        }
        catch (Exception ex)
        {
            LogIngestedItemsError(ex);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "OK. Ingested {Count} items to {Table}")]
    private partial void LogIngestedItemsOk(int count, string table);

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "Error while ingesting data")]
    private partial void LogIngestedItemsError(Exception exception);
}
