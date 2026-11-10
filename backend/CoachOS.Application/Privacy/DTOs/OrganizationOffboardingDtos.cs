namespace CoachOS.Application.Privacy.DTOs;

/// <summary>Reden waarom een organisatie (nog) niet verwijderd mag worden.</summary>
/// <param name="Code">Stabiele code voor de frontend-vertaling.</param>
/// <param name="Message">Nederlandse uitleg voor logs en API-clients.</param>
/// <param name="Date">Relevante datum (einde abonnement of einde bewaartermijn), indien van toepassing.</param>
public sealed record OffboardingBlockerDto(string Code, string Message, DateOnly? Date = null);

public sealed record OrganizationDataCountsDto(
    int TennisClubs,
    int LessonSeries,
    int Lessons,
    int Enrollments,
    int Camps,
    int CampEnrollments,
    int Payments,
    int Memberships,
    int AccountsToDelete,
    int SubscriptionPayments);

/// <summary>Wat een verwijdering zou raken en of ze nu mag.</summary>
public sealed record OrganizationOffboardingPreviewDto(
    Guid OrganizationId,
    string OrganizationName,
    string SubscriptionStatus,
    DateOnly? EndedOn,
    DateOnly? DeletableFrom,
    bool Eligible,
    IReadOnlyList<OffboardingBlockerDto> Blockers,
    OrganizationDataCountsDto Counts);

/// <summary>Bevestiging met de exacte organisatienaam.</summary>
public sealed record DeleteOrganizationRequest(string ConfirmationName);

public sealed record OrganizationDeletedDto(Guid OrganizationId, int DeletedAccounts, DateTime ErasedAtUtc);
