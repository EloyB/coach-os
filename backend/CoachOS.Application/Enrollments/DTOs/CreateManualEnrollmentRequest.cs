namespace CoachOS.Application.Enrollments.DTOs;

public record CreateManualEnrollmentRequest
{
    public string StudentName { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string? StudentEmail { get; init; }
    public string? StudentPhone { get; init; }
    public string DateOfBirth { get; init; } = string.Empty;
    public List<FormResponseValueDto> Responses { get; init; } = new();

    /// <summary>
    /// Maximum aantal personen waarmee de deelnemer in één tijdslot wil zitten (1 tot 4). Null = 1 (solo, niet
    /// koppelbaar). Bij het toevoegen aan een bestaande groep wordt dit genegeerd: het lid erft het van de groep.
    /// </summary>
    public int? MaxGroupSize { get; init; }
}
