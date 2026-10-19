using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TestAPI.Models;
using TestAPI.Services;

namespace TestAPI.Tests.Controllers;

/// <summary>
/// Boots the real pipeline (routing, validation, ProblemDetails, exception handler) with the
/// forecast service swapped for a scripted fake, so no network calls are made.
/// </summary>
public class WeatherForecastEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WeatherForecastEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private sealed class ScriptedForecastService(Func<string, int, Task<CityForecast?>> script) : IWeatherForecastService
    {
        public Task<CityForecast?> GetForecastAsync(string cityName, int days, CancellationToken cancellationToken = default) =>
            script(cityName, days);
    }

    private HttpClient CreateClient(Func<string, int, Task<CityForecast?>> script) =>
        _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IWeatherForecastService>();
                services.AddSingleton<IWeatherForecastService>(new ScriptedForecastService(script));
            });
        }).CreateClient();

    private static CityForecast SampleForecast(string city, int days) => new()
    {
        Location = new Location { Name = city, Country = "Testland", Latitude = 1, Longitude = 2, TimeZone = "UTC" },
        Days = Enumerable.Range(0, days).Select(i => new WeatherForecast
        {
            Date = new DateOnly(2026, 10, 19).AddDays(i),
            TemperatureMinC = 5 + i,
            TemperatureMaxC = 15 + i,
            WeatherCode = 1,
            Summary = "Mainly clear",
        }).ToList(),
        RetrievedAt = DateTimeOffset.UnixEpoch,
    };

    [Fact]
    public async Task Returns_200_with_forecast_for_known_city()
    {
        var client = CreateClient((city, days) => Task.FromResult<CityForecast?>(SampleForecast(city, days)));

        var response = await client.GetAsync("/api/weatherforecast/Seattle?days=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CityForecast>();
        Assert.NotNull(body);
        Assert.Equal("Seattle", body.Location.Name);
        Assert.Equal(3, body.Days.Count);
        Assert.Equal(59, body.Days[0].TemperatureMaxF);
    }

    [Fact]
    public async Task Defaults_to_seven_days_when_days_is_omitted()
    {
        int? requestedDays = null;
        var client = CreateClient((city, days) =>
        {
            requestedDays = days;
            return Task.FromResult<CityForecast?>(SampleForecast(city, days));
        });

        await client.GetAsync("/api/weatherforecast/Seattle");

        Assert.Equal(7, requestedDays);
    }

    [Fact]
    public async Task Returns_404_problem_when_city_is_unknown()
    {
        var client = CreateClient((_, _) => Task.FromResult<CityForecast?>(null));

        var response = await client.GetAsync("/api/weatherforecast/Xyzzyqq");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("City not found", problem.Title);
        Assert.Contains("Xyzzyqq", problem.Detail);
    }

    [Theory]
    [InlineData("/api/weatherforecast/Seattle?days=0", "days")]
    [InlineData("/api/weatherforecast/Seattle?days=17", "days")]
    [InlineData("/api/weatherforecast/Seattle?days=abc", "days")]
    [InlineData("/api/weatherforecast/X", "city")]
    public async Task Returns_400_validation_problem_for_bad_input(string url, string expectedField)
    {
        var client = CreateClient((_, _) => throw new InvalidOperationException("service should not be reached"));

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains(expectedField, problem.Errors.Keys);
    }

    [Fact]
    public async Task Returns_502_problem_when_provider_is_unreachable()
    {
        var client = CreateClient((_, _) => throw new HttpRequestException("boom"));

        var response = await client.GetAsync("/api/weatherforecast/Seattle");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Weather provider unavailable", problem.Title);
    }

    [Fact]
    public async Task Returns_500_problem_without_leaking_details_for_unexpected_errors()
    {
        var client = CreateClient((_, _) => throw new InvalidOperationException("secret internal state"));

        var response = await client.GetAsync("/api/weatherforecast/Seattle");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret internal state", raw);
    }
}
