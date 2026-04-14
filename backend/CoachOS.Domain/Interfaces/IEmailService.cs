namespace CoachOS.Domain.Interfaces;

public interface IEmailService
{
    Task SendTrainerInviteAsync(string toEmail, string firstName, string inviteUrl, CancellationToken ct = default);

    Task SendEnrollmentConfirmationAsync(
        string studentEmail,
        string studentName,
        string seriesName,
        string trainerName,
        CancellationToken ct = default);

    Task SendEnrollmentNotificationToTrainerAsync(
        string trainerEmail,
        string trainerName,
        string studentName,
        string studentEmail,
        string seriesName,
        List<(string FieldLabel, string Value)> responses,
        CancellationToken ct = default);

    Task SendScheduleConfirmationAsync(
        string studentEmail,
        string studentName,
        string seriesName,
        int dayOfWeek,
        string startTime,
        string endTime,
        string? courtName,
        string confirmationUrl,
        CancellationToken ct = default);

    Task SendStudentMagicLinkAsync(
        string toEmail,
        string magicLinkUrl,
        CancellationToken ct = default);
}
