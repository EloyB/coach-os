using CoachOS.Application.Abstractions;

namespace CoachOS.Infrastructure.Email;

/// <summary>
/// De <see cref="IEmailService"/> die de applicatie gebruikt: verstuurt niets zelf, maar
/// zet elke mail in de outbox. De <see cref="EmailOutboxWorker"/> levert hem daarna buiten
/// het verzoek af via de SMTP-<see cref="EmailService"/>, met nieuwe pogingen bij een fout.
/// Zo kan een tijdelijke storing van de mailserver of een afgebroken verzoek geen mail
/// meer stil laten verdwijnen.
/// </summary>
/// <remarks>
/// Het <c>ct</c>-argument wordt genegeerd: inplannen is snel en mag niet afgebroken worden
/// zodra de zakelijke wijziging opgeslagen is (zie <see cref="EmailOutboxWriter"/>).
/// </remarks>
internal sealed class OutboxEmailService(IEmailOutboxWriter outbox) : IEmailService
{
    public Task SendWebsiteWaitlistAsync(string email, string token, CancellationToken ct = default)
        => outbox.EnqueueAsync(new WebsiteWaitlistOutboxMail(email, token));

    public Task SendWebsiteContactNotificationAsync(
        string toEmail, WebsiteContactNotification notification, CancellationToken ct = default)
        => outbox.EnqueueAsync(new WebsiteContactNotificationMail(toEmail, notification));

    public Task SendTrainerInviteAsync(
        string toEmail, string firstName, string inviteUrl, CancellationToken ct = default)
        => outbox.EnqueueAsync(new TrainerInviteMail(toEmail, firstName, inviteUrl));

    public Task SendAdminInviteAsync(
        string toEmail, string firstName, string organizationName, string inviteUrl,
        CancellationToken ct = default)
        => outbox.EnqueueAsync(new AdminInviteMail(toEmail, firstName, organizationName, inviteUrl));

    public Task SendEnrollmentConfirmationAsync(
        string studentEmail, string studentName, string seriesName,
        IReadOnlyList<string>? participantNames = null, CancellationToken ct = default)
        => outbox.EnqueueAsync(new EnrollmentConfirmationMail(
            studentEmail, studentName, seriesName, participantNames));

    public Task SendGroupMemberAddedAsync(
        string studentEmail, string studentName, string seriesName, string groupName,
        CancellationToken ct = default)
        => outbox.EnqueueAsync(new GroupMemberAddedMail(studentEmail, studentName, seriesName, groupName));

    public Task SendEnrollmentPendingCashAsync(
        string studentEmail, string studentName, string seriesName, decimal amount,
        IReadOnlyList<string>? participantNames = null, CancellationToken ct = default)
        => outbox.EnqueueAsync(new EnrollmentPendingCashMail(
            studentEmail, studentName, seriesName, amount, participantNames));

    public Task SendEnrollmentNotificationToTrainerAsync(
        string trainerEmail, string trainerName, string studentName, string studentEmail,
        string seriesName, List<(string FieldLabel, string Value)> responses,
        CancellationToken ct = default)
        => outbox.EnqueueAsync(new TrainerEnrollmentNotificationMail(
            trainerEmail, trainerName, studentName, studentEmail, seriesName,
            responses.Select(r => new EmailResponseItem(r.FieldLabel, r.Value)).ToList()));

    public Task SendScheduleConfirmationAsync(
        string studentEmail, string studentName, string seriesName, int dayOfWeek,
        string startTime, string endTime, string? courtName, string confirmationUrl,
        IReadOnlyList<string>? participantNames = null, CancellationToken ct = default)
        => outbox.EnqueueAsync(new ScheduleConfirmationMail(
            studentEmail, studentName, seriesName, dayOfWeek, startTime, endTime,
            courtName, confirmationUrl, participantNames));

    public Task SendScheduleConfirmationBundleAsync(
        string contactEmail, string seriesName, IReadOnlyList<ScheduleConfirmationItem> items,
        CancellationToken ct = default)
        => outbox.EnqueueAsync(new ScheduleConfirmationBundleMail(contactEmail, seriesName, items));

    public Task SendStudentMagicLinkAsync(string toEmail, string magicLinkUrl, CancellationToken ct = default)
        => outbox.EnqueueAsync(new StudentMagicLinkMail(toEmail, magicLinkUrl));

    public Task SendLessonCancellationAsync(
        string studentEmail, string studentName, string seriesName, DateOnly lessonDate,
        TimeOnly startTime, string? cancellationReason, CancellationToken ct = default)
        => outbox.EnqueueAsync(new LessonCancellationMail(
            studentEmail, studentName, seriesName, lessonDate, startTime, cancellationReason));

    public Task SendLessonRescheduledAsync(
        string toEmail, string toName, string? seriesName, DateOnly oldDate, TimeOnly oldStartTime,
        DateOnly newDate, TimeOnly newStartTime, TimeOnly newEndTime, string? courtName,
        string? reason, CancellationToken ct = default)
        => outbox.EnqueueAsync(new LessonRescheduledMail(
            toEmail, toName, seriesName, oldDate, oldStartTime, newDate, newStartTime,
            newEndTime, courtName, reason));

    public Task SendScheduleInfoAsync(
        string toEmail, string toName, string seriesName, string groupName, int dayOfWeek,
        string startTime, string endTime, string? courtName, CancellationToken ct = default)
        => outbox.EnqueueAsync(new ScheduleInfoMail(
            toEmail, toName, seriesName, groupName, dayOfWeek, startTime, endTime, courtName));

    public Task SendAssignmentMovedAsync(
        string toEmail, string toName, string seriesName, int oldDayOfWeek, string oldStartTime,
        string oldEndTime, int newDayOfWeek, string newStartTime, string newEndTime,
        string? newCourtName, IReadOnlyList<string>? participantNames = null,
        CancellationToken ct = default)
        => outbox.EnqueueAsync(new AssignmentMovedMail(
            toEmail, toName, seriesName, oldDayOfWeek, oldStartTime, oldEndTime,
            newDayOfWeek, newStartTime, newEndTime, newCourtName, participantNames));

    public Task SendStandaloneLessonInvitationAsync(
        string toEmail, string? firstName, DateOnly date, TimeOnly startTime, TimeOnly endTime,
        string? courtName, string? levelText, string? notes, string invitationUrl,
        CancellationToken ct = default)
        => outbox.EnqueueAsync(new StandaloneLessonInvitationMail(
            toEmail, firstName, date, startTime, endTime, courtName, levelText, notes, invitationUrl));

    public Task SendPasswordResetAsync(
        string toEmail, string firstName, string resetUrl, CancellationToken ct = default)
        => outbox.EnqueueAsync(new PasswordResetMail(toEmail, firstName, resetUrl));

    public Task SendCampEnrollmentPaymentLinkAsync(
        string participantEmail, string participantName, string campName, DateOnly startDate,
        DateOnly endDate, string checkoutUrl, CancellationToken ct = default)
        => outbox.EnqueueAsync(new CampEnrollmentPaymentLinkMail(
            participantEmail, participantName, campName, startDate, endDate, checkoutUrl));

    public Task SendCampEnrollmentConfirmedAsync(
        string participantEmail, string participantName, string campName, DateOnly startDate,
        DateOnly endDate, CancellationToken ct = default)
        => outbox.EnqueueAsync(new CampEnrollmentConfirmedMail(
            participantEmail, participantName, campName, startDate, endDate));

    public Task SendEmailVerificationAsync(
        string toEmail, string firstName, string code, CancellationToken ct = default)
        => outbox.EnqueueAsync(new EmailVerificationMail(toEmail, firstName, code));

    public Task SendTrialReminderAsync(
        string toEmail, string organizationName, int daysRemaining, DateOnly trialEndsAt,
        string appUrl, CancellationToken ct = default)
        => outbox.EnqueueAsync(new TrialReminderMail(
            toEmail, organizationName, daysRemaining, trialEndsAt, appUrl));

    public Task SendSubscriptionStartedAsync(
        string toEmail, string organizationName, string planName, string intervalLabel,
        decimal amountInclVat, DateOnly periodStart, bool bankTransfer, DateOnly? dueDate,
        string manageUrl, CancellationToken ct = default)
        => outbox.EnqueueAsync(new SubscriptionStartedMail(
            toEmail, organizationName, planName, intervalLabel, amountInclVat, periodStart,
            bankTransfer, dueDate, manageUrl));

    public Task SendSubscriptionPaymentFailedAsync(
        string toEmail, string organizationName, decimal amountInclVat, string paymentUrl,
        CancellationToken ct = default)
        => outbox.EnqueueAsync(new SubscriptionPaymentFailedMail(
            toEmail, organizationName, amountInclVat, paymentUrl));

    public Task SendSubscriptionInvitationAsync(
        string toEmail, string organizationName, string subscribeUrl, CancellationToken ct = default)
        => outbox.EnqueueAsync(new SubscriptionInvitationMail(toEmail, organizationName, subscribeUrl));

    public Task SendBillingSuperAdminNoticeAsync(
        string toEmail, string subject, string body, string? link, CancellationToken ct = default)
        => outbox.EnqueueAsync(new BillingSuperAdminNoticeMail(toEmail, subject, body, link));
}
