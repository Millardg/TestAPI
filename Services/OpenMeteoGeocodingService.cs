using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TestAPI.Models;
using TestAPI.Options;
using TestAPI.Services.OpenMeteo;

namespace TestAPI.Services;

/// <summary>
/// <see cref="IGeocodingService"/> backed by the Open-Meteo geocoding API.
/// Results are cached per normalised city name because places do not move.
/// </summary>
public sealed class OpenMeteoGeocodingService(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<OpenMeteoOptions> options,
    ILogger<OpenMeteoGeocodingService> logger) : IGeocodingService
{
    public async Task<Location?> FindCityAsync(string cityName, CancellationToken cancellationToken = default)
    {
        var normalised = cityName.Trim().ToLowerInvariant();
        var cacheKey = $"geocode:{normalised}";

        if (cache.TryGetValue(cacheKey, out Location? cached))
        {
            return cached;
        }

        var url = $"v1/search?name={Uri.EscapeDataString(normalised)}&count=1&language=en&format=json";
        logger.LogDebug("Geocoding {City} via {Url}", normalised, url);

        var response = await httpClient.GetFromJsonAsync<GeocodingResponse>(url, cancellationToken);
        var match = response?.Results?.FirstOrDefault();

        Location? location = match is null
            ? null
            : new Location
            {
                Name = match.Name,
                Region = match.Admin1,
                Country = match.Country ?? string.Empty,
                Latitude = match.Latitude,
                Longitude = match.Longitude,
                TimeZone = match.Timezone ?? "UTC",
            };

        // Cache misses as well, so repeated unknown names do not hammer the upstream.
        cache.Set(cacheKey, location, options.Value.GeocodingCacheDuration);
        return location;
    }
}
