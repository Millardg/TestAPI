using TestAPI.Models;

namespace TestAPI.Tests.Models;

public class TemperatureConverterTests
{
    [Theory]
    [InlineData(0, 32)]
    [InlineData(100, 212)]
    [InlineData(-40, -40)]
    [InlineData(37, 98.6)]
    [InlineData(13.7, 56.7)]
    public void CelsiusToFahrenheit_matches_known_values(double celsius, double expectedF)
    {
        Assert.Equal(expectedF, TemperatureConverter.CelsiusToFahrenheit(celsius));
    }

    [Fact]
    public void Forecast_exposes_both_scales_consistently()
    {
        var forecast = new WeatherForecast { TemperatureMinC = -5, TemperatureMaxC = 20 };

        Assert.Equal(23, forecast.TemperatureMinF);
        Assert.Equal(68, forecast.TemperatureMaxF);
    }
}
