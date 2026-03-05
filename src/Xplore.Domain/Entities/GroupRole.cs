namespace Xplore.Domain.Entities;

/// <summary>
/// Defines the role of a user within a group.
/// </summary>
public enum GroupRole
{
    /// <summary>
    /// Regular group member.
    /// </summary>
    Member = 0,

    /// <summary>
    /// Group administrator (creator or promoted).
    /// </summary>
    Admin = 1
}
