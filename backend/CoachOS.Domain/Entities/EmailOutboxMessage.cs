using CoachOS.Domain.Common;

namespace CoachOS.Domain.Entities;

/// <summary>
/// Durable email work item. It is created in the same transaction as the business change
/// that caused it, so a successful enrollment cannot lose its notifications.
/// </summary>
public class EmailOutboxMessage : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid EnrollmentId { get; set; }
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
