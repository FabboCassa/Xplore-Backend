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

/// <summary>
/// A single member of a group.
/// </summary>
public record GroupMemberResponse(
    string UserId,
    string DisplayName,
    int Role,
    DateTime JoinedAt);

/// <summary>
/// Response with full group information, including members.
/// </summary>
public record GroupDetailResponse(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    string CreatedById,
    DateTime CreatedAt,
    int MemberCount,
    int AccessType,
    bool IsPasswordProtected,
    List<GroupMemberResponse> Members);

/// <summary>
/// Request to change a group's visibility.
/// </summary>
public record ChangeGroupVisibilityRequest(
    int AccessType,
    string? Password = null);

/// <summary>
/// Request to record a visited place.
/// </summary>
public record VisitPlaceRequest(string PlaceId);

/// <summary>
/// Request to create a new competition.
/// </summary>
public record CreateCompetitionRequest(
    string Name,
    int Type,
    DateTime? StartDate,
    DateTime? EndDate,
    List<CompetitionRuleRequest> Rules);

/// <summary>
/// Request to update an existing competition. Includes IsActive to allow manual deactivation.
/// </summary>
public record UpdateCompetitionRequest(
    string Name,
    int Type,
    DateTime? StartDate,
    DateTime? EndDate,
    bool IsActive,
    List<CompetitionRuleRequest> Rules);

/// <summary>
/// Request to create a new competition rule.
/// </summary>
public record CompetitionRuleRequest(
    int ActionType,
    int PointsAwarded,
    string? TargetPlaceId);

/// <summary>
/// Response with competition information.
/// </summary>
public record CompetitionResponse(
    Guid Id,
    Guid GroupId,
    string Name,
    int Type,
    DateTime? StartDate,
    DateTime? EndDate,
    bool IsActive,
    DateTime CreatedAt,
    List<CompetitionRuleResponse> Rules);

/// <summary>
/// Response with competition rule information.
/// </summary>
public record CompetitionRuleResponse(
    Guid Id,
    int ActionType,
    int PointsAwarded,
    string? TargetPlaceId);

/// <summary>
/// A single entry in a competition leaderboard.
/// </summary>
public record CompetitionLeaderboardEntryResponse(
    string UserId,
    string? DisplayName,
    int Points,
    int Rank);
