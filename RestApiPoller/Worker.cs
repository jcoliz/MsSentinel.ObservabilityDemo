using System.Diagnostics;
using System.Runtime.CompilerServices;
using Azure.Monitor.Ingestion;
using Microsoft.Extensions.Options;
using MsSentinel.ObservabilityDemo.DataCollectionRule;
using MsSentinel.ObservabilityDemo.DataCollectionRule.Options;

namespace MsSentinel.ObservabilityDemo.RestApiPoller;

public partial class Worker(MockApi.MockApiClient client,
    IEnumerable<LogsIngestionClient> logsIngestionClients,
    IOptions<LogIngestionOptions> logOptions,
    ActivitySource activitySource, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var t0 = Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            try
            {
                var activitiesRun = new GetUpdatedActivitiesRun(client, logsIngestionClients.FirstOrDefault(), logOptions, activitySource);
                var alertsRun = new GetAlertsRun(client, logsIngestionClients.FirstOrDefault(), activitySource);

                var t1 = activitiesRun.RunAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                var t2 = alertsRun.RunAsync(stoppingToken);

                await Task.WhenAll(t1, t2);

                logOk();
            }
            catch (Exception ex)
            {
                logFailed(ex);
            }
 
            await Task.WhenAll(t0);
        }
    }

    [LoggerMessage(1, LogLevel.Information, "{Location}: OK")]
    private partial void logOk([CallerMemberName] string? location = null);

    [LoggerMessage(2, LogLevel.Error, "{Location}: Failed")]
    private partial void logFailed(Exception exception, [CallerMemberName] string? location = null);
}
