using TestAPI.Services;

namespace TestAPI.Tests.Services;

public class WeatherCodesTests
{
    [Theory]
    [InlineData(0, "Clear sky")]
    [InlineData(3, "Overcast")]
    [InlineData(61, "Slight rain")]
    [InlineData(95, "Thunderstorm")]
    [InlineData(99, "Thunderstorm with heavy hail")]
    public void Describe_returns_text_for_known_codes(int code, string expected)
    {
        Assert.Equal(expected, WeatherCodes.Describe(code));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(1000)]
    public void Describe_returns_Unknown_for_unmapped_codes(int code)
    {
        Assert.Equal("Unknown", WeatherCodes.Describe(code));
    }
}
