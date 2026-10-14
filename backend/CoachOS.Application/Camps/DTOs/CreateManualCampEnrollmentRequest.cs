namespace CoachOS.Application.Camps.DTOs;

/// <summary>
/// Beheerder schrijft een deelnemer handmatig in op een kamp (bv. telefonisch of ter plaatse).
/// <see cref="Paid"/> bepaalt bij een betaald kamp of de cash al ontvangen is.
/// </summary>
public record CreateManualCampEnrollmentRequest
{
    public string ParticipantName { get; init; } = string.Empty;
    public string ParticipantEmail { get; init; } = string.Empty;
    public string? ParticipantPhone { get; init; }

    /// <summary>true = cash al ontvangen (meteen bevestigd); false = nog te betalen (PendingPayment).</summary>
    public bool Paid { get; init; }

    public List<CampFormResponseValueDto> Responses { get; init; } = new();
}
