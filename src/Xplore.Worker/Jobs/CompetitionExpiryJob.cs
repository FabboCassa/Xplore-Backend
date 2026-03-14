using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xplore.Contracts.Notifications;
using Xplore.Infrastructure.Persistence;

namespace Xplore.Worker.Jobs;

/// <summary>
/// Background job that runs every hour to deactivate expired competitions
/// and publish <see cref="CompetitionEndedEvent"/> for each.
/// </summary>
public class CompetitionExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CompetitionExpiryJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public CompetitionExpiryJob(
        IServiceScopeFactory scopeFactory,
        ILogger<CompetitionExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CompetitionExpiryJob started, checking every {Interval}", Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckExpiredCompetitions(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expired competitions");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CheckExpiredCompetitions(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var now = DateTime.UtcNow;

        var expiredCompetitions = await dbContext.Competitions
            .Where(c => c.IsActive && c.EndDate != null && c.EndDate <= now)
            .ToListAsync(ct);

        if (expiredCompetitions.Count == 0) return;

        _logger.LogInformation("Found {Count} expired competitions to deactivate", expiredCompetitions.Count);

        foreach (var competition in expiredCompetitions)
        {
            competition.IsActive = false;

            var memberUserIds = await dbContext.GroupMembers
                .Where(m => m.GroupId == competition.GroupId)
                .Select(m => m.UserId)
                .ToListAsync(ct);

            await publishEndpoint.Publish(new CompetitionEndedEvent(
                competition.Id,
                competition.Name,
                competition.GroupId,
                memberUserIds), ct);

            _logger.LogInformation(
                "Deactivated competition {CompId} '{CompName}', notifying {Count} members",
                competition.Id, competition.Name, memberUserIds.Count);
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
