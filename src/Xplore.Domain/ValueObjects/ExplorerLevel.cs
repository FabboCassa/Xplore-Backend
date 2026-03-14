namespace Xplore.Domain.ValueObjects;

/// <summary>
/// Determines the explorer level based on accumulated points.
/// Thresholds must stay in sync with the mobile client.
/// </summary>
public static class ExplorerLevel
{
    private static readonly int[] Thresholds = [0, 50, 150, 300, 500, 800, 1200, 1800, 2600, 3500];

    /// <summary>
    /// Returns the 1-based level for the given point total.
    /// </summary>
    public static int LevelForPoints(int points) =>
        Thresholds.Count(t => points >= t);
}
