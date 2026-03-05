using CoachOS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CoachOS.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendTrainerInviteAsync(string toEmail, string firstName, string inviteUrl, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "=== TRAINER INVITE ===\nAan: {Email}\nNaam: {FirstName}\nURL: {InviteUrl}\n======================",
            toEmail, firstName, inviteUrl);

        return Task.CompletedTask;
    }
}
