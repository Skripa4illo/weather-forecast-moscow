namespace WeatherForecastMoscow.Api.Models.Dtos;

public sealed class WeatherApiLocationDto
{
    public string? Name { get; set; }
    public string? Region { get; set; }
    public string? Country { get; set; }
    public double Lat { get; set; }
    public double Lon { get; set; }
    public string? TzId { get; set; }
    public long LocaltimeEpoch { get; set; }
    public string? Localtime { get; set; }
}

public sealed class WeatherApiConditionDto
{
    public string? Text { get; set; }
    public string? Icon { get; set; }
    public int Code { get; set; }
}

public sealed class WeatherApiCurrentDto
{
    public long LastUpdatedEpoch { get; set; }
    public string? LastUpdated { get; set; }
    public double TempC { get; set; }
    public double TempF { get; set; }
    public int IsDay { get; set; }
    public WeatherApiConditionDto? Condition { get; set; }
    public double WindKph { get; set; }
    public double WindMph { get; set; }
    public int Humidity { get; set; }
    public double FeelslikeC { get; set; }
    public double PressureMb { get; set; }
    public double PrecipMm { get; set; }
}

public sealed class WeatherApiCurrentResponseDto
{
    public WeatherApiLocationDto? Location { get; set; }
    public WeatherApiCurrentDto? Current { get; set; }
}

public sealed class WeatherApiHourDto
{
    public long TimeEpoch { get; set; }
    public string? Time { get; set; }
    public double TempC { get; set; }
    public int IsDay { get; set; }
    public WeatherApiConditionDto? Condition { get; set; }
    public double ChanceOfRain { get; set; }
    public double PrecipMm { get; set; }
}

public sealed class WeatherApiDaySummaryDto
{
    public double MaxtempC { get; set; }
    public double MintempC { get; set; }
    public double MaxwindKph { get; set; }
    public double TotalprecipMm { get; set; }
    public double AvgvisKm { get; set; }
    public double Avghumidity { get; set; }
    public double Uv { get; set; }
    public WeatherApiConditionDto? Condition { get; set; }
    public int DailyChanceOfRain { get; set; }
}

public sealed class WeatherApiForecastDayDto
{
    public string? Date { get; set; }
    public long DateEpoch { get; set; }
    public WeatherApiDaySummaryDto? Day { get; set; }
    public List<WeatherApiHourDto> Hour { get; set; } = [];
}

public sealed class WeatherApiForecastBlockDto
{
    public List<WeatherApiForecastDayDto> Forecastday { get; set; } = [];
}

public sealed class WeatherApiForecastResponseDto
{
    public WeatherApiLocationDto? Location { get; set; }
    public WeatherApiCurrentDto? Current { get; set; }
    public WeatherApiForecastBlockDto? Forecast { get; set; }
}
