using WeatherForecastMoscow.Api.Clients;
using WeatherForecastMoscow.Api.Middleware;
using WeatherForecastMoscow.Api.Options;
using WeatherForecastMoscow.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services
    .AddOptions<WeatherApiOptions>()
    .Bind(builder.Configuration.GetSection(WeatherApiOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), "WeatherApi:BaseUrl is required.")
    .ValidateOnStart();

var weatherApiSection = builder.Configuration.GetSection(WeatherApiOptions.SectionName);
var baseUrl = weatherApiSection["BaseUrl"] ?? "https://api.weatherapi.com";
var timeoutSeconds = int.TryParse(weatherApiSection["RequestTimeoutSeconds"], out var ts) ? ts : 20;

builder.Services.AddHttpClient<IWeatherApiClient, WeatherApiClient>(client =>
{
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 120));
});

builder.Services.AddScoped<IWeatherService, WeatherService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Weather Forecast Moscow API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "Frontend",
        policy => policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
