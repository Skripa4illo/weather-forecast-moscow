using Microsoft.Extensions.Caching.Memory;
using WeatherForecastMoscow.Api.Clients;
using WeatherForecastMoscow.Api.Models.Dtos;
using WeatherForecastMoscow.Api.Models.ViewModels;

namespace WeatherForecastMoscow.Api.Services;

public sealed class WeatherService : IWeatherService
{
    public const string CacheKey = "weather_aggregate_moscow_v1";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly IWeatherApiClient _apiClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WeatherService> _logger;
    private readonly TimeProvider _timeProvider;

    public WeatherService(
        IWeatherApiClient apiClient,
        IMemoryCache cache,
        ILogger<WeatherService> logger,
        TimeProvider timeProvider)
    {
        _apiClient = apiClient;
        _cache = cache;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<WeatherAggregateViewModel> GetMoscowWeatherAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out WeatherAggregateViewModel? cached) && cached is not null)
        {
            _logger.LogInformation("Weather cache hit for key {CacheKey}", CacheKey);
            return CloneWithFlags(cached, fromCache: true);
        }

        _logger.LogInformation("Weather cache miss for key {CacheKey}", CacheKey);

        var currentTask = _apiClient.GetCurrentWeatherAsync(cancellationToken);
        var forecastTask = _apiClient.GetForecastAsync(cancellationToken);
        await Task.WhenAll(currentTask, forecastTask).ConfigureAwait(false);

        var currentResponse = await currentTask.ConfigureAwait(false);
        var forecastResponse = await forecastTask.ConfigureAwait(false);

        var vm = BuildViewModel(currentResponse, forecastResponse);
        vm.FromCache = false;

        _cache.Set(
            CacheKey,
            CloneWithFlags(vm, fromCache: false),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration
            });

        _logger.LogInformation("Weather data cached for {Minutes} minutes", CacheDuration.TotalMinutes);

        return vm;
    }

    private WeatherAggregateViewModel BuildViewModel(
        WeatherApiCurrentResponseDto currentResponse,
        WeatherApiForecastResponseDto forecastResponse)
    {
        var location = MapLocation(currentResponse.Location ?? forecastResponse.Location);
        var current = MapCurrent(currentResponse.Current);
        var nowUnix = _timeProvider.GetUtcNow().ToUnixTimeSeconds();

        var days = forecastResponse.Forecast?.Forecastday ?? [];
        var hourly = BuildHourlyForecast(days, nowUnix);
        var daily = BuildDailyForecast(days);

        return new WeatherAggregateViewModel
        {
            Location = location,
            Current = current,
            HourlyForecast = hourly,
            DailyForecast = daily,
            RetrievedAtUtcUnix = _timeProvider.GetUtcNow().ToUnixTimeSeconds(),
            FromCache = false
        };
    }

    private static IReadOnlyList<HourlyForecastViewModel> BuildHourlyForecast(
        IReadOnlyList<WeatherApiForecastDayDto> days,
        long nowUnixSeconds)
    {
        if (days.Count == 0)
            return [];

        var today = days[0];
        var todayDate = today.Date ?? string.Empty;

        var todayHours = today.Hour
            .Where(h => h.TimeEpoch >= nowUnixSeconds
                        && !string.IsNullOrEmpty(h.Time)
                        && h.Time.StartsWith(todayDate, StringComparison.Ordinal))
            .Select(MapHour)
            .ToList();

        List<HourlyForecastViewModel> tomorrowHours = [];
        if (days.Count > 1)
            tomorrowHours = days[1].Hour.Select(MapHour).ToList();

        var combined = new List<HourlyForecastViewModel>(todayHours.Count + tomorrowHours.Count);
        combined.AddRange(todayHours);
        combined.AddRange(tomorrowHours);
        return combined;
    }

    private static IReadOnlyList<DailyForecastViewModel> BuildDailyForecast(IReadOnlyList<WeatherApiForecastDayDto> days) =>
        days.Select(d => new DailyForecastViewModel
        {
            Date = d.Date ?? string.Empty,
            DateEpoch = d.DateEpoch,
            MaxTempC = d.Day?.MaxtempC ?? 0,
            MinTempC = d.Day?.MintempC ?? 0,
            ConditionText = d.Day?.Condition?.Text?.Trim() ?? string.Empty,
            ConditionIconUrl = NormalizeIconUrl(d.Day?.Condition?.Icon),
            ChanceOfRain = d.Day?.DailyChanceOfRain ?? 0,
            TotalPrecipMm = d.Day?.TotalprecipMm ?? 0,
            MaxWindKph = d.Day?.MaxwindKph ?? 0,
            Uv = d.Day?.Uv ?? 0
        }).ToList();

    private static LocationViewModel MapLocation(WeatherApiLocationDto? dto) =>
        dto is null
            ? new LocationViewModel()
            : new LocationViewModel
            {
                Name = dto.Name ?? string.Empty,
                Region = dto.Region ?? string.Empty,
                Country = dto.Country ?? string.Empty,
                Latitude = dto.Lat,
                Longitude = dto.Lon,
                TimeZoneId = dto.TzId ?? string.Empty,
                LocalTime = dto.Localtime ?? string.Empty
            };

    private static CurrentWeatherViewModel MapCurrent(WeatherApiCurrentDto? dto) =>
        dto is null
            ? new CurrentWeatherViewModel()
            : new CurrentWeatherViewModel
            {
                TempC = dto.TempC,
                FeelsLikeC = dto.FeelslikeC,
                ConditionText = dto.Condition?.Text?.Trim() ?? string.Empty,
                ConditionIconUrl = NormalizeIconUrl(dto.Condition?.Icon),
                IsDay = dto.IsDay,
                WindKph = dto.WindKph,
                Humidity = dto.Humidity,
                PressureMb = dto.PressureMb,
                PrecipMm = dto.PrecipMm,
                LastUpdatedLocal = dto.LastUpdated ?? string.Empty
            };

    private static HourlyForecastViewModel MapHour(WeatherApiHourDto h)
    {
        var datePart = string.Empty;
        if (!string.IsNullOrEmpty(h.Time) && h.Time.Length >= 10)
            datePart = h.Time[..10];

        return new HourlyForecastViewModel
        {
            TimeEpoch = h.TimeEpoch,
            TimeLocal = h.Time ?? string.Empty,
            CalendarDate = datePart,
            TempC = h.TempC,
            ConditionText = h.Condition?.Text?.Trim() ?? string.Empty,
            ConditionIconUrl = NormalizeIconUrl(h.Condition?.Icon),
            IsDay = h.IsDay,
            ChanceOfRain = h.ChanceOfRain
        };
    }

    private static string NormalizeIconUrl(string? icon)
    {
        if (string.IsNullOrEmpty(icon))
            return string.Empty;
        return icon.StartsWith("//", StringComparison.Ordinal) ? "https:" + icon : icon;
    }

    private static WeatherAggregateViewModel CloneWithFlags(WeatherAggregateViewModel source, bool fromCache) =>
        new()
        {
            Location = source.Location,
            Current = source.Current,
            HourlyForecast = source.HourlyForecast.ToList(),
            DailyForecast = source.DailyForecast.ToList(),
            RetrievedAtUtcUnix = source.RetrievedAtUtcUnix,
            FromCache = fromCache
        };
}
