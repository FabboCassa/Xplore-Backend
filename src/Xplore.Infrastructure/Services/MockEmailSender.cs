namespace Xplore.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xplore.Application.Services;

public class MockEmailSender : IEmailSender
{
    private readonly ILogger<MockEmailSender> _logger;

    public MockEmailSender(ILogger<MockEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogWarning("MOCK EMAIL SENT TO {Email}: Subject: {Subject} | Body: {Body}", email, subject, htmlMessage);
        return Task.CompletedTask;
    }
}
