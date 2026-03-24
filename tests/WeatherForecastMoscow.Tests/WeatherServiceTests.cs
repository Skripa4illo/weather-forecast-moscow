using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WeatherForecastMoscow.Api.Clients;
using WeatherForecastMoscow.Api.Models.Dtos;
using WeatherForecastMoscow.Api.Services;

namespace WeatherForecastMoscow.Tests;

public sealed class WeatherServiceTests
{
    [Fact]
    public async Task GetMoscowWeatherAsync_FiltersHourly_ToFutureTodayAndFullTomorrow()
    {
        const long nowUnix = 1_000;
        var time = new FixedTimeProvider(DateTimeOffset.FromUnixTimeSeconds(nowUnix));

        var current = new WeatherApiCurrentResponseDto
        {
            Location = new WeatherApiLocationDto { Name = "Moscow", Localtime = "2026-03-24 12:00" },
            Current = new WeatherApiCurrentDto
            {
                TempC = 5,
                FeelslikeC = 3,
                Condition = new WeatherApiConditionDto { Text = "Cloudy", Icon = "//cdn.example/1.png" },
                IsDay = 1,
                LastUpdated = "2026-03-24 11:45"
            }
        };

        var forecast = new WeatherApiForecastResponseDto
        {
            Forecast = new WeatherApiForecastBlockDto
            {
                Forecastday =
                [
                    new WeatherApiForecastDayDto
                    {
                        Date = "2026-03-24",
                        DateEpoch = 0,
                        Day = new WeatherApiDaySummaryDto
                        {
                            MaxtempC = 8,
                            MintempC = 1,
                            Condition = new WeatherApiConditionDto { Text = "Day summary", Icon = "//x" },
                            DailyChanceOfRain = 10
                        },
                        Hour =
                        [
                            new WeatherApiHourDto
                            {
                                TimeEpoch = 500,
                                Time = "2026-03-24 08:00",
                                TempC = 2,
                                IsDay = 1,
                                Condition = new WeatherApiConditionDto { Text = "Past", Icon = "//a" },
                                ChanceOfRain = 0
                            },
                            new WeatherApiHourDto
                            {
                                TimeEpoch = 1_500,
                                Time = "2026-03-24 12:00",
                                TempC = 5,
                                IsDay = 1,
                                Condition = new WeatherApiConditionDto { Text = "Later today", Icon = "//b" },
                                ChanceOfRain = 5
                            }
                        ]
                    },
                    new WeatherApiForecastDayDto
                    {
                        Date = "2026-03-25",
                        DateEpoch = 0,
                        Day = new WeatherApiDaySummaryDto { MaxtempC = 9, MintempC = 2 },
                        Hour =
                        [
                            new WeatherApiHourDto
                            {
                                TimeEpoch = 2_000,
                                Time = "2026-03-25 06:00",
                                TempC = 3,
                                IsDay = 0,
                                Condition = new WeatherApiConditionDto { Text = "Tmrw1", Icon = "//c" },
                                ChanceOfRain = 1
                            },
                            new WeatherApiHourDto
                            {
                                TimeEpoch = 2_100,
                                Time = "2026-03-25 07:00",
                                TempC = 4,
                                IsDay = 1,
                                Condition = new WeatherApiConditionDto { Text = "Tmrw2", Icon = "//d" },
                                ChanceOfRain = 2
                            }
                        ]
                    },
                    new WeatherApiForecastDayDto
                    {
                        Date = "2026-03-26",
                        DateEpoch = 0,
                        Day = new WeatherApiDaySummaryDto { MaxtempC = 7, MintempC = 0 }
                    }
                ]
            }
        };

        var client = new Mock<IWeatherApiClient>(MockBehavior.Strict);
        client.Setup(c => c.GetCurrentWeatherAsync(It.IsAny<CancellationToken>())).ReturnsAsync(current);
        client.Setup(c => c.GetForecastAsync(It.IsAny<CancellationToken>())).ReturnsAsync(forecast);

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new WeatherService(
            client.Object,
            cache,
            NullLogger<WeatherService>.Instance,
            time);

        var result = await sut.GetMoscowWeatherAsync(CancellationToken.None);

        Assert.False(result.FromCache);
        Assert.Equal(3, result.DailyForecast.Count);
        Assert.Equal(3, result.HourlyForecast.Count); // 1 future today + 2 tomorrow

        Assert.Equal("Later today", result.HourlyForecast[0].ConditionText);
        Assert.Equal("Tmrw1", result.HourlyForecast[1].ConditionText);
        Assert.Equal("Tmrw2", result.HourlyForecast[2].ConditionText);

        client.Verify(c => c.GetCurrentWeatherAsync(It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.GetForecastAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMoscowWeatherAsync_SecondCall_UsesCache_AndSetsFromCache()
    {
        var time = new FixedTimeProvider(DateTimeOffset.FromUnixTimeSeconds(10_000));

        var current = new WeatherApiCurrentResponseDto
        {
            Location = new WeatherApiLocationDto { Name = "Moscow" },
            Current = new WeatherApiCurrentDto { TempC = 1, Condition = new WeatherApiConditionDto { Text = "OK" } }
        };

        var forecast = new WeatherApiForecastResponseDto
        {
            Forecast = new WeatherApiForecastBlockDto
            {
                Forecastday =
                [
                    new WeatherApiForecastDayDto
                    {
                        Date = "2026-03-24",
                        Hour =
                        [
                            new WeatherApiHourDto
                            {
                                TimeEpoch = 20_000,
                                Time = "2026-03-24 20:00",
                                TempC = 1,
                                Condition = new WeatherApiConditionDto { Text = "H" }
                            }
                        ]
                    },
                    new WeatherApiForecastDayDto { Date = "2026-03-25", Hour = [] }
                ]
            }
        };

        var client = new Mock<IWeatherApiClient>();
        client.Setup(c => c.GetCurrentWeatherAsync(It.IsAny<CancellationToken>())).ReturnsAsync(current);
        client.Setup(c => c.GetForecastAsync(It.IsAny<CancellationToken>())).ReturnsAsync(forecast);

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new WeatherService(client.Object, cache, NullLogger<WeatherService>.Instance, time);

        var first = await sut.GetMoscowWeatherAsync(CancellationToken.None);
        var second = await sut.GetMoscowWeatherAsync(CancellationToken.None);

        Assert.False(first.FromCache);
        Assert.True(second.FromCache);
        client.Verify(c => c.GetCurrentWeatherAsync(It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.GetForecastAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
