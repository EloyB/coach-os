namespace CoachOS.Application.Enrollments.DTOs;

public record GroupMemberDto
{
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public string? StudentPhone { get; init; }

    /// <summary>Geboortedatum in formaat yyyy-MM-dd. Verplicht, net als bij de leider.</summary>
    public string DateOfBirth { get; init; } = string.Empty;

    public List<FormResponseValueDto>? Responses { get; init; }
}
