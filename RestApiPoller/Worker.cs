using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MsSentinel.ObservabilityDemo.RestApiPoller;

public partial class Worker(ActivitySource activitySource, ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = activitySource.StartActivity("Run", ActivityKind.Consumer);
            activity?.SetTag("MsSentinel.ObservabilityDemo.RestApiPoller.Location", nameof(Worker));

            using (var activity_1 = activitySource.StartActivity("Request", ActivityKind.Consumer))
            {
                await Task.Delay(TimeSpan.FromSeconds(0.2), stoppingToken);
                logOk();
            }

            using (var activity_1 = activitySource.StartActivity("Wait", ActivityKind.Consumer))
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    [LoggerMessage(1, LogLevel.Information, "{Location}: OK")]
    private partial void logOk([CallerMemberName] string? location = null);
}
