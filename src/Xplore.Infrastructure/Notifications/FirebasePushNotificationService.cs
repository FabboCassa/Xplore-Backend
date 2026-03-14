using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xplore.Infrastructure.Persistence;

namespace Xplore.Infrastructure.Notifications;

/// <summary>
/// Firebase Cloud Messaging implementation of <see cref="IPushNotificationService"/>.
/// </summary>
public class FirebasePushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<FirebasePushNotificationService> _logger;

    public FirebasePushNotificationService(
        ApplicationDbContext dbContext,
        ILogger<FirebasePushNotificationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SendToUserAsync(
        string userId,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        var tokens = await _dbContext.UserDeviceTokens
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .ToListAsync(ct);

        if (tokens.Count == 0)
        {
            _logger.LogDebug("No device tokens found for user {UserId}, skipping push", userId);
            return;
        }

        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new Notification
            {
                Title = title,
                Body = body,
            },
            Data = data,
        };

        var response = await FirebaseMessaging.DefaultInstance
            .SendEachForMulticastAsync(message, ct);

        if (response.FailureCount > 0)
        {
            var tokensToRemove = new List<string>();
            for (var i = 0; i < response.Responses.Count; i++)
            {
                if (!response.Responses[i].IsSuccess)
                {
                    var error = response.Responses[i].Exception?.MessagingErrorCode;
                    if (error is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                    {
                        tokensToRemove.Add(tokens[i]);
                    }
                    _logger.LogWarning(
                        "FCM send failed for token index {Index}: {Error}",
                        i, response.Responses[i].Exception?.Message);
                }
            }

            if (tokensToRemove.Count > 0)
            {
                var staleTokens = await _dbContext.UserDeviceTokens
                    .Where(t => tokensToRemove.Contains(t.Token))
                    .ToListAsync(ct);
                _dbContext.UserDeviceTokens.RemoveRange(staleTokens);
                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Removed {Count} stale device tokens for user {UserId}",
                    staleTokens.Count, userId);
            }
        }

        _logger.LogInformation(
            "Push notification sent to user {UserId}: {Success}/{Total} succeeded",
            userId, response.SuccessCount, tokens.Count);
    }
}
