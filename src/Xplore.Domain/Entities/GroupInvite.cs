namespace Xplore.Domain.Entities;

/// <summary>
/// Represents an invitation for a user to join a group.
/// </summary>
public class GroupInvite
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid GroupId { get; init; }

    /// <summary>User who sent the invitation (must be group admin).</summary>
    public string InviterId { get; init; } = null!;

    /// <summary>User being invited.</summary>
    public string InviteeId { get; init; } = null!;

    public GroupInviteStatus Status { get; set; } = GroupInviteStatus.Pending;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    // Navigation
    public Group Group { get; init; } = null!;
}

/// <summary>
/// Status of a group invitation.
/// </summary>
public enum GroupInviteStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
}
