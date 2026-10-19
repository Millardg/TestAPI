namespace TestAPI.Tests.Support;

/// <summary>Trimmed, real Open-Meteo payloads captured on 2026-10-19.</summary>
public static class SampleResponses
{
    public const string GeocodeSeattle = """
        {"results":[{"id":5809844,"name":"Seattle","latitude":47.60621,"longitude":-122.33207,
        "country_code":"US","admin1":"Washington","country":"United States","timezone":"America/Los_Angeles"}],
        "generationtime_ms":0.6}
        """;

    /// <summary>Open-Meteo omits the <c>results</c> key entirely when nothing matches.</summary>
    public const string GeocodeNoMatch = """{"generationtime_ms":0.11515617}""";

    public const string ForecastSeattleTwoDays = """
        {"latitude":47.595562,"longitude":-122.32443,"timezone":"America/Los_Angeles",
        "daily_units":{"time":"iso8601","temperature_2m_max":"°C"},
        "daily":{
          "time":["2026-10-19","2026-10-20"],
          "temperature_2m_max":[13.7,17.0],
          "temperature_2m_min":[6.8,7.2],
          "weather_code":[3,61],
          "wind_speed_10m_max":[10.5,11.5],
          "precipitation_sum":[0.00,2.40],
          "precipitation_probability_max":[6,55]}}
        """;
}
