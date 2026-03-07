using System;

namespace Xplore.Domain.Entities;

/// <summary>
/// Represents a place visited by a user globally.
/// </summary>
public class VisitedPlace
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The ID of the user who visited the place.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The external ID of the place or museum (e.g., from an external API or our own data source).
    /// </summary>
    public string PlaceId { get; set; } = string.Empty;

    /// <summary>
    /// When the user visited the place.
    /// </summary>
    public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
}
