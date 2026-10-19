using System.Text.Json.Serialization;

namespace TestAPI.Services.OpenMeteo;

/// <summary>Wire format of <c>GET /v1/forecast</c> on the Open-Meteo forecast API (daily block only).</summary>
internal sealed class ForecastResponse
{
    [JsonPropertyName("timezone")]
    public string? Timezone { get; set; }

    [JsonPropertyName("daily")]
    public DailyBlock? Daily { get; set; }
}

/// <summary>Open-Meteo returns daily data as parallel arrays, one per variable, indexed by day.</summary>
internal sealed class DailyBlock
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = [];

    [JsonPropertyName("temperature_2m_max")]
    public List<double?> TemperatureMax { get; set; } = [];

    [JsonPropertyName("temperature_2m_min")]
    public List<double?> TemperatureMin { get; set; } = [];

    [JsonPropertyName("weather_code")]
    public List<int?> WeatherCode { get; set; } = [];

    [JsonPropertyName("wind_speed_10m_max")]
    public List<double?> WindSpeedMax { get; set; } = [];

    [JsonPropertyName("precipitation_sum")]
    public List<double?> PrecipitationSum { get; set; } = [];

    [JsonPropertyName("precipitation_probability_max")]
    public List<int?> PrecipitationProbabilityMax { get; set; } = [];
}
