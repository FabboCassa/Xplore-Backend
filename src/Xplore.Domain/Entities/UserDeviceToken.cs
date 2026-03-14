namespace Xplore.Domain.Entities;

/// <summary>
/// Stores an FCM device token for push notifications.
/// </summary>
public class UserDeviceToken
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>User ID from ASP.NET Identity.</summary>
    public string UserId { get; init; } = null!;

    /// <summary>FCM registration token.</summary>
    public string Token { get; init; } = null!;

    /// <summary>Platform identifier: "android" or "ios".</summary>
    public string Platform { get; init; } = null!;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
