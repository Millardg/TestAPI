namespace TestAPI.Models;

/// <summary>A single day's forecast for a location.</summary>
public sealed class WeatherForecast
{
    /// <summary>Calendar date of the forecast in the location's local time zone.</summary>
    public DateOnly Date { get; init; }

    /// <summary>Forecast daily low in degrees Celsius.</summary>
    public double TemperatureMinC { get; init; }

    /// <summary>Forecast daily high in degrees Celsius.</summary>
    public double TemperatureMaxC { get; init; }

    /// <summary>Forecast daily low in degrees Fahrenheit.</summary>
    public double TemperatureMinF => TemperatureConverter.CelsiusToFahrenheit(TemperatureMinC);

    /// <summary>Forecast daily high in degrees Fahrenheit.</summary>
    public double TemperatureMaxF => TemperatureConverter.CelsiusToFahrenheit(TemperatureMaxC);

    /// <summary>WMO weather interpretation code reported by the provider.</summary>
    public int WeatherCode { get; init; }

    /// <summary>Human-readable description of <see cref="WeatherCode"/>.</summary>
    public string Summary { get; init; } = string.Empty;

    /// <summary>Maximum sustained wind speed at 10 m, in km/h.</summary>
    public double WindSpeedMaxKph { get; init; }

    /// <summary>Total precipitation for the day, in millimetres.</summary>
    public double PrecipitationMm { get; init; }

    /// <summary>Highest hourly probability of precipitation for the day, as a percentage.</summary>
    public int PrecipitationProbability { get; init; }
}
