using Microsoft.AspNetCore.Mvc;
using WeatherForecastMoscow.Api.Models.ViewModels;
using WeatherForecastMoscow.Api.Services;

namespace WeatherForecastMoscow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(IWeatherService weatherService, ILogger<WeatherController> logger)
    {
        _weatherService = weatherService;
        _logger = logger;
    }

    /// <summary>Current conditions, filtered hourly (today remainder + tomorrow), and 3-day daily forecast for Moscow.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(WeatherAggregateViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<WeatherAggregateViewModel>> GetAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/weather");
        var vm = await _weatherService.GetMoscowWeatherAsync(cancellationToken).ConfigureAwait(false);
        return Ok(vm);
    }
}
