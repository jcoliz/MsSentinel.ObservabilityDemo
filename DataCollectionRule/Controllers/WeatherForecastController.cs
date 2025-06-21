using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MsSentinel.ObservabilityDemo.DataCollectionRule.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(ActivitySource activitySource, ILogger<WeatherForecastController> logger) : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    [HttpPost(Name = "PostToStream")]
    public async Task<IEnumerable<WeatherForecast>> Post(string[] strings)
    {
        using (var activity = activitySource.StartActivity("Transform", ActivityKind.Consumer))
        {
            await Task.Delay(TimeSpan.FromSeconds(0.2));
        }

        using (var activity = activitySource.StartActivity("Store", ActivityKind.Consumer))
        {
            activity?.SetTag("DataCollectionRule.Count", strings.Length);            

            await Task.Delay(TimeSpan.FromSeconds(0.3));
        }

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
}
