namespace TestAPI.Models;

/// <summary>A multi-day forecast for a resolved location.</summary>
public sealed class CityForecast
{
    /// <summary>The location the forecast applies to.</summary>
    public required Location Location { get; init; }

    /// <summary>One entry per day, starting today in the location's time zone.</summary>
    public required IReadOnlyList<WeatherForecast> Days { get; init; }

    /// <summary>When this forecast was retrieved from the provider (UTC).</summary>
    public DateTimeOffset RetrievedAt { get; init; }
}
