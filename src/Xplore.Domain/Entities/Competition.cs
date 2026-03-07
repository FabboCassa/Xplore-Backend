using System;
using System.Collections.Generic;

namespace Xplore.Domain.Entities;

/// <summary>
/// Represents a competition created within a group to encourage exploration.
/// </summary>
public class Competition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The Group to which this competition belongs.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Name of the competition.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of competition, determining how the winner is selected.
    /// </summary>
    public CompetitionType Type { get; set; } = CompetitionType.MostPlaces;

    /// <summary>
    /// Optional date when the competition starts.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Optional date when the competition ends.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Whether the competition is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the competition was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Rules defining how points are awarded in this competition.
    /// </summary>
    public ICollection<CompetitionRule> Rules { get; set; } = new List<CompetitionRule>();
}
