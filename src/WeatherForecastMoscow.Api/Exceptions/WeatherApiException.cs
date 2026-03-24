namespace WeatherForecastMoscow.Api.Exceptions;

public sealed class WeatherApiException : Exception
{
    public int? StatusCode { get; }

    public WeatherApiException(string message, int? statusCode = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
    }
}
