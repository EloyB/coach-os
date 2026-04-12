using System.Net;
using System.Net.Mail;
using CoachOS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Email;

public class EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
    : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendTrainerInviteAsync(
        string toEmail,
        string firstName,
        string inviteUrl,
        CancellationToken ct = default)
    {
        (var subject, var html) = EmailTemplates.TrainerInvite(firstName, "CoachOS", inviteUrl);
        await SendAsync(toEmail, $"{firstName}", subject, html, ct);
    }

    public async Task SendEnrollmentConfirmationAsync(
        string studentEmail,
        string studentName,
        string seriesName,
        string trainerName,
        CancellationToken ct = default)
    {
        (var subject, var html) = EmailTemplates.EnrollmentConfirmation(studentName, seriesName, trainerName);
        await SendAsync(studentEmail, studentName, subject, html, ct);
    }

    public async Task SendScheduleConfirmationAsync(
        string studentEmail,
        string studentName,
        string seriesName,
        int dayOfWeek,
        string startTime,
        string endTime,
        string? courtName,
        string confirmationUrl,
        CancellationToken ct = default)
    {
        (var subject, var html) = EmailTemplates.ScheduleConfirmation(
            studentName, seriesName, dayOfWeek, startTime, endTime, courtName, confirmationUrl);
        await SendAsync(studentEmail, studentName, subject, html, ct);
    }

    public async Task SendEnrollmentNotificationToTrainerAsync(
        string trainerEmail,
        string trainerName,
        string studentName,
        string studentEmail,
        string seriesName,
        List<(string FieldLabel, string Value)> responses,
        CancellationToken ct = default)
    {
        (var subject, var html) = EmailTemplates.EnrollmentNotificationToTrainer(
            trainerName, studentName, studentEmail, seriesName, responses);
        await SendAsync(trainerEmail, trainerName, subject, html, ct);
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
            EnableSsl = _options.EnableSsl,
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
            logger.LogInformation("E-mail verstuurd naar {Email} — onderwerp: {Subject}", toEmail, subject);
        }
        catch (SmtpException ex)
        {
            logger.LogError(ex, "Versturen mislukt naar {Email} — {Message}", toEmail, ex.Message);
            throw;
        }
    }
}
