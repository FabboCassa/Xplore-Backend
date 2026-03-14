using MassTransit;
using Microsoft.Extensions.Logging;
using Xplore.Contracts.Notifications;
using Xplore.Infrastructure.Notifications;

namespace Xplore.Worker.Consumers;

/// <summary>
/// Sends a push notification when a user reaches a new explorer level.
/// </summary>
public class AchievementNotificationConsumer : IConsumer<AchievementReachedEvent>
{
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<AchievementNotificationConsumer> _logger;

    public AchievementNotificationConsumer(
        IPushNotificationService pushService,
        ILogger<AchievementNotificationConsumer> logger)
    {
        _pushService = pushService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AchievementReachedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation(
            "Achievement notification: user {UserId} reached level {Level}",
            msg.UserId, msg.NewLevel);

        await _pushService.SendToUserAsync(
            msg.UserId,
            "Livello raggiunto! 🎉",
            $"Sei al livello {msg.NewLevel}! Continua così",
            new Dictionary<string, string>
            {
                ["type"] = "achievement",
                ["level"] = msg.NewLevel.ToString(),
                ["totalScore"] = msg.TotalScore.ToString(),
            },
            context.CancellationToken);
    }
}
