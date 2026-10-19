using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using TestAPI.Options;
using TestAPI.Services;

namespace TestAPI.Tests.Support;

/// <summary>Builds service instances with stubbed HTTP and fresh in-memory caches.</summary>
public static class TestFactories
{
    public static readonly DateTimeOffset FixedNow = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    public static IOptions<OpenMeteoOptions> Options() => Microsoft.Extensions.Options.Options.Create(new OpenMeteoOptions());

    public static HttpClient Client(HttpMessageHandler handler, string baseAddress) =>
        new(handler) { BaseAddress = new Uri(baseAddress) };

    public static OpenMeteoGeocodingService Geocoding(StubHttpMessageHandler handler, IMemoryCache? cache = null) =>
        new(
            Client(handler, "https://geocoding.test/"),
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            Options(),
            NullLogger<OpenMeteoGeocodingService>.Instance);

    public static OpenMeteoWeatherForecastService Forecast(
        StubHttpMessageHandler handler,
        IGeocodingService geocoding,
        IMemoryCache? cache = null) =>
        new(
            Client(handler, "https://forecast.test/"),
            geocoding,
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            Options(),
            new FakeTimeProvider(FixedNow),
            NullLogger<OpenMeteoWeatherForecastService>.Instance);
}
