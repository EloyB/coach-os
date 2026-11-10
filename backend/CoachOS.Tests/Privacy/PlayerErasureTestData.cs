using CoachOS.Domain.Enums;
using CoachOS.Domain.Models.Privacy;

namespace CoachOS.Tests.Privacy;

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal static class PlayerErasureTestData
{
    public static readonly DateOnly Today = new(2026, 11, 10);
    public static readonly DateTime UpdatedAt = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

    public static FixedTimeProvider Clock() => new(new DateTimeOffset(Today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero));

    public static PlayerErasureFacts Facts(
        Guid? id = null,
        DateOnly? lastParticipation = null,
        bool hasPayments = false,
        bool openPayments = false,
        EnrollmentStatus status = EnrollmentStatus.Confirmed,
        ErasureGroupFacts? group = null,
        bool pendingMail = false,
        bool chainIncomplete = false,
        bool noParticipation = false)
    {
        Guid targetId = id ?? Guid.NewGuid();
        IReadOnlyList<DateOnly> dates = noParticipation ? [] : [lastParticipation ?? Today.AddYears(-3)];
        return new PlayerErasureFacts(
            new ErasureTarget(targetId, PrivacyErasureKind.Enrollment, "Synthetic Player", status, group?.GroupId, UpdatedAt),
            dates, chainIncomplete, hasPayments, openPayments, group, false, false, pendingMail, false, 2);
    }

    public static ErasureGroupFacts Group(Guid leader, params Guid[] members) =>
        new(Guid.NewGuid(), leader, members.Select(m => new ErasureGroupMember(m, $"Member {m:N}")).ToList(), false);
}
