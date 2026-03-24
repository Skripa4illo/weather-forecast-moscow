# Weather Forecast Moscow

Production-style sample: **ASP.NET Core 8 Web API** aggregates [WeatherAPI.com](https://www.weatherapi.com/) for a fixed Moscow coordinate, and a **React (Vite)** SPA consumes only the backend.

## Stack

| Layer | Technology |
|--------|------------|
| Backend | .NET 8, ASP.NET Core Web API, `HttpClientFactory`, `IMemoryCache`, `ILogger`, global exception middleware |
| Frontend | React 19 (Vite template), Axios, CSS variables (light/dark) |
| External API | WeatherAPI.com (`current` + `forecast` 3 days) |

## Architecture (backend)

Request flow: **Controller → Service → Weather API client** (HTTP).

- **Controllers** — HTTP only; `GET /api/weather`.
- **Services** — `WeatherService` merges current + forecast, filters hourly slots (future hours for **today** in the forecast’s first day + **all hours for tomorrow**), builds **3-day daily** list, applies **10-minute** `IMemoryCache`, logs cache hit/miss.
- **Clients** — `WeatherApiClient` uses named `HttpClient` configuration, timeouts, structured logging, and maps failures to `WeatherApiException`.
- **Models** — `Models/Dtos` mirror provider JSON (snake_case via `System.Text.Json`); `Models/ViewModels` are API-facing shapes.
- **Middleware** — `GlobalExceptionHandlingMiddleware` returns **Problem Details–style** JSON (`application/problem+json`).

Configuration: `WeatherApi` section in `appsettings.json`. **Do not commit real API keys**; use environment variables or user secrets (see below).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) (for the frontend)
- A [WeatherAPI.com](https://www.weatherapi.com/) API key (free tier is enough)

## Configuration (API key)

Set one of:

```powershell
# PowerShell (session)
$env:WeatherApi__ApiKey = "your-key-here"
```

```bash
# bash
export WeatherApi__ApiKey="your-key-here"
```

Or use .NET user secrets in the API project:

```bash
cd src/WeatherForecastMoscow.Api
dotnet user-secrets init
dotnet user-secrets set "WeatherApi:ApiKey" "your-key-here"
```

Optional: `WeatherApi__BaseUrl` (default `https://api.weatherapi.com`), `WeatherApi__LocationQuery` (default `55.7558,37.6173`).

## Run the API

```bash
cd src/WeatherForecastMoscow.Api
dotnet run
```

Default HTTP URL (see `Properties/launchSettings.json`): `http://localhost:5269`  
Swagger (Development): `/swagger`

## Run the SPA

The dev server proxies `/api` to `http://localhost:5269` (see `frontend/weather-ui/vite.config.js`).

```bash
cd frontend/weather-ui
npm install
npm run dev
```

Open `http://localhost:5173`. The UI refreshes data every **10 minutes** and supports **dark/light** toggle (persisted in `localStorage`).

### Production build (frontend)

```bash
cd frontend/weather-ui
npm run build
```

Serve `dist/` behind a reverse proxy that forwards `/api` to the backend, or set `VITE_API_BASE_URL` at build time to the public API origin (e.g. `https://api.example.com`).

## Tests

```bash
dotnet test
```

Includes `WeatherServiceTests` with a **mocked** `IWeatherApiClient` (hourly filtering + cache behavior).

## Docker (API)

From the repository root:

```bash
docker build -t weather-moscow-api .
docker run --rm -p 8080:8080 -e WeatherApi__ApiKey="your-key-here" weather-moscow-api
```

API base: `http://localhost:8080`. Point the SPA `VITE_API_BASE_URL` at that origin for integrated demos.

## Example `GET /api/weather` response (shape)

```json
{
  "location": {
    "name": "Moscow",
    "region": "Moscow City",
    "country": "Russia",
    "latitude": 55.752,
    "longitude": 37.616,
    "timeZoneId": "Europe/Moscow",
    "localTime": "2026-03-24 12:00"
  },
  "current": {
    "tempC": 5,
    "feelsLikeC": 3,
    "conditionText": "Partly cloudy",
    "conditionIconUrl": "https://cdn.weatherapi.com/weather/64x64/day/116.png",
    "isDay": 1,
    "windKph": 13,
    "humidity": 72,
    "pressureMb": 1016,
    "precipMm": 0,
    "lastUpdatedLocal": "2026-03-24 11:45"
  },
  "hourlyForecast": [
    {
      "timeEpoch": 1774357200,
      "timeLocal": "2026-03-24 15:00",
      "calendarDate": "2026-03-24",
      "tempC": 6.1,
      "conditionText": "Sunny",
      "conditionIconUrl": "https://cdn.weatherapi.com/...",
      "isDay": 1,
      "chanceOfRain": 0
    }
  ],
  "dailyForecast": [
    {
      "date": "2026-03-24",
      "dateEpoch": 1774310400,
      "maxTempC": 9.6,
      "minTempC": 2.2,
      "conditionText": "Partly Cloudy",
      "conditionIconUrl": "https://cdn.weatherapi.com/...",
      "chanceOfRain": 0,
      "totalPrecipMm": 0.02,
      "maxWindKph": 13,
      "uv": 0.6
    }
  ],
  "retrievedAtUtcUnix": 1774321200,
  "fromCache": false
}
```

Values vary with live weather; `hourlyForecast` contains **only** filtered rows as specified in requirements; `dailyForecast` has up to **3** days.

## Repository layout

```
src/WeatherForecastMoscow.Api/   # Web API
frontend/weather-ui/             # React + Vite
tests/WeatherForecastMoscow.Tests/
Dockerfile
WeatherForecastMoscow.sln
```

## Risks / limitations

- **Quota / rate limits** apply per WeatherAPI.com plan; caching reduces calls to at most one refresh per **10 minutes** per process (in-memory cache is **per instance**, not shared across pods).
- **No retry policy** on the HTTP client in this sample; add Polly if you need resilient calls to the provider.
- **Secrets**: keep keys in environment variables, vaults, or Kubernetes Secrets—not in source control.
