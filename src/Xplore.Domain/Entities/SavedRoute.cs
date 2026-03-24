using System;
using System.Collections.Generic;

namespace Xplore.Domain.Entities;

/// <summary>
/// Represents a route created and saved by a user.
/// </summary>
public class SavedRoute
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The ID of the user who created this route.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the route.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the route.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the user has completed this route.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Unique token used to share this route via link.
    /// </summary>
    public string ShareToken { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// When the route was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Ordered waypoints that form this route.
    /// </summary>
    public List<SavedRouteWaypoint> Waypoints { get; set; } = new();
}
