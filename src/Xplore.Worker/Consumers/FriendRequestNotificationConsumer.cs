using MassTransit;
using Microsoft.Extensions.Logging;
using Xplore.Contracts.Notifications;
using Xplore.Infrastructure.Notifications;

namespace Xplore.Worker.Consumers;

/// <summary>
/// Sends a push notification when a friend request is received.
/// </summary>
public class FriendRequestNotificationConsumer : IConsumer<FriendRequestReceivedEvent>
{
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<FriendRequestNotificationConsumer> _logger;

    public FriendRequestNotificationConsumer(
        IPushNotificationService pushService,
        ILogger<FriendRequestNotificationConsumer> logger)
    {
        _pushService = pushService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<FriendRequestReceivedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "Friend request notification: {RequesterId} → {AddresseeId}",
            msg.RequesterId, msg.AddresseeId);

        await _pushService.SendToUserAsync(
            msg.AddresseeId,
            "Nuova richiesta di amicizia",
            $"{msg.RequesterDisplayName} vuole essere tuo amico",
            new Dictionary<string, string>
            {
                ["type"] = "friend_request",
                ["friendshipId"] = msg.FriendshipId.ToString(),
            },
            context.CancellationToken);
    }
}
