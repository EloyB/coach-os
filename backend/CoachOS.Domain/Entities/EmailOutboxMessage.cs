using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Durable email work item, delivered by a background worker with retries. Enrollment
/// notifications are created in the same transaction as the business change that caused
/// them; every other mail is queued by the outbox-backed email service instead of being
/// sent inside the request.
/// </summary>
public class EmailOutboxMessage : BaseEntity
{
    /// <summary>Organisatie waartoe de mail hoort, of null zonder tenant (publiek, achtergrondtaak).</summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>Inschrijving waartoe de mail hoort, of null voor mails zonder inschrijving.</summary>
    public Guid? EnrollmentId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string Status { get; set; } = EmailOutboxStatuses.Pending;
    public int Attempts { get; set; }
    public DateTime AvailableAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? LastError { get; set; }
}

public static class EmailOutboxStatuses
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Completed = "completed";
    public const string Failed = "failed";
}

public static class EmailOutboxMessageTypes
{
    public const string EnrollmentConfirmation = "enrollment-confirmation";
    public const string TrainerNotification = "trainer-notification";
    public const string GroupMemberAdded = "group-member-added";
}
