namespace CoachOS.Domain.Models;

/// <summary>Aantallen gegevens van één organisatie, voor de offboarding-preview.</summary>
public sealed record OrganizationDataCounts(
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
