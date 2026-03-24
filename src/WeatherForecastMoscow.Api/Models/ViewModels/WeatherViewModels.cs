namespace WeatherForecastMoscow.Api.Models.ViewModels;

public sealed class WeatherAggregateViewModel
{
    public LocationViewModel Location { get; set; } = new();
    public CurrentWeatherViewModel Current { get; set; } = new();
    public IReadOnlyList<HourlyForecastViewModel> HourlyForecast { get; set; } = [];
    public IReadOnlyList<DailyForecastViewModel> DailyForecast { get; set; } = [];
    public long RetrievedAtUtcUnix { get; set; }
    public bool FromCache { get; set; }
}

public sealed class LocationViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
    public string LocalTime { get; set; } = string.Empty;
}

public sealed class CurrentWeatherViewModel
{
    public double TempC { get; set; }
    public double FeelsLikeC { get; set; }
    public string ConditionText { get; set; } = string.Empty;
    public string ConditionIconUrl { get; set; } = string.Empty;
    public int IsDay { get; set; }
    public double WindKph { get; set; }
    public int Humidity { get; set; }
    public double PressureMb { get; set; }
    public double PrecipMm { get; set; }
    public string LastUpdatedLocal { get; set; } = string.Empty;
}

public sealed class HourlyForecastViewModel
{
    public long TimeEpoch { get; set; }
    public string TimeLocal { get; set; } = string.Empty;
    public string CalendarDate { get; set; } = string.Empty;
    public double TempC { get; set; }
    public string ConditionText { get; set; } = string.Empty;
    public string ConditionIconUrl { get; set; } = string.Empty;
    public int IsDay { get; set; }
    public double ChanceOfRain { get; set; }
}

public sealed class DailyForecastViewModel
{
    public string Date { get; set; } = string.Empty;
    public long DateEpoch { get; set; }
    public double MaxTempC { get; set; }
    public double MinTempC { get; set; }
    public string ConditionText { get; set; } = string.Empty;
    public string ConditionIconUrl { get; set; } = string.Empty;
    public double ChanceOfRain { get; set; }
    public double TotalPrecipMm { get; set; }
    public double MaxWindKph { get; set; }
    public double Uv { get; set; }
}
