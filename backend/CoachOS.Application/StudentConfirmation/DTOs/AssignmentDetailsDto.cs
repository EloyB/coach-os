namespace CoachOS.Application.StudentConfirmation.DTOs;

public record AssignmentDetailsDto
{
    public Guid AssignmentId { get; init; }
    public string SeriesName { get; init; } = string.Empty;
    public int DayOfWeek { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string? CourtName { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public decimal PricePerPerson { get; init; }
    public decimal TotalPrice { get; init; }
    public bool IsGroup { get; init; }
    public List<string> GroupMemberNames { get; init; } = new();
    public string Status { get; init; } = string.Empty; // Pending | Confirmed | Declined | Expired
    public DateTime ExpiresAt { get; init; }
}
