namespace Xplore.Infrastructure.Notifications;

/// <summary>
/// Sends push notifications to a user's registered devices.
/// </summary>
public interface IPushNotificationService
{
    /// <summary>
    /// Send a push notification to all devices registered by the given user.
    /// </summary>
    Task SendToUserAsync(
        string userId,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default);
}
