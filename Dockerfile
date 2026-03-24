# Stack: .NET 8 (ASP.NET Core Web API)
# Build from repository root: docker build -t weather-moscow-api .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/WeatherForecastMoscow.Api/WeatherForecastMoscow.Api.csproj WeatherForecastMoscow.Api/
RUN dotnet restore WeatherForecastMoscow.Api/WeatherForecastMoscow.Api.csproj
COPY src/WeatherForecastMoscow.Api/ ./WeatherForecastMoscow.Api/
WORKDIR /src/WeatherForecastMoscow.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
# Provide WeatherApi__ApiKey at runtime (Kubernetes Secret, docker run -e, etc.)
ENTRYPOINT ["dotnet", "WeatherForecastMoscow.Api.dll"]
