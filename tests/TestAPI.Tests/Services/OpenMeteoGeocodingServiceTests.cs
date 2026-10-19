using System.Net;
using TestAPI.Tests.Support;

namespace TestAPI.Tests.Services;

public class OpenMeteoGeocodingServiceTests
{
    [Fact]
    public async Task Returns_mapped_location_for_a_match()
    {
        var handler = StubHttpMessageHandler.Json(SampleResponses.GeocodeSeattle);
        var service = TestFactories.Geocoding(handler);

        var location = await service.FindCityAsync("Seattle");

        Assert.NotNull(location);
        Assert.Equal("Seattle", location.Name);
        Assert.Equal("Washington", location.Region);
        Assert.Equal("United States", location.Country);
        Assert.Equal(47.60621, location.Latitude);
        Assert.Equal(-122.33207, location.Longitude);
        Assert.Equal("America/Los_Angeles", location.TimeZone);
    }

    [Fact]
    public async Task Returns_null_when_provider_has_no_results()
    {
        var handler = StubHttpMessageHandler.Json(SampleResponses.GeocodeNoMatch);
        var service = TestFactories.Geocoding(handler);

        var location = await service.FindCityAsync("Xyzzyqq");

        Assert.Null(location);
    }

    [Fact]
    public async Task Normalises_and_escapes_the_city_in_the_query()
    {
        var handler = StubHttpMessageHandler.Json(SampleResponses.GeocodeSeattle);
        var service = TestFactories.Geocoding(handler);

        await service.FindCityAsync("  New York ");

        var request = Assert.Single(handler.Requests);
        Assert.Contains("name=new%20york", request.Query);
        Assert.Contains("count=1", request.Query);
    }

    [Fact]
    public async Task Caches_results_so_repeat_lookups_do_not_hit_the_network()
    {
        var handler = StubHttpMessageHandler.Json(SampleResponses.GeocodeSeattle);
        var service = TestFactories.Geocoding(handler);

        await service.FindCityAsync("Seattle");
        await service.FindCityAsync("seattle");
        await service.FindCityAsync("SEATTLE ");

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Throws_HttpRequestException_when_provider_fails()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "{}");
        var service = TestFactories.Geocoding(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => service.FindCityAsync("Seattle"));
    }
}
