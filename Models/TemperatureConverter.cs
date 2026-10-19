namespace TestAPI.Models;

/// <summary>Temperature unit conversions.</summary>
public static class TemperatureConverter
{
    /// <summary>Converts Celsius to Fahrenheit, rounded to one decimal place.</summary>
    public static double CelsiusToFahrenheit(double celsius) =>
        Math.Round(celsius * 9.0 / 5.0 + 32.0, 1);
}
