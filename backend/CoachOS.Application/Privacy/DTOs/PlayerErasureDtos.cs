namespace CoachOS.Application.Privacy.DTOs;

public sealed record ErasureGroupMemberDto(Guid Id, string Name);

/// <summary>Overzicht vóór verwijdering: wat verdwijnt er, en wat blokkeert (typed codes).</summary>
public sealed record PlayerErasurePreviewDto(
    Guid EnrollmentId,
    string ParticipantName,
    DateOnly? LastParticipation,
    bool RetentionPeriodPassed,
    bool CanErase,
    IReadOnlyList<string> BlockingReasons,
    int FormAnswerCount,
    string GroupEffect,
    IReadOnlyList<ErasureGroupMemberDto> OtherGroupMembers,
    DateTime UpdatedAt);

/// <param name="ConfirmationName">Exacte naam van de speler, als bevestiging.</param>
/// <param name="NewGroupLeaderId">Verplicht als de speler groepsleider is van een groep met andere leden.</param>
/// <param name="ExpectedUpdatedAt">Optioneel: <c>UpdatedAt</c> uit het overzicht; wijkt het af, dan wordt er niets gewist.</param>
public sealed record ErasePlayerRequest(
    string ConfirmationName,
    Guid? NewGroupLeaderId = null,
    DateTime? ExpectedUpdatedAt = null);

public sealed record PlayerErasureResultDto(Guid EnrollmentId, DateTime ErasedAt);

/// <summary>
/// Resultaat van één bewaartermijn-run voor één organisatie. Bij een dry-run blijven
/// <paramref name="Erased"/> en <paramref name="Anonymized"/> 0 en tonen de andere velden wat er zou gebeuren.
/// </summary>
/// <param name="Candidates">Inschrijvingen die nog persoonsgegevens bevatten.</param>
/// <param name="DueForErasure">Termijn verstreken, mag volledig gewist worden.</param>
/// <param name="DueForAnonymization">Termijn verstreken, maar er hangen betalingen aan: enkel anonimiseren.</param>
/// <param name="DueButBlocked">Termijn verstreken, maar iets blokkeert (open betaling, groep, onverwerkte mail, ...).</param>
public sealed record PlayerRetentionReport(
    int Candidates,
    int DueForErasure,
    int DueForAnonymization,
    int DueButBlocked,
    int Erased,
    int Anonymized)
{
    public int Due => DueForErasure + DueForAnonymization + DueButBlocked;
}

/// <summary>Aantal opgeruimde (of bij een dry-run: op te ruimen) technische records.</summary>
public sealed record PrivacyCleanupReport(int CompletedMails, int ExpiredMagicLinks, int ExpiredConfirmationTokens)
{
    public int Total => CompletedMails + ExpiredMagicLinks + ExpiredConfirmationTokens;
}
