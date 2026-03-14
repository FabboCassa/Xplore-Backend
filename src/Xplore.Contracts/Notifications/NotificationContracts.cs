namespace Xplore.Contracts.Notifications;

/// <summary>
/// Request to register an FCM device token for push notifications.
/// </summary>
public record RegisterDeviceTokenRequest(string Token, string Platform);

/// <summary>
/// Request to send a group invitation.
/// </summary>
public record SendGroupInviteRequest(string InviteeUserId);

/// <summary>
/// Response representing a pending group invitation.
/// </summary>
public record GroupInviteResponse(
    Guid Id,
    Guid GroupId,
    string GroupName,
    string InvitedByUserId,
    string? InvitedByDisplayName,
    string InvitedUserId,
    int Status,
    DateTime CreatedAt);
