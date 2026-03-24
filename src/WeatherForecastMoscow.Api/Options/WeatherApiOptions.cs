namespace WeatherForecastMoscow.Api.Options;

public sealed class WeatherApiOptions
{
    public const string SectionName = "WeatherApi";

    /// <summary>Base URL without trailing slash, e.g. https://api.weatherapi.com</summary>
    public string BaseUrl { get; set; } = "https://api.weatherapi.com";

    /// <summary>API key from WeatherAPI.com (prefer environment variable WeatherApi__ApiKey).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Query parameter "q" (lat,lon or city name).</summary>
    public string LocationQuery { get; set; } = "55.7558,37.6173";

    public int ForecastDays { get; set; } = 3;

    public int RequestTimeoutSeconds { get; set; } = 20;
}
