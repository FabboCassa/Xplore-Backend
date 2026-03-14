using MassTransit;
using Microsoft.Extensions.Logging;
using Xplore.Contracts.Notifications;
using Xplore.Infrastructure.Notifications;

namespace Xplore.Worker.Consumers;

/// <summary>
/// Sends a push notification when a user is invited to a group.
/// </summary>
public class GroupInviteNotificationConsumer : IConsumer<GroupInviteReceivedEvent>
{
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<GroupInviteNotificationConsumer> _logger;

    public GroupInviteNotificationConsumer(
        IPushNotificationService pushService,
        ILogger<GroupInviteNotificationConsumer> logger)
    {
        _pushService = pushService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GroupInviteReceivedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "Group invite notification: {InviterId} invited {InviteeId} to group {GroupId}",
            msg.InviterId, msg.InviteeId, msg.GroupId);

        await _pushService.SendToUserAsync(
            msg.InviteeId,
            "Invito al gruppo",
            $"{msg.InviterDisplayName} ti ha invitato in {msg.GroupName}",
            new Dictionary<string, string>
            {
                ["type"] = "group_invite",
                ["inviteId"] = msg.InviteId.ToString(),
                ["groupId"] = msg.GroupId.ToString(),
            },
            context.CancellationToken);
    }
}
