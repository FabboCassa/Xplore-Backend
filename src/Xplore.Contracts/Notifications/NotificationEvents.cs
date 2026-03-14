namespace Xplore.Contracts.Notifications;

/// <summary>
/// Published when a user receives a friend request.
/// </summary>
public record FriendRequestReceivedEvent(
    Guid FriendshipId,
    string RequesterId,
    string RequesterDisplayName,
    string AddresseeId);

/// <summary>
/// Published when a user reaches a new explorer level.
/// </summary>
public record AchievementReachedEvent(
    string UserId,
    int NewLevel,
    int TotalScore);

/// <summary>
/// Published when a user is invited to join a group.
/// </summary>
public record GroupInviteReceivedEvent(
    Guid InviteId,
    Guid GroupId,
    string GroupName,
    string InviterId,
    string InviterDisplayName,
    string InviteeId);

/// <summary>
/// Published when a competition ends (manually or by scheduled job).
/// </summary>
public record CompetitionEndedEvent(
    Guid CompetitionId,
    string CompetitionName,
    Guid GroupId,
    List<string> MemberUserIds);
