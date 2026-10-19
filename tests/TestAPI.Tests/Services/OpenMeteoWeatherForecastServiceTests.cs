using System.Net;
using System.Text.Json;
using TestAPI.Models;
using TestAPI.Services;
using TestAPI.Services.OpenMeteo;
using TestAPI.Tests.Support;

namespace TestAPI.Tests.Services;

public class OpenMeteoWeatherForecastServiceTests
{
    private static readonly Location Seattle = new()
    {
        Name = "Seattle",
        Region = "Washington",
        Country = "United States",
        Latitude = 47.60621,
        Longitude = -122.33207,
        TimeZone = "America/Los_Angeles",
    };

    private sealed class FixedGeocoding(Location? result) : IGeocodingService
    {
        public int Calls { get; private set; }

        public Task<Location?> FindCityAsync(string cityName, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(result);
        }
    }

    [Fact]
    public async Task Returns_null_without_calling_forecast_api_when_city_is_unknown()
    {
        var handler = StubHttpMessageHandler.Json(SampleResponses.ForecastSeattleTwoDays);
        var service = TestFactories.Forecast(handler, new FixedGeocoding(null));

        var result = await service.GetForecastAsync("Nowhere", 7);

        Assert.Null(result);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Maps_provider_payload_into_one_entry_per_day()
    {
        var handler = StubHttpMessageHandler.Json(SampleResponses.ForecastSeattleTwoDays);
        var service = TestFactories.Forecast(handler, new FixedGeocoding(Seattle));

        var result = await service.GetForecastAsync("Seattle", 2);

        Assert.NotNull(result);
        Assert.Same(Seattle, result.Location);
        Assert.Equal(TestFactories.FixedNow, result.RetrievedAt);
        Assert.Equal(2, result.Days.Count);

        var first = result.Days[0];
        Assert.Equal(new DateOnly(2026, 10, 19), first.Date);
        Assert.Equal(13.7, first.TemperatureMaxC);
        Assert.Equal(6.8, first.TemperatureMinC);
        Assert.Equal(3, first.WeatherCode);
        Assert.Equal("Overcast", first.Summary);
        Assert.Equal(10.5, first.WindSpeedMaxKph);
        Assert.Equal(0, first.PrecipitationMm);
        Assert.Equal(6, first.PrecipitationProbability);

        var second = result.Days[1];
        Assert.Equal("Slight rain", second.Summary);
        Assert.Equal(2.4, second.PrecipitationMm);
        Assert.Equal(55, second.PrecipitationProbability);
    }

    [Fact]
    public async Task Requests_the_expected_daily_variables_and_day_count()
    {
        var handler = StubHttpMessageHandler.Json(SampleResponses.ForecastSeattleTwoDays);
        var service = TestFactories.Forecast(handler, new FixedGeocoding(Seattle));

        await service.GetForecastAsync("Seattle", 5);

        var query = Assert.Single(handler.Requests).Query;
        Assert.Contains("latitude=47.60621", query);
        Assert.Contains("longitude=-122.33207", query);
        Assert.Contains("forecast_days=5", query);
        Assert.Contains("timezone=auto", query);
        Assert.Contains("temperature_2m_max", query);
        Assert.Contains("weather_code", query);
    }

    [Fact]
    public async Task Caches_forecasts_per_location_and_day_count()
    {
        var handler = StubHttpMessageHandler.Json(SampleResponses.ForecastSeattleTwoDays);
        var service = TestFactories.Forecast(handler, new FixedGeocoding(Seattle));

        await service.GetForecastAsync("Seattle", 2);
        await service.GetForecastAsync("Seattle", 2);
        await service.GetForecastAsync("Seattle", 3);

        Assert.Equal(2, handler.Requests.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public async Task Rejects_day_counts_outside_the_supported_range(int days)
    {
        var handler = StubHttpMessageHandler.Json(SampleResponses.ForecastSeattleTwoDays);
        var service = TestFactories.Forecast(handler, new FixedGeocoding(Seattle));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetForecastAsync("Seattle", days));
    }

    [Fact]
    public async Task Propagates_HttpRequestException_when_provider_fails()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.BadGateway, "{}");
        var service = TestFactories.Forecast(handler, new FixedGeocoding(Seattle));

        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetForecastAsync("Seattle", 2));
    }

    [Fact]
    public void MapDays_tolerates_short_or_missing_variable_arrays()
    {
        var daily = JsonSerializer.Deserialize<DailyBlock>("""
            {"time":["2026-10-19","2026-10-20"],"temperature_2m_max":[10.0],"weather_code":[null, 999]}
            """)!;

        var days = OpenMeteoWeatherForecastService.MapDays(daily);

        Assert.Equal(2, days.Count);
        Assert.Equal(10.0, days[0].TemperatureMaxC);
        Assert.Equal(0, days[1].TemperatureMaxC);
        Assert.Equal("Unknown", days[0].Summary);
        Assert.Equal(999, days[1].WeatherCode);
        Assert.Equal("Unknown", days[1].Summary);
    }

    [Fact]
    public void MapDays_returns_empty_list_for_missing_daily_block()
    {
        Assert.Empty(OpenMeteoWeatherForecastService.MapDays(null));
    }
}
