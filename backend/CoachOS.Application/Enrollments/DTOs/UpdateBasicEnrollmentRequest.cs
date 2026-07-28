namespace CoachOS.Application.Enrollments.DTOs;

public record UpdateBasicEnrollmentRequest
{
    public string StudentName { get; init; } = string.Empty;

    /// <summary>Adres waar communicatie voor deze inschrijving heen gaat.</summary>
    public string ContactEmail { get; init; } = string.Empty;

    /// <summary>Eigen adres van de deelnemer. Null wanneer communicatie via de contactpersoon loopt.</summary>
    public string? StudentEmail { get; init; }

    public string? StudentPhone { get; init; }

    /// <summary>Geboortedatum in formaat yyyy-MM-dd.</summary>
    public string DateOfBirth { get; init; } = string.Empty;

    public bool IsOpenToGrouping { get; init; }
}
