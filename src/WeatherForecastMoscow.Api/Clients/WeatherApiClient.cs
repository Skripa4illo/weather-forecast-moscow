using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using WeatherForecastMoscow.Api.Exceptions;
using WeatherForecastMoscow.Api.Models.Dtos;
using WeatherForecastMoscow.Api.Options;

namespace WeatherForecastMoscow.Api.Clients;

public sealed class WeatherApiClient : IWeatherApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly WeatherApiOptions _options;
    private readonly ILogger<WeatherApiClient> _logger;

    public WeatherApiClient(HttpClient http, IOptions<WeatherApiOptions> options, ILogger<WeatherApiClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public Task<WeatherApiCurrentResponseDto> GetCurrentWeatherAsync(CancellationToken cancellationToken = default) =>
        GetAsync<WeatherApiCurrentResponseDto>("current.json", null, cancellationToken);

    public Task<WeatherApiForecastResponseDto> GetForecastAsync(CancellationToken cancellationToken = default)
    {
        var days = Math.Clamp(_options.ForecastDays, 1, 10);
        return GetAsync<WeatherApiForecastResponseDto>("forecast.json", $"days={days}", cancellationToken);
    }

    private async Task<T> GetAsync<T>(string endpoint, string? extraQuery, CancellationToken cancellationToken)
    {
        EnsureApiKey();

        var q = Uri.EscapeDataString(_options.LocationQuery);
        var key = Uri.EscapeDataString(_options.ApiKey);
        var uri = new StringBuilder($"/v1/{endpoint}?key={key}&q={q}");
        if (!string.IsNullOrEmpty(extraQuery))
            uri.Append('&').Append(extraQuery);

        var requestUri = uri.ToString();

        _logger.LogInformation("Calling Weather API endpoint {Endpoint}", endpoint);

        using var response = await _http.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Weather API error {StatusCode} for {Endpoint}. Body: {Body}",
                (int)response.StatusCode,
                endpoint,
                Truncate(body, 500));

            throw new WeatherApiException(
                $"Weather API returned {(int)response.StatusCode} {response.ReasonPhrase}.",
                (int)response.StatusCode);
        }

        try
        {
            var dto = JsonSerializer.Deserialize<T>(body, JsonOptions);
            if (dto is null)
                throw new WeatherApiException("Weather API returned an empty payload.");

            return dto;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Weather API response.");
            throw new WeatherApiException("Failed to parse Weather API response.", inner: ex);
        }
    }

    private void EnsureApiKey()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException(
                "Weather API key is not configured. Set WeatherApi:ApiKey or environment variable WeatherApi__ApiKey.");
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
