using CoachOS.Domain.Common;
using CoachOS.Tests.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Common;

[TestFixture]
public class TimeProviderExtensionsTests
{
    [Test]
    public void GetBrusselsToday_JustBeforeMidnightUtc_StillYesterdayInBrussels()
    {
        // 22:30 UTC op 30 juni (zomertijd, CEST = UTC+2) is 00:30 CEST op 1 juli:
        // al de volgende dag lokaal, ook al is het UTC nog de vorige dag.
        FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 6, 30, 22, 30, 0, TimeSpan.Zero));

        DateOnly today = timeProvider.GetBrusselsToday();

        today.Should().Be(new DateOnly(2026, 7, 1));
    }

    [Test]
    public void GetBrusselsToday_JustAfterMidnightUtc_SameDayInWinter()
    {
        // 00:30 UTC op 15 januari (wintertijd, CET = UTC+1) is 01:30 CET dezelfde dag.
        FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 15, 0, 30, 0, TimeSpan.Zero));

        DateOnly today = timeProvider.GetBrusselsToday();

        today.Should().Be(new DateOnly(2026, 1, 15));
    }

    [Test]
    public void GetBrusselsToday_23h30Utc_IsAlreadyTomorrowInWinterCet()
    {
        // 23:30 UTC op 14 januari (CET = UTC+1) is 00:30 CET op 15 januari: de kernbug
        // die deze fix oplost — DateOnly.FromDateTime(DateTime.UtcNow) zou hier nog
        // 14 januari teruggeven, één dag te vroeg.
        FixedTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 14, 23, 30, 0, TimeSpan.Zero));

        DateOnly today = timeProvider.GetBrusselsToday();

        today.Should().Be(new DateOnly(2026, 1, 15));
    }
}
