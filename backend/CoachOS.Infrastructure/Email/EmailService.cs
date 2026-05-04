using System.Net;
using System.Net.Mail;
using CoachOS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Email;

public class EmailService(
    IOptions<EmailOptions> options,
    IMjmlTemplateRenderer renderer,
    ILogger<EmailService> logger)
    : IEmailService
{
    private readonly EmailOptions _options = options.Value;
    private static readonly string[] DaysNl =
        ["zondag", "maandag", "dinsdag", "woensdag", "donderdag", "vrijdag", "zaterdag"];

    public async Task SendTrainerInviteAsync(
        string toEmail, string firstName, string inviteUrl, CancellationToken ct = default)
    {
        var html = renderer.Render("trainer-invite", new Dictionary<string, string>
        {
            ["firstName"] = firstName,
            ["organizationName"] = "CoachOS",
            ["inviteUrl"] = inviteUrl,
            ["year"] = DateTime.UtcNow.Year.ToString(),
        });
        await SendAsync(toEmail, firstName,
            $"Je bent uitgenodigd als trainer bij CoachOS", html, ct);
    }

    public async Task SendEnrollmentConfirmationAsync(
        string studentEmail, string studentName, string seriesName, string trainerName,
        CancellationToken ct = default)
    {
        var html = renderer.Render("enrollment-confirmation", new Dictionary<string, string>
        {
            ["studentName"] = studentName,
            ["seriesName"] = seriesName,
            ["trainerName"] = trainerName,
            ["year"] = DateTime.UtcNow.Year.ToString(),
        });
        await SendAsync(studentEmail, studentName,
            $"Inschrijving bevestigd: {seriesName}", html, ct);
    }

    public async Task SendScheduleConfirmationAsync(
        string studentEmail, string studentName, string seriesName,
        int dayOfWeek, string startTime, string endTime, string? courtName,
        string confirmationUrl, CancellationToken ct = default)
    {
        var dayName = DaysNl[Math.Clamp(dayOfWeek, 0, 6)];
        var courtLine = string.IsNullOrWhiteSpace(courtName) ? string.Empty : $"Baan: {courtName}";

        var html = renderer.Render("schedule-confirmation", new Dictionary<string, string>
        {
            ["studentName"] = studentName,
            ["seriesName"] = seriesName,
            ["dayName"] = dayName,
            ["startTime"] = startTime,
            ["endTime"] = endTime,
            ["courtLine"] = courtLine,
            ["confirmationUrl"] = confirmationUrl,
            ["year"] = DateTime.UtcNow.Year.ToString(),
        });
        await SendAsync(studentEmail, studentName,
            $"Bevestig je lesmoment: {seriesName}", html, ct);
    }

    public async Task SendStudentMagicLinkAsync(
        string toEmail, string magicLinkUrl, CancellationToken ct = default)
    {
        var html = renderer.Render("student-magic-link", new Dictionary<string, string>
        {
            ["magicLinkUrl"] = magicLinkUrl,
            ["year"] = DateTime.UtcNow.Year.ToString(),
        });
        await SendAsync(toEmail, toEmail, "Je inloglink voor CoachOS", html, ct);
    }

    public async Task SendEnrollmentNotificationToTrainerAsync(
        string trainerEmail, string trainerName,
        string studentName, string studentEmail, string seriesName,
        List<(string FieldLabel, string Value)> responses,
        CancellationToken ct = default)
    {
        string responsesLabel;
        string responsesHtml;
        if (responses.Count > 0)
        {
            responsesLabel = "Antwoorden formulier";
            // Elke waarde HTML-encoden: FieldLabel en Value zijn student-invoer en kunnen
            // anders HTML/script bevatten dat in de trainer-inbox gerenderd wordt.
            var rows = string.Join("",
                responses.Select(r =>
                    $"<div style=\"padding:8px 0;border-bottom:1px solid #f3f4f6\">" +
                    $"<span style=\"color:#6b7280;display:inline-block;width:40%\">{WebUtility.HtmlEncode(r.FieldLabel)}</span>" +
                    $"<span style=\"color:#111827\">{WebUtility.HtmlEncode(r.Value)}</span></div>"));
            responsesHtml = rows;
        }
        else
        {
            responsesLabel = string.Empty;
            responsesHtml = string.Empty;
        }

        // responsesHtml is door ons zelf gebouwde HTML (reeds geëncodeerde waarden) →
        // gebruik "raw:" prefix zodat de renderer niet nogmaals encodeert.
        var html = renderer.Render("enrollment-notification-trainer", new Dictionary<string, string>
        {
            ["trainerName"] = trainerName,
            ["studentName"] = studentName,
            ["studentEmail"] = studentEmail,
            ["seriesName"] = seriesName,
            ["responsesLabel"] = responsesLabel,
            ["raw:responsesHtml"] = responsesHtml,
            ["year"] = DateTime.UtcNow.Year.ToString(),
        });
        await SendAsync(trainerEmail, trainerName,
            $"Nieuwe inschrijving: {studentName} voor {seriesName}", html, ct);
    }

    public async Task SendStandaloneLessonInvitationAsync(
        string toEmail,
        string? firstName,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        string? courtName,
        string trainerName,
        string? levelText,
        string? notes,
        string invitationUrl,
        CancellationToken ct = default)
    {
        string greetingName = string.IsNullOrWhiteSpace(firstName) ? string.Empty : $" {firstName}";
        string dayName = DaysNl[(int)date.DayOfWeek];
        string dateText = date.ToString("dd/MM/yyyy");
        string courtSuffix = string.IsNullOrWhiteSpace(courtName) ? string.Empty : $" · Baan: {courtName}";
        string levelLine = string.IsNullOrWhiteSpace(levelText) ? string.Empty : $"Niveau: {levelText}";
        string notesLine = string.IsNullOrWhiteSpace(notes) ? string.Empty : notes!;
        string toName = string.IsNullOrWhiteSpace(firstName) ? toEmail : firstName!;

        string html = renderer.Render("standalone-lesson-invitation", new Dictionary<string, string>
        {
            ["greetingName"] = greetingName,
            ["trainerName"] = trainerName,
            ["dayName"] = dayName,
            ["dateText"] = dateText,
            ["startTime"] = startTime.ToString("HH:mm"),
            ["endTime"] = endTime.ToString("HH:mm"),
            ["courtSuffix"] = courtSuffix,
            ["levelLine"] = levelLine,
            ["notesLine"] = notesLine,
            ["invitationUrl"] = invitationUrl,
            ["year"] = DateTime.UtcNow.Year.ToString(),
        });

        await SendAsync(toEmail, toName,
            $"Uitnodiging tennisles op {dateText}", html, ct);
    }

    // ── Core send method (reused by all email types) ──────────────────────────

    private async Task SendAsync(
        string toEmail, string toName, string subject, string htmlBody,
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
