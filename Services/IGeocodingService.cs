using TestAPI.Models;

namespace TestAPI.Services;

/// <summary>Resolves free-text place names to coordinates.</summary>
public interface IGeocodingService
{
    /// <summary>
    /// Finds the best match for <paramref name="cityName"/>.
    /// Returns <c>null</c> when the geocoder has no match.
    /// </summary>
    Task<Location?> FindCityAsync(string cityName, CancellationToken cancellationToken = default);
}
