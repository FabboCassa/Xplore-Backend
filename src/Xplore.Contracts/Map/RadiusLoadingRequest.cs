namespace Xplore.Contracts.Map;

/// <summary>
/// Request body sent by the mobile app after each POI loading operation.
/// </summary>
public record RadiusLoadingRequest(
    double RadiusKm,
    long LoadingTimeMs
);
