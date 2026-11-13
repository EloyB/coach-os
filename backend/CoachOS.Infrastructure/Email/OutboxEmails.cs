using System.Text.Json;
using System.Text.Json.Serialization;
using CoachOS.Application.Abstractions;

namespace CoachOS.Infrastructure.Email;

/// <summary>
/// Eén mail die in de outbox wacht. Bevat alle argumenten van de oorspronkelijke
/// <see cref="IEmailService"/>-aanroep en speelt die aanroep later opnieuw af tegen de
/// echte (SMTP) mailservice.
/// </summary>
internal interface IOutboxEmail
{
    /// <summary>Adres waar de mail naartoe gaat (eerste argument van de aanroep).</summary>
    string Recipient { get; }

    Task SendAsync(IEmailService sender, CancellationToken ct);
}

/// <summary>
/// Mail over een speler of les van een club. Hoort altijd bij een organisatie: ofwel de tenant
/// van het verzoek, ofwel expliciet meegegeven via <see cref="IEmailOrganizationContext"/>.
/// Zonder organisatie logt de <see cref="EmailOutboxWriter"/> een waarschuwing.
/// </summary>
internal interface IClubOutboxEmail : IOutboxEmail;

/// <summary>
/// Register van alle mails die via de outbox gaan: een vaste typenaam per mail (zo blijven
/// rijen die al in de wachtrij staan leesbaar, ook als een record ooit hernoemd wordt) en
/// (de)serialisatie van de argumenten.
/// </summary>
internal static class OutboxEmails
{
    private static readonly (string Key, Type Type)[] Registrations =
    [
        ("mail:website-waitlist", typeof(WebsiteWaitlistOutboxMail)),
        ("mail:website-contact-notification", typeof(WebsiteContactNotificationMail)),
        ("mail:trainer-invite", typeof(TrainerInviteMail)),
        ("mail:admin-invite", typeof(AdminInviteMail)),
        ("mail:enrollment-confirmation", typeof(EnrollmentConfirmationMail)),
        ("mail:group-member-added", typeof(GroupMemberAddedMail)),
        ("mail:enrollment-pending-cash", typeof(EnrollmentPendingCashMail)),
        ("mail:trainer-enrollment-notification", typeof(TrainerEnrollmentNotificationMail)),
        ("mail:schedule-confirmation", typeof(ScheduleConfirmationMail)),
        ("mail:schedule-confirmation-bundle", typeof(ScheduleConfirmationBundleMail)),
        ("mail:student-magic-link", typeof(StudentMagicLinkMail)),
        ("mail:lesson-cancellation", typeof(LessonCancellationMail)),
        ("mail:lesson-rescheduled", typeof(LessonRescheduledMail)),
        ("mail:schedule-info", typeof(ScheduleInfoMail)),
        ("mail:assignment-moved", typeof(AssignmentMovedMail)),
        ("mail:standalone-lesson-invitation", typeof(StandaloneLessonInvitationMail)),
        ("mail:password-reset", typeof(PasswordResetMail)),
        ("mail:camp-enrollment-payment-link", typeof(CampEnrollmentPaymentLinkMail)),
        ("mail:camp-enrollment-confirmed", typeof(CampEnrollmentConfirmedMail)),
        ("mail:email-verification", typeof(EmailVerificationMail)),
        ("mail:trial-reminder", typeof(TrialReminderMail)),
        ("mail:organization-deletion-notice", typeof(OrganizationDeletionNoticeMail)),
        ("mail:subscription-started", typeof(SubscriptionStartedMail)),
        ("mail:subscription-payment-failed", typeof(SubscriptionPaymentFailedMail)),
        ("mail:subscription-invitation", typeof(SubscriptionInvitationMail)),
        ("mail:billing-super-admin-notice", typeof(BillingSuperAdminNoticeMail)),
    ];

    private static readonly Dictionary<Type, string> KeyByType =
        Registrations.ToDictionary(r => r.Type, r => r.Key);

    private static readonly Dictionary<string, Type> TypeByKey =
        Registrations.ToDictionary(r => r.Key, r => r.Type);

    public static IReadOnlyCollection<string> Keys => TypeByKey.Keys;

    public static Type TypeOf(string key) => TypeByKey[key];

    /// <summary>
    /// Typenamen van platformmails: geen <see cref="IClubOutboxEmail"/>, horen bij geen club en
    /// staan dus terecht zonder organisatie in de outbox (wachtwoordreset, inloglink, website, ...).
    /// </summary>
    public static IReadOnlyList<string> PlatformKeys { get; } = Registrations
        .Where(r => !r.Type.IsAssignableTo(typeof(IClubOutboxEmail)))
        .Select(r => r.Key)
        .ToList();

    public static (string Type, string Payload) Serialize(IOutboxEmail mail)
    {
        Type type = mail.GetType();
        if (!KeyByType.TryGetValue(type, out string? key))
            throw new InvalidOperationException($"Mailtype {type.Name} is niet geregistreerd in de outbox.");
        return (key, JsonSerializer.Serialize(mail, type));
    }

    public static bool TryDeserialize(string type, string payload, out IOutboxEmail? mail)
    {
        mail = null;
        if (!TypeByKey.TryGetValue(type, out Type? clrType))
            return false;
        mail = (IOutboxEmail?)JsonSerializer.Deserialize(payload, clrType)
            ?? throw new InvalidOperationException($"Ongeldige payload voor outbox-mail '{type}'.");
        return true;
    }
}

internal sealed record WebsiteWaitlistOutboxMail(string Email, string Token) : IOutboxEmail
{
    [JsonIgnore] public string Recipient => Email;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendWebsiteWaitlistAsync(Email, Token, ct);
}

internal sealed record WebsiteContactNotificationMail(
    string ToEmail, WebsiteContactNotification Notification) : IOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendWebsiteContactNotificationAsync(ToEmail, Notification, ct);
}

internal sealed record TrainerInviteMail(string ToEmail, string FirstName, string InviteUrl) : IOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendTrainerInviteAsync(ToEmail, FirstName, InviteUrl, ct);
}

internal sealed record AdminInviteMail(
    string ToEmail, string FirstName, string OrganizationName, string InviteUrl) : IOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendAdminInviteAsync(ToEmail, FirstName, OrganizationName, InviteUrl, ct);
}

internal sealed record EnrollmentConfirmationMail(
    string StudentEmail, string StudentName, string SeriesName,
    IReadOnlyList<string>? ParticipantNames) : IClubOutboxEmail
{
    [JsonIgnore] public string Recipient => StudentEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendEnrollmentConfirmationAsync(StudentEmail, StudentName, SeriesName, ParticipantNames, ct);
}

internal sealed record GroupMemberAddedMail(
    string StudentEmail, string StudentName, string SeriesName, string GroupName) : IClubOutboxEmail
{
    [JsonIgnore] public string Recipient => StudentEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendGroupMemberAddedAsync(StudentEmail, StudentName, SeriesName, GroupName, ct);
}

internal sealed record EnrollmentPendingCashMail(
    string StudentEmail, string StudentName, string SeriesName, decimal Amount,
    IReadOnlyList<string>? ParticipantNames) : IClubOutboxEmail
{
    [JsonIgnore] public string Recipient => StudentEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendEnrollmentPendingCashAsync(StudentEmail, StudentName, SeriesName, Amount, ParticipantNames, ct);
}

/// <remarks>
/// De interface gebruikt een tuple-lijst; System.Text.Json serialiseert tuple-velden niet,
/// dus hier als <see cref="EmailResponseItem"/> bewaard en bij het afspelen teruggezet.
/// </remarks>
internal sealed record TrainerEnrollmentNotificationMail(
    string TrainerEmail, string TrainerName, string StudentName, string StudentEmail,
    string SeriesName, IReadOnlyList<EmailResponseItem> Responses) : IClubOutboxEmail
{
    [JsonIgnore] public string Recipient => TrainerEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendEnrollmentNotificationToTrainerAsync(
            TrainerEmail, TrainerName, StudentName, StudentEmail, SeriesName,
            Responses.Select(r => (r.FieldLabel, r.Value)).ToList(), ct);
}

internal sealed record ScheduleConfirmationMail(
    string StudentEmail, string StudentName, string SeriesName, int DayOfWeek,
    string StartTime, string EndTime, string? CourtName, string ConfirmationUrl,
    IReadOnlyList<string>? ParticipantNames) : IClubOutboxEmail
{
    [JsonIgnore] public string Recipient => StudentEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendScheduleConfirmationAsync(
            StudentEmail, StudentName, SeriesName, DayOfWeek, StartTime, EndTime,
            CourtName, ConfirmationUrl, ParticipantNames, ct);
}

internal sealed record ScheduleConfirmationBundleMail(
    string ContactEmail, string SeriesName, IReadOnlyList<ScheduleConfirmationItem> Items) : IClubOutboxEmail
{
    [JsonIgnore] public string Recipient => ContactEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendScheduleConfirmationBundleAsync(ContactEmail, SeriesName, Items, ct);
}

internal sealed record StudentMagicLinkMail(string ToEmail, string MagicLinkUrl) : IOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendStudentMagicLinkAsync(ToEmail, MagicLinkUrl, ct);
}

internal sealed record LessonCancellationMail(
    string StudentEmail, string StudentName, string SeriesName, DateOnly LessonDate,
    TimeOnly StartTime, string? CancellationReason) : IClubOutboxEmail
{
    [JsonIgnore] public string Recipient => StudentEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendLessonCancellationAsync(
            StudentEmail, StudentName, SeriesName, LessonDate, StartTime, CancellationReason, ct);
}

internal sealed record LessonRescheduledMail(
    string ToEmail, string ToName, string? SeriesName, DateOnly OldDate, TimeOnly OldStartTime,
    DateOnly NewDate, TimeOnly NewStartTime, TimeOnly NewEndTime, string? CourtName,
    string? Reason) : IClubOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendLessonRescheduledAsync(
            ToEmail, ToName, SeriesName, OldDate, OldStartTime, NewDate, NewStartTime,
            NewEndTime, CourtName, Reason, ct);
}

internal sealed record ScheduleInfoMail(
    string ToEmail, string ToName, string SeriesName, string GroupName, int DayOfWeek,
    string StartTime, string EndTime, string? CourtName) : IClubOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendScheduleInfoAsync(
            ToEmail, ToName, SeriesName, GroupName, DayOfWeek, StartTime, EndTime, CourtName, ct);
}

internal sealed record AssignmentMovedMail(
    string ToEmail, string ToName, string SeriesName, int OldDayOfWeek, string OldStartTime,
    string OldEndTime, int NewDayOfWeek, string NewStartTime, string NewEndTime,
    string? NewCourtName, IReadOnlyList<string>? ParticipantNames) : IClubOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendAssignmentMovedAsync(
            ToEmail, ToName, SeriesName, OldDayOfWeek, OldStartTime, OldEndTime,
            NewDayOfWeek, NewStartTime, NewEndTime, NewCourtName, ParticipantNames, ct);
}

internal sealed record StandaloneLessonInvitationMail(
    string ToEmail, string? FirstName, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime,
    string? CourtName, string? LevelText, string? Notes, string InvitationUrl) : IClubOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendStandaloneLessonInvitationAsync(
            ToEmail, FirstName, Date, StartTime, EndTime, CourtName, LevelText, Notes, InvitationUrl, ct);
}

internal sealed record PasswordResetMail(string ToEmail, string FirstName, string ResetUrl) : IOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendPasswordResetAsync(ToEmail, FirstName, ResetUrl, ct);
}

internal sealed record CampEnrollmentPaymentLinkMail(
    string ParticipantEmail, string ParticipantName, string CampName, DateOnly StartDate,
    DateOnly EndDate, string CheckoutUrl) : IClubOutboxEmail
{
    [JsonIgnore] public string Recipient => ParticipantEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendCampEnrollmentPaymentLinkAsync(
            ParticipantEmail, ParticipantName, CampName, StartDate, EndDate, CheckoutUrl, ct);
}

internal sealed record CampEnrollmentConfirmedMail(
    string ParticipantEmail, string ParticipantName, string CampName, DateOnly StartDate,
    DateOnly EndDate) : IClubOutboxEmail
{
    [JsonIgnore] public string Recipient => ParticipantEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendCampEnrollmentConfirmedAsync(
            ParticipantEmail, ParticipantName, CampName, StartDate, EndDate, ct);
}

internal sealed record EmailVerificationMail(string ToEmail, string FirstName, string Code) : IOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendEmailVerificationAsync(ToEmail, FirstName, Code, ct);
}

internal sealed record TrialReminderMail(
    string ToEmail, string OrganizationName, int DaysRemaining, DateOnly TrialEndsAt,
    string AppUrl) : IOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendTrialReminderAsync(ToEmail, OrganizationName, DaysRemaining, TrialEndsAt, AppUrl, ct);
}

internal sealed record OrganizationDeletionNoticeMail(
    string ToEmail, string OrganizationName, DateOnly DeletionDate, bool FinalNotice,
    string ExportUrl) : IClubOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendOrganizationDeletionNoticeAsync(ToEmail, OrganizationName, DeletionDate, FinalNotice, ExportUrl, ct);
}

internal sealed record SubscriptionStartedMail(
    string ToEmail, string OrganizationName, string PlanName, string IntervalLabel,
    decimal AmountInclVat, DateOnly PeriodStart, bool BankTransfer, DateOnly? DueDate,
    string ManageUrl) : IOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendSubscriptionStartedAsync(
            ToEmail, OrganizationName, PlanName, IntervalLabel, AmountInclVat, PeriodStart,
            BankTransfer, DueDate, ManageUrl, ct);
}

internal sealed record SubscriptionPaymentFailedMail(
    string ToEmail, string OrganizationName, decimal AmountInclVat, string PaymentUrl) : IOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendSubscriptionPaymentFailedAsync(ToEmail, OrganizationName, AmountInclVat, PaymentUrl, ct);
}

internal sealed record SubscriptionInvitationMail(
    string ToEmail, string OrganizationName, string SubscribeUrl) : IOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendSubscriptionInvitationAsync(ToEmail, OrganizationName, SubscribeUrl, ct);
}

internal sealed record BillingSuperAdminNoticeMail(
    string ToEmail, string Subject, string Body, string? Link) : IOutboxEmail
{
    [JsonIgnore] public string Recipient => ToEmail;

    public Task SendAsync(IEmailService sender, CancellationToken ct)
        => sender.SendBillingSuperAdminNoticeAsync(ToEmail, Subject, Body, Link, ct);
}
