using System.Runtime.CompilerServices;
using MsSentinel.MockApi.WebApi.Api;
using MsSentinel.MockApi.WebApi.Application;

namespace MsSentinel.MockApi.WebApi.Controllers;

public partial class AdminControllerImplementation(FailureModes failureModes, ILogger<ServiceControllerImplementation> logger) : IAdminController
{
    private IReadOnlyDictionary<string,IEnumerable<string>> emptyHeaders { get; } = new Dictionary<string,IEnumerable<string>>();

    public Task<SwaggerResponse> SetFailureStatesAsync(FailureModesSpecification body)
    {
        bool ok = false;

        if (body.Threats.HasValue && body.Threats.Value >= 200)
        {
            failureModes.ThreatsStatus = body.Threats.Value;
            ok = true;
        }

        if (body.Groups.HasValue && body.Groups.Value >= 200)
        {
            failureModes.GroupsStatus = body.Groups.Value;
            ok = true;
        }

        if (body.Alerts.HasValue && body.Alerts.Value >= 200)
        {
            failureModes.AlertsStatus = body.Alerts.Value;
            ok = true;
        }

        logOk();

        return Task.FromResult(
            new SwaggerResponse(
                ok ? StatusCodes.Status204NoContent : StatusCodes.Status400BadRequest,
                emptyHeaders
            )
        );
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Location}: OK", EventId = 1000)]
    public partial void logOk([CallerMemberName] string? location = null);

}