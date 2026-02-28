namespace Xplore.Contracts.Map;

/// <summary>
/// Average loading time for a specific radius step, returned by the averages endpoint.
/// </summary>
public record RadiusLoadingAverage(
    double RadiusKm,
    long AvgMs
);
