namespace Xplore.Domain.Entities;

/// <summary>
/// Defines how users can join a group.
/// </summary>
public enum GroupAccessType
{
    /// <summary>
    /// Anyone can join freely.
    /// </summary>
    Public = 0,

    /// <summary>
    /// Requires a password to join.
    /// </summary>
    Password = 1,

    /// <summary>
    /// Only invited users can join. Group won't appear in search results.
    /// </summary>
    InviteOnly = 2
}
