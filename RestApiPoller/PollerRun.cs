using System.Diagnostics;
using MsSentinel.ObservabilityDemo.DataCollectionRule;

namespace MsSentinel.ObservabilityDemo.RestApiPoller;

public abstract class PollerRun
{
    protected readonly ActivitySource ActivitySource;
    protected readonly string Name;
    protected readonly DateTimeOffset QueryWindowStartTime ;
    protected readonly DateTimeOffset QueryWindowEndTime = DateTimeOffset.UtcNow;

    protected PollerRun(ActivitySource activitySource, string name)
    {
        Name = name;
        ActivitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
        QueryWindowEndTime = DateTimeOffset.UtcNow;
        QueryWindowStartTime = QueryWindowEndTime - TimeSpan.FromMinutes(5);
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        using (var activity = ActivitySource.StartActivity($"Run {Name}", ActivityKind.Consumer))
        {
            activity?.SetTag("RestApiPoller.Name", Name);
            activity?.SetTag("RestApiPoller.QueryWindowStartTime", QueryWindowStartTime);
            activity?.SetTag("RestApiPoller.QueryWindowEndTime", QueryWindowEndTime);

            await AuthenticateAsync(stoppingToken);

            var done = false;
            while (!done && !stoppingToken.IsCancellationRequested)
            {
                done = await PageAsync(stoppingToken);
            }
        }
    }

    public abstract Task<bool> PageAsync(CancellationToken stoppingToken);

    protected void SetTag(string key, object value)
    {
        Activity.Current?.SetTag(key, value);
    }

    protected async Task AuthenticateAsync(CancellationToken stoppingToken)
    {
        using (var activity = ActivitySource.StartActivity("Auth", ActivityKind.Consumer))
        {
            using (var activity_2 = ActivitySource.StartActivity("Request", ActivityKind.Consumer))
            {
                // Simulate authentication delay
                await Task.Delay(TimeSpan.FromSeconds(0.1), stoppingToken);
            }
        }
    }
}

public class GetUpdatedActivitiesRun(
    MockApi.MockApiClient client,
    DcrApiClient dataCollectionRuleClient,
    ActivitySource activitySource
)
    : PollerRun(activitySource, "GetUpdatedActivities")
{
    private int PageNumber { get; set; } = 1;
    private int? NextCursor { get; set; }

    public override async Task<bool> PageAsync(CancellationToken stoppingToken)
    {
        bool done = true;
        using (var activity = ActivitySource.StartActivity($"Page {PageNumber}", ActivityKind.Consumer))
        {
            activity?.SetTag("RestApiPoller.PageNumber", PageNumber);

            done = await RequestAsync(stoppingToken);

            ++PageNumber;
        }

        return done;
    }

    public async Task<bool> RequestAsync(CancellationToken stoppingToken)
    {
        MockApi.ActivityResponse? response = null;

        using (var activity = ActivitySource.StartActivity("Request", ActivityKind.Consumer))
        {
            activity?.SetTag("RestApiPoller.Parameter.createdAt__gt", QueryWindowStartTime);
            activity?.SetTag("RestApiPoller.Parameter.createdAt__lt", QueryWindowEndTime);
            activity?.SetTag("RestApiPoller.Parameter.limit", 12);
            activity?.SetTag("RestApiPoller.Parameter.cursor", NextCursor);

            response = await client.SyntheticS1_GetActivitiesAsync(
                "MsSentinel.ObservabilityDemo.RestApiPoller/1.0.0",
                QueryWindowStartTime, QueryWindowEndTime, null, null, 12, NextCursor, stoppingToken);
        }

        if (response == null)
        {
            return true;
        }

        List<string>? ingestData = null;

        using (var activity = ActivitySource.StartActivity("Extract", ActivityKind.Consumer))
        {
            NextCursor = response.Pagination?.NextCursor;
            var Count = response.Data.Count;

            activity?.SetTag("RestApiPoller.NextCursor", NextCursor);
            activity?.SetTag("RestApiPoller.Count", Count);

            ingestData = response.Data
                .Select(activity => activity.Id.ToString())
                .ToList();

            // Simulate request processing
            await Task.Delay(TimeSpan.FromSeconds(0.05));
        }

        if (ingestData == null || ingestData.Count == 0)
        {
            return true; // No more data to process
        }

        // Ingest the data into the Data Collection Rule
        using (var activity = ActivitySource.StartActivity("Ingest", ActivityKind.Consumer))
        {
            activity?.SetTag("RestApiPoller.Count", ingestData.Count);

            await dataCollectionRuleClient.WeatherForecast_PostAsync(ingestData, CancellationToken.None);
            await Task.Delay(TimeSpan.FromSeconds(0.05));
        }        

        return NextCursor == null || NextCursor <= 0;
    }
}