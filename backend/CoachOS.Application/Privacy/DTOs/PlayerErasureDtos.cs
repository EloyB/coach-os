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

public sealed record PlayerRetentionReport(int Candidates, int RetentionDue, int Erased);
