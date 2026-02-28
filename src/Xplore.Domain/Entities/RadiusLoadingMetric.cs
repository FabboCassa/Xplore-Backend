namespace Xplore.Domain.Entities;

/// <summary>
/// Technical table: stores individual POI loading time measurements per radius step.
/// Used to compute average loading times shown to users in the settings dialog.
/// </summary>
public class RadiusLoadingMetric
{
    public Guid Id { get; set; }

    /// <summary>Radius in km (e.g. 0.1, 0.5, 3.0, 10.0).</summary>
    public double RadiusKm { get; set; }

    /// <summary>Measured loading time in milliseconds.</summary>
    public long LoadingTimeMs { get; set; }

    /// <summary>When this measurement was recorded (UTC).</summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
