using System.ComponentModel.DataAnnotations;

namespace TestAPI.Options;

/// <summary>Configuration for the Open-Meteo upstream APIs. Bound from the <c>OpenMeteo</c> section.</summary>
public sealed class OpenMeteoOptions
{
    public const string SectionName = "OpenMeteo";

    [Required, Url]
    public string GeocodingBaseUrl { get; set; } = "https://geocoding-api.open-meteo.com/";

    [Required, Url]
    public string ForecastBaseUrl { get; set; } = "https://api.open-meteo.com/";

    /// <summary>How long a resolved city lookup is cached. Cities do not move, so this can be long.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "7.00:00:00")]
    public TimeSpan GeocodingCacheDuration { get; set; } = TimeSpan.FromHours(24);

    /// <summary>How long a forecast is cached before Open-Meteo is called again.</summary>
    [Range(typeof(TimeSpan), "00:00:30", "06:00:00")]
    public TimeSpan ForecastCacheDuration { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Upstream request timeout.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
