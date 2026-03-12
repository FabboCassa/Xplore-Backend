namespace Xplore.Contracts.Friends;

/// <summary>
/// Request body to send a friend request.
/// </summary>
public record SendFriendRequestRequest(string AddresseeId);

/// <summary>
/// Response representing an accepted friend.
/// </summary>
public record FriendResponse(
    string UserId,
    string? DisplayName,
    bool HasAvatar,
    DateTime FriendsSince);

/// <summary>
/// Response representing an incoming pending friend request.
/// </summary>
public record FriendRequestResponse(
    Guid RequestId,
    string RequesterId,
    string? RequesterDisplayName,
    bool RequesterHasAvatar,
    DateTime SentAt);

/// <summary>
/// Response representing a user found in a search, including their friendship status with the current user.
/// </summary>
public record SearchUserResponse(
    string UserId,
    string? DisplayName,
    bool HasAvatar,
    string? FriendshipStatus);
