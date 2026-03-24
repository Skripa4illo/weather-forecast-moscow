using WeatherForecastMoscow.Api.Models.ViewModels;

namespace WeatherForecastMoscow.Api.Services;

public interface IWeatherService
{
    Task<WeatherAggregateViewModel> GetMoscowWeatherAsync(CancellationToken cancellationToken = default);
}
