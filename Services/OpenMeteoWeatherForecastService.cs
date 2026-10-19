using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TestAPI.Models;
using TestAPI.Options;
using TestAPI.Services.OpenMeteo;

namespace TestAPI.Services;

/// <summary>
/// <see cref="IWeatherForecastService"/> backed by the Open-Meteo forecast API.
/// Resolves the city with <see cref="IGeocodingService"/>, then fetches and caches the daily forecast.
/// </summary>
public sealed class OpenMeteoWeatherForecastService(
    HttpClient httpClient,
    IGeocodingService geocoding,
    IMemoryCache cache,
    IOptions<OpenMeteoOptions> options,
    TimeProvider timeProvider,
    ILogger<OpenMeteoWeatherForecastService> logger) : IWeatherForecastService
{
    private const string DailyVariables =
        "temperature_2m_max,temperature_2m_min,weather_code,wind_speed_10m_max,precipitation_sum,precipitation_probability_max";

    public async Task<CityForecast?> GetForecastAsync(string cityName, int days, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(days, IWeatherForecastService.MinDays);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(days, IWeatherForecastService.MaxDays);

        var location = await geocoding.FindCityAsync(cityName, cancellationToken);
        if (location is null)
        {
            logger.LogInformation("No geocoding match for {City}", cityName);
            return null;
        }

        var cacheKey = FormattableString.Invariant(
            $"forecast:{location.Latitude:F4}:{location.Longitude:F4}:{days}");

        if (cache.TryGetValue(cacheKey, out CityForecast? cached) && cached is not null)
        {
            return cached;
        }

        var url = FormattableString.Invariant(
            $"v1/forecast?latitude={location.Latitude}&longitude={location.Longitude}&daily={DailyVariables}&timezone=auto&forecast_days={days}");

        logger.LogDebug("Fetching forecast for {City} via {Url}", location.Name, url);

        var response = await httpClient.GetFromJsonAsync<ForecastResponse>(url, cancellationToken)
            ?? throw new HttpRequestException("Open-Meteo returned an empty forecast body.");

        var forecast = new CityForecast
        {
            Location = location,
            Days = MapDays(response.Daily),
            RetrievedAt = timeProvider.GetUtcNow(),
        };

        cache.Set(cacheKey, forecast, options.Value.ForecastCacheDuration);
        return forecast;
    }

    /// <summary>Zips the parallel per-variable arrays from Open-Meteo into one object per day.</summary>
    internal static IReadOnlyList<WeatherForecast> MapDays(DailyBlock? daily)
    {
        if (daily is null || daily.Time.Count == 0)
        {
            return [];
        }

        var result = new List<WeatherForecast>(daily.Time.Count);
        for (var i = 0; i < daily.Time.Count; i++)
        {
            var code = At(daily.WeatherCode, i) ?? -1;
            result.Add(new WeatherForecast
            {
                Date = DateOnly.Parse(daily.Time[i], CultureInfo.InvariantCulture),
                TemperatureMaxC = At(daily.TemperatureMax, i) ?? 0,
                TemperatureMinC = At(daily.TemperatureMin, i) ?? 0,
                WeatherCode = code,
                Summary = WeatherCodes.Describe(code),
                WindSpeedMaxKph = At(daily.WindSpeedMax, i) ?? 0,
                PrecipitationMm = At(daily.PrecipitationSum, i) ?? 0,
                PrecipitationProbability = At(daily.PrecipitationProbabilityMax, i) ?? 0,
            });
        }

        return result;
    }

    private static T? At<T>(List<T?> list, int index) where T : struct =>
        index < list.Count ? list[index] : null;
}
