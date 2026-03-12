namespace Xplore.Domain.Entities;

/// <summary>
/// Defines the current state of a friendship between two users.
/// </summary>
public enum FriendshipStatus
{
    /// <summary>
    /// Request sent, waiting for the addressee to respond.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Both users are friends.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Request was rejected or cancelled.
    /// </summary>
    Rejected = 2
}
