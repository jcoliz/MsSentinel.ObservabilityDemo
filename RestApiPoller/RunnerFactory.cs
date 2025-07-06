using System.Diagnostics;
using Azure.Monitor.Ingestion;
using Microsoft.Extensions.Options;
using MsSentinel.ObservabilityDemo.DataCollectionRule.Options;

namespace MsSentinel.ObservabilityDemo.RestApiPoller;

public class RunnerFactory(MockApi.MockApiClient client,
    IEnumerable<LogsIngestionClient> logsIngestionClients,
    IOptions<LogIngestionOptions> logOptions,
    ActivitySource activitySource, ILoggerFactory loggerFactory)
{
    public T CreateRunner<T>() where T : PollerRun
    {
        if (typeof(T) == typeof(GetUpdatedActivitiesRun))
        {
            var logger = loggerFactory.CreateLogger<GetUpdatedActivitiesRun>();
            return (T)(object)new GetUpdatedActivitiesRun(client, logsIngestionClients.FirstOrDefault(), logOptions, activitySource, logger);
        }
        else if (typeof(T) == typeof(GetAlertsRun))
        {
            return (T)(object)new GetAlertsRun(client, logsIngestionClients.FirstOrDefault(), activitySource);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported runner type: {typeof(T).Name}");
        }
    }
}