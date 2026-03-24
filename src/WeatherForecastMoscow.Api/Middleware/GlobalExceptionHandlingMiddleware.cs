using System.Text.Json;
using WeatherForecastMoscow.Api.Exceptions;

namespace WeatherForecastMoscow.Api.Middleware;

public sealed class GlobalExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(exception, "Response already started; cannot write error body.");
            return Task.CompletedTask;
        }

        var (status, title, detail) = MapException(exception);

        _logger.LogError(exception, "Request failed with {Status}: {Title}", status, title);

        context.Response.ContentType = "application/problem+json; charset=utf-8";
        context.Response.StatusCode = status;

        var problem = new
        {
            type = "https://httpstatuses.io/" + status,
            title,
            status,
            detail,
            traceId = context.TraceIdentifier
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static (int Status, string Title, string Detail) MapException(Exception exception) =>
        exception switch
        {
            WeatherApiException wx => (
                StatusCodes.Status502BadGateway,
                "Upstream weather provider error",
                wx.Message),
            InvalidOperationException op => (
                StatusCodes.Status500InternalServerError,
                "Configuration or operational error",
                op.Message),
            OperationCanceledException => (
                StatusCodes.Status408RequestTimeout,
                "Request cancelled",
                "The client closed the request."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "See server logs for details.")
        };
}
