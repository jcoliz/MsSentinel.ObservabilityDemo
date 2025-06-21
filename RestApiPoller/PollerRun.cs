using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using MsSentinel.ObservabilityDemo.DataCollectionRule;

namespace MsSentinel.ObservabilityDemo.RestApiPoller;

public abstract class PollerRun
{
    protected readonly ActivitySource ActivitySource;
    protected readonly MockApi.MockApiClient MockApiClient;
    protected readonly string Name;
    protected readonly DateTimeOffset QueryWindowStartTime;
    protected readonly DateTimeOffset QueryWindowEndTime = DateTimeOffset.UtcNow;

    protected PollerRun(MockApi.MockApiClient client, ActivitySource activitySource, string name)
    {
        MockApiClient = client ?? throw new ArgumentNullException(nameof(client));
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

    private int PageNumber { get; set; } = 1;
    protected async Task<bool> PageAsync(CancellationToken stoppingToken)
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

    protected abstract Task<bool> RequestAsync(CancellationToken stoppingToken);

    protected void SetTag(string key, object value)
    {
        Activity.Current?.SetTag(key, value);
    }

    protected async Task AuthenticateAsync(CancellationToken stoppingToken)
    {
        using (var activity = ActivitySource.StartActivity("Auth", ActivityKind.Consumer))
        {
            activity?.SetTag("RestApiPoller.Parameter.user", "user1");

            var token = await MockApiClient.SyntheticS1_GetTokenAsync(new() { Username = "user1", Password = "password1" }, stoppingToken);
            if (token != null)
            {
                activity?.SetTag("RestApiPoller.Result.token.size", token.Token.Length);
                foreach(var claim in UnpackJwtToken(token.Token))
                {
                    activity?.SetTag($"RestApiPoller.Result.token.claims.{claim.Key}", claim.Value);
                }
            }
        }
    }
    
    /// <summary>
    /// Decode a JWT and return its claims as a dictionary
    /// </summary>
    /// <param name="token">The JWT to decode</param>
    /// <returns>A dictionary of claims contained in the JWT</returns>
    private static IDictionary<string, string> UnpackJwtToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        return jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);
    }    
}

public class GetUpdatedActivitiesRun(
    MockApi.MockApiClient client,
    DcrApiClient dataCollectionRuleClient,
    ActivitySource activitySource
)
    : PollerRun(client, activitySource, "GetUpdatedActivities")
{
    private int? NextCursor { get; set; }

    protected override async Task<bool> RequestAsync(CancellationToken stoppingToken)
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

            await dataCollectionRuleClient.Ingest_PostAsync("Activities_CL", ingestData, CancellationToken.None);
            await Task.Delay(TimeSpan.FromSeconds(0.05));
        }

        return NextCursor == null || NextCursor <= 0;
    }
}


public class GetAlertsRun(
    MockApi.MockApiClient client,
    DcrApiClient dataCollectionRuleClient,
    ActivitySource activitySource
)
    : PollerRun(client, activitySource, "GetAlerts")
{
    private int? NextCursor { get; set; }

    protected override async Task<bool> RequestAsync(CancellationToken stoppingToken)
    {
        MockApi.AlertResponse? response = null;

        using (var activity = ActivitySource.StartActivity("Request", ActivityKind.Consumer))
        {
            activity?.SetTag("RestApiPoller.Parameter.createdAt__gt", QueryWindowStartTime);
            activity?.SetTag("RestApiPoller.Parameter.createdAt__lt", QueryWindowEndTime);
            activity?.SetTag("RestApiPoller.Parameter.limit", 12);
            activity?.SetTag("RestApiPoller.Parameter.cursor", NextCursor);

            response = await client.SyntheticS1_GetAlertsAsync(
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
                .Select(x => x.AlertInfo.CreatedAt.ToString())
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

            await dataCollectionRuleClient.Ingest_PostAsync("Alerts_CL",ingestData, CancellationToken.None);
            await Task.Delay(TimeSpan.FromSeconds(0.05));
        }        

        return NextCursor == null || NextCursor <= 0;
    }
}