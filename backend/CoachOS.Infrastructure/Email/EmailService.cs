using System.Net;
using System.Net.Mail;
using CoachOS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendTrainerInviteAsync(
        string toEmail,
        string firstName,
        string inviteUrl,
        CancellationToken ct = default)
    {
        // Derive org name from the invite URL host for now (will be passed explicitly later)
        (string subject, string html) = EmailTemplates.TrainerInvite(firstName, "CoachOS", inviteUrl);
        await SendAsync(toEmail, $"{firstName}", subject, html, ct);
    }

    // ── Core send method (reused by all future email types) ───────────────────

    private async Task SendAsync(
        string toEmail,
        string toName,
        string subject,
        string htmlBody,
        CancellationToken ct = default)
    {
        using SmtpClient smtp = new(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        using MailMessage message = new()
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(new MailAddress(toEmail, toName));

        try
        {
            await smtp.SendMailAsync(message, ct);
            _logger.LogInformation("E-mail verstuurd naar {Email} — onderwerp: {Subject}", toEmail, subject);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "Versturen mislukt naar {Email} — {Message}", toEmail, ex.Message);
            throw;
        }
    }
}
