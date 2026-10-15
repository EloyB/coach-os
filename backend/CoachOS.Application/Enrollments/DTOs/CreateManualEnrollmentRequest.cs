namespace CoachOS.Application.Enrollments.DTOs;

public record CreateManualEnrollmentRequest
{
    public string StudentName { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string? StudentEmail { get; init; }
    public string? StudentPhone { get; init; }
    public string DateOfBirth { get; init; } = string.Empty;
    public List<FormResponseValueDto> Responses { get; init; } = new();
}
