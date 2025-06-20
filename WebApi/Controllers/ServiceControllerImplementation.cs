using System.Diagnostics;
using System.Runtime.CompilerServices;
using MsSentinel.MockApi.WebApi.Api;
using MsSentinel.MockApi.WebApi.Application;

namespace MsSentinel.MockApi.WebApi.Controllers;

public partial class ServiceControllerImplementation(FailureModes failureModes, ActivitySource activitySource, ILogger<ServiceControllerImplementation> logger) : ISyntheticS1Controller
{
    private IReadOnlyDictionary<string,IEnumerable<string>> emptyHeaders { get; } = new Dictionary<string,IEnumerable<string>>();

    const int maxRecords = 24;
    const int numRecordsPerPage = 12;

    static readonly string[] _users = [ "Amy", "Bob", "Cat", "Dee", "Flo" ]; 

    static readonly string[] _ips = [ "10.0.0.1", "10.0.0.2", "10.0.0.3" ]; 

    static readonly string[] _rules = [ "Visited-TI-Url", "Opened-TI-hash-file" ];

    static readonly string[] _triggers = [ "https://wut.ru/", "1A2DC56F8", "http://threats.gov/", "5F8AS321" ];

    public Task<SwaggerResponse<ActivityResponse>> GetActivitiesAsync(string? userAgent, DateTimeOffset? createdAt__gt, DateTimeOffset? createdAt__lt, DateTimeOffset? updatedAt__gt, DateTimeOffset? updatedAt__lt, int? limit, int? cursor)
    {
        using var activity = activitySource.StartActivity("GetActivitiesAsync", ActivityKind.Server);

        logUserAgent(userAgent ?? "none");

        var count = limit.HasValue ? Math.Min(limit.Value,numRecordsPerPage) : numRecordsPerPage;
        var first = cursor ?? 0;
        var last = first + count;
        var data = Enumerable
            .Range(0, count)
            .Select(x => new CustomSentinelOneActivities_API() 
            {
                Id = Guid.NewGuid(),
                ThreatId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                AgentUpdatedVersion = "1.0",
                CreatedAt = createdAt__lt ?? DateTimeOffset.UtcNow,
                UpdatedAt = updatedAt__lt,
                Description = updatedAt__lt?.ToString(),
                PrimaryDescription = primaryDescription(first + x),
                SecondaryDescription = secondaryDescription(x),
                Comments = "Synthetic activity",
                Data = $"{{ \"computerName\": \"{Guid.NewGuid()}\", \"userName\": \"{user(x)}\", \"role\": \"{role(x)}\" }}",
                AgentId = _agentVersions[ x % _agentVersions.Length ],
                ActivityType = activityType(first + x)
            })
            .ToList();

        logOkDetails(data.Count, createdAt__gt, createdAt__lt, updatedAt__gt, updatedAt__lt, limit, cursor);

        var response = new ActivityResponse() 
        { 
            Data = data,
            Pagination = last < maxRecords ? new Pagination() { NextCursor = last} : null
        };

        return Task.FromResult(new SwaggerResponse<ActivityResponse>(StatusCodes.Status200OK, emptyHeaders, response));
    }

    private static string pick(string[] from, int x)
    {
        return from[ x % from.Length ];
    }

    private static string role(int x)
    {
        return x % 5 == 0 ? "Admin" : "User";
    }

    private static int activityType(int x)
    {
        return (x * 100.0 / maxRecords) switch
        {
            < 10.0 => 51, // agent uninstalled, to trigger "Agent installed from multiple hosts" rule
            < 15.0 => 3002, // black hash
            >= 75.0 => 3608, // alert
            _ => 27 // Shows up in admin sources
        };
    }

    private static string? secondaryDescription(int x)
    {
        return activityType(x) switch
        {
            3002 => user(x).GetHashCode().ToString("X"),
            _ => null
        };
    }

    private static string primaryDescription(int x)
    {
        string result = $"Address {_ips[ x % _ips.Length ]}";

        if (activityType(x) == 3608)
        {
            result = $"Alert created for {pick(_triggers,x)} from Custom Rule: {pick(_rules,x)} in Group detected on {user(x)}.synthetic.contoso.com. " + result;
        }

        return result;
    }

    private static string user(int x) => pick(_users,x);

    // NOTE: ActivityType == 3608 will be the activitiy we need to see for an alert to show up on the daskboard

    // TODO: Sentinel One - Agent uninstalled from multiple hosts

    static readonly string[] _agentVersions = [ "21.7.4.1043", "21.7.4.5853", "21.10.3.3", "21.12.1.5913", "20.0" ]; 

    public Task<SwaggerResponse<AgentResponse>> GetAgentsAsync(DateTimeOffset? createdAt__gt, DateTimeOffset? createdAt__lt, DateTimeOffset? updatedAt__gt, DateTimeOffset? updatedAt__lt, int? limit, int? cursor)
    {
        using var activity = activitySource.StartActivity("GetAgentsAsync", ActivityKind.Server);

        var total = numRecordsPerPage / 2;
        var count = limit.HasValue ? Math.Min(limit.Value,total) : total;
        var data = Enumerable
            .Range(0, count)
            .Select(x => new CustomSentinelOneAgents_API() 
            {
                Id = Guid.NewGuid(),
                UpdatedAt = updatedAt__lt,
                CreatedAt = createdAt__lt ?? DateTimeOffset.UtcNow,
                IsActive = true,
                AgentVersion = _agentVersions[ x % _agentVersions.Length ],
                ComputerName = Guid.NewGuid().ToString()
            })
            .ToList();

        logOkDetails(data.Count, createdAt__gt, createdAt__lt, updatedAt__gt, updatedAt__lt, limit, cursor);

        return Task.FromResult(new SwaggerResponse<AgentResponse>(StatusCodes.Status200OK, emptyHeaders, new() { Data = data }));
    }

    public Task<SwaggerResponse<AlertResponse>> GetAlertsAsync(DateTimeOffset? createdAt__gt, DateTimeOffset? createdAt__lt, DateTimeOffset? updatedAt__gt, DateTimeOffset? updatedAt__lt, int? limit, int? cursor)
    {
        using var activity = activitySource.StartActivity("GetAlertsAsync", ActivityKind.Server);
        
        if (failureModes.AlertsStatus != StatusCodes.Status200OK)
        {
            logFailureMode(failureModes.AlertsStatus);
            return Task.FromResult(new SwaggerResponse<AlertResponse>(failureModes.AlertsStatus, emptyHeaders, new()));
        }

        logOkDetails(1, createdAt__gt, createdAt__lt, updatedAt__gt, updatedAt__lt, limit, cursor);

        return Task.FromResult
        (
            new SwaggerResponse<AlertResponse>
            (
                StatusCodes.Status200OK, 
                emptyHeaders, 
                new () 
                { 
                    Data = 
                    [ 
                        new() 
                        {
                            AlertInfo = new()
                            {
                                CreatedAt = createdAt__lt ?? DateTimeOffset.UtcNow
                            }
                        }
                    ]                    
                }
            )
        );
    }

    public Task<SwaggerResponse<GroupsResponse>> GetGroupsAsync(DateTimeOffset? createdAt__gt, DateTimeOffset? createdAt__lt, DateTimeOffset? updatedAt__gt, DateTimeOffset? updatedAt__lt, int? limit, int? cursor)
    {
        using var activity = activitySource.StartActivity("GetGroupsAsync", ActivityKind.Server);

        if (failureModes.GroupsStatus != StatusCodes.Status200OK)
        {
            logFailureMode(failureModes.GroupsStatus);
            return Task.FromResult(new SwaggerResponse<GroupsResponse>(failureModes.GroupsStatus, emptyHeaders, new()));
        }

        logOkDetails(1, createdAt__gt, createdAt__lt, updatedAt__gt, updatedAt__lt, limit, cursor);

        return Task.FromResult
        (
            new SwaggerResponse<GroupsResponse>
            (
                StatusCodes.Status200OK, 
                emptyHeaders, 
                new () 
                { 
                    Data = 
                    [ 
                        new() 
                        {
                            Id = Guid.NewGuid(),
                            UpdatedAt = updatedAt__lt,
                            CreatedAt = createdAt__lt ?? DateTimeOffset.UtcNow,
                        }
                    ] 
                }
            )
        );
    }

    public Task<SwaggerResponse<ThreatResponse>> GetThreatsAsync(DateTimeOffset? createdAt__gt, DateTimeOffset? createdAt__lt, DateTimeOffset? updatedAt__gt, DateTimeOffset? updatedAt__lt, int? limit, int? cursor)
    {
        using var activity = activitySource.StartActivity("GetThreatsAsync", ActivityKind.Server);

        if (failureModes.ThreatsStatus != StatusCodes.Status200OK)
        {
            logFailureMode(failureModes.ThreatsStatus);
            return Task.FromResult(new SwaggerResponse<ThreatResponse>(failureModes.ThreatsStatus, emptyHeaders, new()));
        }

        var total = numRecordsPerPage / 4;
        var count = limit.HasValue ? Math.Min(limit.Value,total) : total;
        var data = Enumerable
            .Range(0, count)
            .Select(x => new CustomSentinelOneThreats_API()
            {
                Id = Guid.NewGuid().ToString(),
                Indicators = Guid.NewGuid().ToString()
            })
            .ToList();

        logOkDetails(data.Count, createdAt__gt, createdAt__lt, updatedAt__gt, updatedAt__lt, limit, cursor);

        return Task.FromResult(new SwaggerResponse<ThreatResponse>(StatusCodes.Status200OK, emptyHeaders, new() { Data = data }));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Location}: OK", EventId = 1000)]
    public partial void logOk([CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Location}: OK C={createdAt__gt}-{createdAt__lt} U={updatedAt__gt}-{updatedAt__lt} #={cursor}+{limit} #Results: {length}", EventId = 1001)]
    public partial void logOkDetails(int length, DateTimeOffset? createdAt__gt, DateTimeOffset? createdAt__lt, DateTimeOffset? updatedAt__gt, DateTimeOffset? updatedAt__lt, int? limit, int? cursor, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Location}: User-Agent: {UserAgent}", EventId = 1002)]
    public partial void logUserAgent(string userAgent, [CallerMemberName] string? location = null);
 
    [LoggerMessage(Level = LogLevel.Warning, Message = "{Location}: Failure {Status}", EventId = 1007)]
    public partial void logFailureMode(int status, [CallerMemberName] string? location = null);
}