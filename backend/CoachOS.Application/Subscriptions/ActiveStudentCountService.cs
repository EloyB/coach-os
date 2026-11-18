using System.Text.RegularExpressions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Common;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Subscriptions;

/// <summary>
/// Telt de unieke actieve leerlingen in het lopende abonnementsjaar.
/// <para>
/// Periode: <see cref="Subscription.CurrentPeriodStart"/>..<see cref="Subscription.CurrentPeriodEnd"/> als beide
/// gezet zijn; anders (proef/legacy/locked) <see cref="Subscription.StartDate"/> + n jaar, met n zo gekozen dat
/// vandaag in de periode valt. Beide dagen zijn inclusief; <c>EnrolledAt</c> (UTC) wordt vergeleken met
/// 00:00 UTC van de startdag tot 00:00 UTC van de dag na de einddag.
/// </para>
/// <para>
/// Telregels: enkel <c>Confirmed</c>, niet geanonimiseerd, org-gescoped; lesinschrijvingen (reeks én losse les)
/// en kampinschrijvingen samen. Persoonssleutel = genormaliseerde naam (trim, lowercase, spaties samengevouwen)
/// + geboortedatum + e-mail (lesinschrijving: <c>StudentEmail ?? ContactEmail</c>; kamp: <c>ParticipantEmail</c>).
/// </para>
/// <para>
/// Bewuste overschatting: een kampinschrijving heeft geen geboortedatum. Dezelfde persoon in een kamp én in een
/// reeks waarvan de inschrijving wel een geboortedatum heeft, telt daarom als twee. Overschatten is aanvaard
/// (de teller bewaakt een limiet); onderschatten door een sleutelbotsing niet.
/// </para>
/// </summary>
public partial class ActiveStudentCountService(
    ISubscriptionRepository subscriptionRepo,
    IEnrollmentRepository enrollmentRepo,
    ICampEnrollmentRepository campEnrollmentRepo,
    TimeProvider timeProvider) : IActiveStudentCountService
{
    public async Task<Result<ActiveStudentsDto>> GetActiveStudentsAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        Subscription? subscription = await subscriptionRepo.GetByOrganizationReadOnlyAsync(organizationId, ct);
        if (subscription is null)
            return Result<ActiveStudentsDto>.Fail(new Error(ErrorCodes.NotFound, "Abonnement niet gevonden."));

        (DateOnly periodStart, DateOnly periodEnd) = ResolvePeriod(subscription, timeProvider.GetBrusselsToday());
        DateTime fromUtc = periodStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime toUtcExclusive = periodEnd.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // De DB levert per bron al een distincte (en getrimde/lowercase) projectie, dus niet alle rijen komen in
        // het geheugen. Het samenvouwen van meervoudige spaties en het samenvoegen van beide bronnen gebeurt
        // daarna in het geheugen: EF/Npgsql vertaalt geen regex-collapse naar SQL.
        IReadOnlyList<StudentIdentityKey> lessonKeys =
            await enrollmentRepo.GetConfirmedStudentKeysAsync(organizationId, fromUtc, toUtcExclusive, ct);
        IReadOnlyList<StudentIdentityKey> campKeys =
            await campEnrollmentRepo.GetConfirmedStudentKeysAsync(organizationId, fromUtc, toUtcExclusive, ct);

        int count = lessonKeys.Concat(campKeys).Select(ToPersonKey).Distinct().Count();

        return Result<ActiveStudentsDto>.Ok(new ActiveStudentsDto(count, subscription.StudentLimit, periodStart, periodEnd));
    }

    internal static (DateOnly Start, DateOnly End) ResolvePeriod(Subscription subscription, DateOnly today)
    {
        if (subscription.CurrentPeriodStart is DateOnly start && subscription.CurrentPeriodEnd is DateOnly end)
            return (start, end);

        int years = 0;
        while (subscription.StartDate.AddYears(years + 1).AddDays(-1) < today)
            years++;
        return (subscription.StartDate.AddYears(years), subscription.StartDate.AddYears(years + 1).AddDays(-1));
    }

    private static (string Name, DateOnly? Dob, string Email) ToPersonKey(StudentIdentityKey key)
    {
        string email = string.IsNullOrWhiteSpace(key.PrimaryEmail) ? key.FallbackEmail ?? "" : key.PrimaryEmail;
        return (Whitespace().Replace(key.Name, " ").Trim().ToLowerInvariant(), key.DateOfBirth, email.Trim().ToLowerInvariant());
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
