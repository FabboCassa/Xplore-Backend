namespace Xplore.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using Xplore.Application.Common.Interfaces;

public class DummyEmailSender : IEmailSender
{
    private readonly ILogger<DummyEmailSender> _logger;

    public DummyEmailSender(ILogger<DummyEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation("--- DUMMY EMAIL SENDER ---");
        _logger.LogInformation("To: {Email}", email);
        _logger.LogInformation("Subject: {Subject}", subject);
        _logger.LogInformation("Body: {HtmlMessage}", htmlMessage);
        _logger.LogInformation("--------------------------");

        return Task.CompletedTask;
    }
}
