using TestAPI.Models;

namespace TestAPI.Services;

/// <summary>Provides multi-day weather forecasts by city name.</summary>
public interface IWeatherForecastService
{
    /// <summary>Smallest number of days a caller may request.</summary>
    public const int MinDays = 1;

    /// <summary>Largest number of days a caller may request (the Open-Meteo limit).</summary>
    public const int MaxDays = 16;

    /// <summary>
    /// Gets a <paramref name="days"/>-day forecast for <paramref name="cityName"/>.
    /// Returns <c>null</c> when the city cannot be resolved.
    /// </summary>
    Task<CityForecast?> GetForecastAsync(string cityName, int days, CancellationToken cancellationToken = default);
}
