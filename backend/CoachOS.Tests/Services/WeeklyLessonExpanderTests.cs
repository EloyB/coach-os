using CoachOS.Application.LessonSerie;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class WeeklyLessonExpanderTests
{
    // 2026-08-03 is een maandag.
    [Test]
    public void MatchingDates_ReturnsEveryMonday_InRange()
    {
        // maandag = 0
        IReadOnlyList<DateOnly> dates = WeeklyLessonExpander.MatchingDates(
            dayOfWeek: 0,
            from: new DateOnly(2026, 8, 3),
            to: new DateOnly(2026, 8, 31));

        dates.Should().Equal(
            new DateOnly(2026, 8, 3),
            new DateOnly(2026, 8, 10),
            new DateOnly(2026, 8, 17),
            new DateOnly(2026, 8, 24),
            new DateOnly(2026, 8, 31));
    }

    [Test]
    public void MatchingDates_IncludesFrom_WhenFromIsTargetDay()
    {
        // 2026-08-05 is een woensdag (woensdag = 2).
        IReadOnlyList<DateOnly> dates = WeeklyLessonExpander.MatchingDates(
            dayOfWeek: 2,
            from: new DateOnly(2026, 8, 5),
            to: new DateOnly(2026, 8, 12));

        dates.Should().Equal(new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 12));
    }

    [Test]
    public void MatchingDates_MapsSundayCorrectly()
    {
        // zondag = 6; 2026-08-09 is een zondag.
        IReadOnlyList<DateOnly> dates = WeeklyLessonExpander.MatchingDates(
            dayOfWeek: 6,
            from: new DateOnly(2026, 8, 3),
            to: new DateOnly(2026, 8, 16));

        dates.Should().Equal(new DateOnly(2026, 8, 9), new DateOnly(2026, 8, 16));
    }

    [Test]
    public void MatchingDates_ReturnsEmpty_WhenToBeforeFrom()
    {
        IReadOnlyList<DateOnly> dates = WeeklyLessonExpander.MatchingDates(
            dayOfWeek: 0,
            from: new DateOnly(2026, 8, 31),
            to: new DateOnly(2026, 8, 3));

        dates.Should().BeEmpty();
    }

    [Test]
    public void MatchingDates_ReturnsEmpty_WhenNoMatchingDayInRange()
    {
        // Dinsdag (1) in een venster ma 2026-08-03 t/m ma 2026-08-03 zonder dinsdag.
        IReadOnlyList<DateOnly> dates = WeeklyLessonExpander.MatchingDates(
            dayOfWeek: 1,
            from: new DateOnly(2026, 8, 3),
            to: new DateOnly(2026, 8, 3));

        dates.Should().BeEmpty();
    }
}
