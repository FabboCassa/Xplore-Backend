namespace Xplore.Domain.Entities;

/// <summary>
/// Represents a friend request or established friendship between two users.
/// </summary>
public class Friendship
{
    public Guid Id { get; set; }

    /// <summary>
    /// The user who sent the friend request.
    /// </summary>
    public string RequesterId { get; set; } = string.Empty;

    /// <summary>
    /// The user who received the friend request.
    /// </summary>
    public string AddresseeId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the friendship.
    /// </summary>
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
