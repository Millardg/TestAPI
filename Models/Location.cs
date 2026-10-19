namespace TestAPI.Models;

/// <summary>A resolved place name with coordinates.</summary>
public sealed class Location
{
    /// <summary>Canonical place name as returned by the geocoder.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>First-level administrative region (state, province, etc.), when known.</summary>
    public string? Region { get; init; }

    /// <summary>Country name.</summary>
    public string Country { get; init; } = string.Empty;

    /// <summary>Latitude in decimal degrees.</summary>
    public double Latitude { get; init; }

    /// <summary>Longitude in decimal degrees.</summary>
    public double Longitude { get; init; }

    /// <summary>IANA time zone identifier, e.g. <c>America/Los_Angeles</c>.</summary>
    public string TimeZone { get; init; } = string.Empty;
}
