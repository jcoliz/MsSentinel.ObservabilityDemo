namespace MsSentinel.ObservabilityDemo.ApiService.Entities;

public record WeatherForecast
{
    public DateOnly Date { get; init; } = DateOnly.MinValue;

    public int TemperatureC { get; init; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; init; }
}
