using System.Runtime.CompilerServices;
using MsSentinel.MockApi.WebApi.Api;
using MsSentinel.MockApi.WebApi.Application;

namespace MsSentinel.MockApi.WebApi.Controllers;

public partial class SyntheticDSImplementation(ILogger<SyntheticDSImplementation> logger) : ISyntheticDSController
{
    private IReadOnlyDictionary<string,IEnumerable<string>> emptyHeaders { get; } = new Dictionary<string,IEnumerable<string>>();

    public Task<SwaggerResponse<ICollection<TriageAlert>>> GetTriageAlertsAsync(Guid? id)
    {
        return Task.FromResult
        (
            new SwaggerResponse<ICollection<TriageAlert>>
            (
                StatusCodes.Status200OK, 
                emptyHeaders, 
                new List<TriageAlert>()
                {
                    new()
                    {
                        Alerts = new() { AlertInfo = new() { CreatedAt = DateTimeOffset.UtcNow}},
                        Items = new() { Id = Guid.NewGuid() },
                        Triage = new() { Id = Guid.NewGuid().ToString() }
                    }
                }
            )
        );        
    }

    public Task<SwaggerResponse<TriageEventResponse>> GetTriageEventsAsync(int? limit)
    {
        return Task.FromResult
        (
            new SwaggerResponse<TriageEventResponse>
            (
                StatusCodes.Status200OK, 
                emptyHeaders, 
                new () 
                { 
                    Data =  Enumerable
                        .Range(0, 10)
                        .Select(x => new TriageEvent() 
                        {
                            TriageItemId = Guid.NewGuid()
                        })
                        .ToList() 
                }
            )
        );
    }

    public Task<SwaggerResponse<TriageItemResponse>> GetTriageItemsAsync(Guid? id)
    {
        return Task.FromResult
        (
            new SwaggerResponse<TriageItemResponse>
            (
                StatusCodes.Status200OK, 
                emptyHeaders, 
                new () 
                { 
                    Data = 
                    [
                        new TriageItem()
                        {
                            Source = new()
                            {
                                AlertId = Guid.NewGuid()
                            }
                        } 
                    ]
                }
            )
        );
    }
}
