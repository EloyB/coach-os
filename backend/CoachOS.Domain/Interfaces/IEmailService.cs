using CoachOS.Domain.Models;

namespace CoachOS.Domain.Interfaces;

public interface IEmailService
{
    Task SendTrainerInviteAsync(string toEmail, string firstName, string inviteUrl, CancellationToken ct = default);

    Task SendAdminInviteAsync(string toEmail, string firstName, string organizationName, string inviteUrl, CancellationToken ct = default);

    Task SendEnrollmentConfirmationAsync(
        string studentEmail,
        string studentName,
        string seriesName,
        string trainerName,
        IReadOnlyList<string>? participantNames = null,
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
        IReadOnlyList<string>? participantNames = null,
        CancellationToken ct = default);

    /// <summary>
    /// Eén mail voor meerdere deelnemers die hetzelfde contactadres delen. Elke
    /// deelnemer krijgt een eigen blok met een eigen bevestigingsknop.
    /// </summary>
    Task SendScheduleConfirmationBundleAsync(
        string contactEmail,
        string seriesName,
        IReadOnlyList<ScheduleConfirmationItem> items,
        CancellationToken ct = default);

    Task SendStudentMagicLinkAsync(
        string toEmail,
        string magicLinkUrl,
        CancellationToken ct = default);

    Task SendLessonCancellationAsync(
        string studentEmail,
        string studentName,
        string seriesName,
        DateOnly lessonDate,
        TimeOnly startTime,
        string? cancellationReason,
        CancellationToken ct = default);

    Task SendLessonRescheduledAsync(
        string toEmail,
        string toName,
        string? seriesName,
        DateOnly oldDate,
        TimeOnly oldStartTime,
        DateOnly newDate,
        TimeOnly newStartTime,
        TimeOnly newEndTime,
        string? courtName,
        string trainerName,
        string? reason,
        CancellationToken ct = default);

    Task SendStandaloneLessonInvitationAsync(
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
        CancellationToken ct = default);

    Task SendPasswordResetAsync(
        string toEmail,
        string firstName,
        string resetUrl,
        CancellationToken ct = default);

    Task SendCampEnrollmentPaymentLinkAsync(
        string participantEmail,
        string participantName,
        string campName,
        DateOnly startDate,
        DateOnly endDate,
        string checkoutUrl,
        CancellationToken ct = default);

    Task SendCampEnrollmentConfirmedAsync(
        string participantEmail,
        string participantName,
        string campName,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default);
}
