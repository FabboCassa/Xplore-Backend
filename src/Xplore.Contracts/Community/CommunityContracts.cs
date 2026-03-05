namespace Xplore.Contracts.Community;

/// <summary>
/// Request to create a new community group.
/// </summary>
public record CreateGroupRequest(
    string Name,
    string? Description,
    string? ImageUrl,
    int AccessType = 0,
    string? Password = null);

/// <summary>
/// Response with group information.
/// </summary>
public record GroupResponse(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    string CreatedById,
    DateTime CreatedAt,
    int MemberCount,
    int AccessType,
    bool IsPasswordProtected);

/// <summary>
/// A single entry in the explorer leaderboard.
/// </summary>
public record LeaderboardEntry(
    string UserId,
    string? DisplayName,
    int VisitedPlacesCount,
    int GroupVictories,
    int CommunityContributions,
    int TotalScore,
    int Rank);

/// <summary>
/// Request to join a password-protected group.
/// </summary>
public record JoinGroupRequest(string? Password = null);
