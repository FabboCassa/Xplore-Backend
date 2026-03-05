using System;

namespace Xplore.Domain.Entities;

/// <summary>
/// Represents the membership of a user in a community group.
/// </summary>
public class GroupMember
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public GroupRole Role { get; set; } = GroupRole.Member;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the group.
    /// </summary>
    public Group Group { get; set; } = null!;
}
