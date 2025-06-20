namespace MsSentinel.MockApi.WebApi.Application;

public class FailureModes
{
    /// <summary>
    /// Set this if Threats API should return a failure status
    /// </summary>
    public int ThreatsStatus { get; set; } = StatusCodes.Status200OK;

    /// <summary>
    /// Set this if Groups API should return a failure status
    /// </summary>
    public int GroupsStatus { get; set; } = StatusCodes.Status200OK;

    /// <summary>
    /// Set this if Alerts API should return a failure status
    /// </summary>
    public int AlertsStatus { get; set; } = StatusCodes.Status200OK;
}
