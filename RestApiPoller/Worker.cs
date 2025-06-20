using System.Diagnostics;
using System.Runtime.CompilerServices;
using MsSentinel.ObservabilityDemo.ApiService;
using MsSentinel.ObservabilityDemo.DataCollectionRule;

namespace MsSentinel.ObservabilityDemo.RestApiPoller;

public partial class Worker(MockApi.MockApiClient client,
    DcrApiClient dataCollectionRuleClient,
    ActivitySource activitySource, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var t1 = ProcessSingleRun("GetUpdatedActivities", stoppingToken);
            await Task.Delay(150, stoppingToken);
            var t2 = ProcessSingleRun("GetAlerts", stoppingToken);

            await Task.WhenAll(t1, t2);

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcessSingleRun(string name, CancellationToken stoppingToken)
    {
        using (var activity = activitySource.StartActivity($"Run {name}", ActivityKind.Consumer))
        {
            var now = DateTimeOffset.UtcNow;
            activity?.SetTag("RestApiPoller.Name", name);
            activity?.SetTag("RestApiPoller.QueryWindowStartTime", now - TimeSpan.FromMinutes(5));
            activity?.SetTag("RestApiPoller.QueryWindowEndTime", now);

            using (var activity_1 = activitySource.StartActivity("Auth", ActivityKind.Consumer))
            {
                using (var activity_2 = activitySource.StartActivity("Request", ActivityKind.Consumer))
                {
                    await Task.Delay(TimeSpan.FromSeconds(0.2));
                }
            }

            using (var activity_1 = activitySource.StartActivity("Page 1", ActivityKind.Consumer))
            {
                activity_1?.SetTag("RestApiPoller.PageNumber", 1);

                int? NextCursor = null;
                int? Count = null;

                using (var activity_2 = activitySource.StartActivity("Request", ActivityKind.Consumer))
                {
                    if (name == "GetUpdatedActivities")
                    {
                        activity_2?.SetTag("RestApiPoller.Parameter.createdAt__gt", now - TimeSpan.FromMinutes(5));
                        activity_2?.SetTag("RestApiPoller.Parameter.createdAt__lt", now);
                        activity_2?.SetTag("RestApiPoller.Parameter.limit", 12);

                        var response = await client.SyntheticS1_GetActivitiesAsync(
                            "MsSentinel.ObservabilityDemo.RestApiPoller/1.0.0",
                            null, null, null, null, null, null, stoppingToken);

                        NextCursor = response.Pagination.NextCursor;
                        Count = response.Data.Count;
                    }
                    else if (name == "GetAlerts")
                    {
                        _ = await client.SyntheticS1_GetAlertsAsync(
                            null, null, null, null, null, null, stoppingToken);
                    }
                }
                using (var activity_2 = activitySource.StartActivity("Extract", ActivityKind.Consumer))
                {
                    activity_2?.SetTag("RestApiPoller.NextCursor", NextCursor);
                    activity_2?.SetTag("RestApiPoller.Count", Count);

                    await Task.Delay(TimeSpan.FromSeconds(0.05));
                }
                using (var activity_2 = activitySource.StartActivity("Ingest", ActivityKind.Consumer))
                {
                    await dataCollectionRuleClient.WeatherForecast_PostAsync([], CancellationToken.None);
                    await Task.Delay(TimeSpan.FromSeconds(0.05));
                }
            }

            using (var activity_1 = activitySource.StartActivity("Page 2", ActivityKind.Consumer))
            {
                activity_1?.SetTag("RestApiPoller.PageNumber", 2);

                using (var activity_2 = activitySource.StartActivity("Request", ActivityKind.Consumer))
                {
                    if (name == "GetUpdatedActivities")
                    {
                        activity_2?.SetTag("RestApiPoller.Parameter.createdAt__gt", now - TimeSpan.FromMinutes(5));
                        activity_2?.SetTag("RestApiPoller.Parameter.createdAt__lt", now);
                        activity_2?.SetTag("RestApiPoller.Parameter.limit", 12);
                        activity_2?.SetTag("RestApiPoller.Parameter.cursor", 12);

                        _ = await client.SyntheticS1_GetActivitiesAsync(
                            "MsSentinel.ObservabilityDemo.RestApiPoller/1.0.0",
                            null, null, null, null, null, null, stoppingToken);
                    }
                    else if (name == "GetAlerts")
                    {
                        _ = await client.SyntheticS1_GetAlertsAsync(
                            null, null, null, null, null, null, stoppingToken);
                    }
                }
                using (var activity_2 = activitySource.StartActivity("Extract", ActivityKind.Consumer))
                {
                    await Task.Delay(TimeSpan.FromSeconds(0.05));
                }
                using (var activity_2 = activitySource.StartActivity("Ingest", ActivityKind.Consumer))
                {
                    await dataCollectionRuleClient.WeatherForecast_PostAsync([], CancellationToken.None);
                    await Task.Delay(TimeSpan.FromSeconds(0.05));
                }
            }

            logOk();
        }
    }

    [LoggerMessage(1, LogLevel.Information, "{Location}: OK")]
    private partial void logOk([CallerMemberName] string? location = null);
}
