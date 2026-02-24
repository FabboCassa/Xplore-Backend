namespace Xplore.Contracts.Map;

/// <summary>
/// Response DTO for a map point of interest, returned by the POI proxy endpoint.
/// Matches the mobile app's MapPinDto format with enriched data.
/// </summary>
public record MapPinResponse(
    string Id,
    string Label,
    double Latitude,
    double Longitude,
    string Type,
    string? Description,
    string? Category,
    string? ImageUrl
);
