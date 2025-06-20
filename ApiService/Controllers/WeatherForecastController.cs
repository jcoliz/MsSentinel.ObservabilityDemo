using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MsSentinel.ObservabilityDemo.ApiService.Entities;

namespace MsSentinel.ObservabilityDemo.ApiService.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(ActivitySource activitySource, ILogger<WeatherForecastController> logger) : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        IEnumerable<WeatherForecast> result = Enumerable.Empty<WeatherForecast>();

        using (var activity = activitySource.StartActivity("Gateway", ActivityKind.Consumer))
        {
            activity?.SetTag("MsSentinel.ObservabilityDemo.RestApiPoller.Location", nameof(WeatherForecastController));

            using (var activity_1 = activitySource.StartActivity("Application", ActivityKind.Consumer))
            {
                await Task.Delay(TimeSpan.FromSeconds(0.1));
                using (var activity_2 = activitySource.StartActivity("Database", ActivityKind.Consumer))
                {
                    await Task.Delay(TimeSpan.FromSeconds(0.7));
                    result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                    {
                        Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                        TemperatureC = Random.Shared.Next(-20, 55),
                        Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                    })
                    .ToArray();
                }
                await Task.Delay(TimeSpan.FromSeconds(0.1));
            }
        }

        return result;
    }
}
