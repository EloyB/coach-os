namespace CoachOS.Application.Enrollments.DTOs;

public record GroupMemberDto
{
    public string StudentName { get; init; } = string.Empty;

    /// <summary>
    /// Eigen adres van dit groepslid. Null of leeg betekent: communicatie loopt via
    /// de groepsleider, wiens adres dan als ContactEmail wordt opgeslagen.
    /// </summary>
    public string? StudentEmail { get; init; }
    public string? StudentPhone { get; init; }

    /// <summary>Geboortedatum in formaat yyyy-MM-dd. Verplicht, net als bij de leider.</summary>
    public string DateOfBirth { get; init; } = string.Empty;

    public List<FormResponseValueDto>? Responses { get; init; }
}
