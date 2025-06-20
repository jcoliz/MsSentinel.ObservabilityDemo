using System.Runtime.CompilerServices;

namespace MsSentinel.ObservabilityDemo.RestApiPoller;

public partial class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logOk();
            await Task.Delay(1000, stoppingToken);
        }
    }

    [LoggerMessage(1, LogLevel.Information, "{Location}: OK")]
    private partial void logOk([CallerMemberName] string? location = null);
}
