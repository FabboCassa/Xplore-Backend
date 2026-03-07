using System;

namespace Xplore.Domain.Entities;

/// <summary>
/// A rule governing how points are distributed for a specific competition.
/// </summary>
public class CompetitionRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The Competition this rule belongs to.
    /// </summary>
    public Guid CompetitionId { get; set; }

    /// <summary>
    /// Navigation property to the Competition.
    /// </summary>
    public Competition Competition { get; set; } = null!;

    /// <summary>
    /// The type of action that triggers this rule.
    /// </summary>
    public CompetitionActionType ActionType { get; set; } = CompetitionActionType.VisitPlace;

    /// <summary>
    /// Points awarded when the action is completed.
    /// </summary>
    public int PointsAwarded { get; set; } = 1;

    /// <summary>
    /// Optional target place ID. Useful for ScavengerHunt and FastestToPreset types 
    /// where users need to visit specific locations.
    /// </summary>
    public string? TargetPlaceId { get; set; }
}
