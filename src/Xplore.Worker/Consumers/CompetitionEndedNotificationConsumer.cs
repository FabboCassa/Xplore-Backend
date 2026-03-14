using MassTransit;
using Microsoft.Extensions.Logging;
using Xplore.Contracts.Notifications;
using Xplore.Infrastructure.Notifications;

namespace Xplore.Worker.Consumers;

/// <summary>
/// Sends push notifications to all group members when a competition ends.
/// </summary>
public class CompetitionEndedNotificationConsumer : IConsumer<CompetitionEndedEvent>
{
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<CompetitionEndedNotificationConsumer> _logger;

    public CompetitionEndedNotificationConsumer(
        IPushNotificationService pushService,
        ILogger<CompetitionEndedNotificationConsumer> logger)
    {
        _pushService = pushService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CompetitionEndedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "Competition ended notification: {CompetitionId} in group {GroupId}, notifying {Count} members",
            msg.CompetitionId, msg.GroupId, msg.MemberUserIds.Count);

        var data = new Dictionary<string, string>
        {
            ["type"] = "competition_ended",
            ["competitionId"] = msg.CompetitionId.ToString(),
            ["groupId"] = msg.GroupId.ToString(),
        };

        foreach (var userId in msg.MemberUserIds)
        {
            await _pushService.SendToUserAsync(
                userId,
                "Competizione terminata",
                $"La competizione '{msg.CompetitionName}' è finita. Vedi i risultati!",
                data,
                context.CancellationToken);
        }
    }
}
