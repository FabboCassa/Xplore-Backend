using System;

namespace Xplore.Domain.Entities;

/// <summary>
/// Represents a single waypoint/stop within a saved route.
/// </summary>
public class SavedRouteWaypoint
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The route this waypoint belongs to.
    /// </summary>
    public Guid SavedRouteId { get; set; }

    /// <summary>
    /// Navigation property to the parent route.
    /// </summary>
    public SavedRoute SavedRoute { get; set; } = null!;

    /// <summary>
    /// External place identifier (e.g., from Overpass/OSM).
    /// </summary>
    public string PlaceId { get; set; } = string.Empty;

    /// <summary>
    /// Display name for this waypoint.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Latitude coordinate.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Order of this waypoint within the route (0-based).
    /// </summary>
    public int OrderIndex { get; set; }
}
