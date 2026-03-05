using System;
using System.Collections.Generic;

namespace Xplore.Domain.Entities;

/// <summary>
/// Represents a community group that users can create and join.
/// </summary>
public class Group
{
    public Guid Id { get; set; }

    /// <summary>
    /// Display name of the group.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the group's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional image/avatar URL for the group.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// How users can join: Public, Password, or InviteOnly.
    /// </summary>
    public GroupAccessType AccessType { get; set; } = GroupAccessType.Public;

    /// <summary>
    /// Hashed password for Password-protected groups. Null otherwise.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// The user who created this group.
    /// </summary>
    public string CreatedById { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for group members.
    /// </summary>
    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
}

