using WeatherForecastMoscow.Api.Models.Dtos;

namespace WeatherForecastMoscow.Api.Clients;

public interface IWeatherApiClient
{
    Task<WeatherApiCurrentResponseDto> GetCurrentWeatherAsync(CancellationToken cancellationToken = default);

    Task<WeatherApiForecastResponseDto> GetForecastAsync(CancellationToken cancellationToken = default);
}
