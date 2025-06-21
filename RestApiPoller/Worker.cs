using System.Diagnostics;
using System.Runtime.CompilerServices;
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
            var activitiesRun = new GetUpdatedActivitiesRun(client, dataCollectionRuleClient, activitySource);
            var alertsRun = new GetAlertsRun(client, dataCollectionRuleClient, activitySource);

            var t1 = activitiesRun.RunAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            var t2 = alertsRun.RunAsync(stoppingToken);

            await Task.WhenAll(t1, t2);

            logOk();

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    [LoggerMessage(1, LogLevel.Information, "{Location}: OK")]
    private partial void logOk([CallerMemberName] string? location = null);
}
