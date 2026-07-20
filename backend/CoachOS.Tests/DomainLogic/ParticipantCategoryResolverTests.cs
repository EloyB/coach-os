using CoachOS.Domain.Common;
using CoachOS.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.DomainLogic;

[TestFixture]
public class ParticipantCategoryResolverTests
{
    [Test]
    public void CalculateAge_BirthdayAlreadyPassed_ReturnsFullYears()
    {
        int age = ParticipantCategoryResolver.CalculateAge(
            new DateOnly(2000, 3, 1), new DateOnly(2026, 7, 20));

        age.Should().Be(26);
    }

    [Test]
    public void CalculateAge_BirthdayNotYetThisYear_SubtractsOne()
    {
        int age = ParticipantCategoryResolver.CalculateAge(
            new DateOnly(2000, 12, 31), new DateOnly(2026, 7, 20));

        age.Should().Be(25);
    }

    [Test]
    public void CalculateAge_OnExactBirthday_CountsAsNewAge()
    {
        int age = ParticipantCategoryResolver.CalculateAge(
            new DateOnly(2008, 7, 20), new DateOnly(2026, 7, 20));

        age.Should().Be(18);
    }

    [Test]
    public void CalculateAge_LeapDayBirthInNonLeapYear_TreatsMarchFirstAsBirthday()
    {
        // Geboren 29 feb 2008. In 2026 (geen schrikkeljaar) valt de verjaardag op 1 maart.
        int dayBefore = ParticipantCategoryResolver.CalculateAge(
            new DateOnly(2008, 2, 29), new DateOnly(2026, 2, 28));
        int onBirthday = ParticipantCategoryResolver.CalculateAge(
            new DateOnly(2008, 2, 29), new DateOnly(2026, 3, 1));

        dayBefore.Should().Be(17);
        onBirthday.Should().Be(18);
    }

    [Test]
    public void Resolve_AtAgeLimit_IsYouth()
    {
        // YouthMaxAge 17 betekent: 17 jaar telt nog als jeugd.
        ParticipantCategory category = ParticipantCategoryResolver.Resolve(
            new DateOnly(2009, 1, 1), youthMaxAge: 17, onDate: new DateOnly(2026, 7, 20));

        category.Should().Be(ParticipantCategory.Youth);
    }

    [Test]
    public void Resolve_OneYearAboveLimit_IsAdult()
    {
        ParticipantCategory category = ParticipantCategoryResolver.Resolve(
            new DateOnly(2008, 1, 1), youthMaxAge: 17, onDate: new DateOnly(2026, 7, 20));

        category.Should().Be(ParticipantCategory.Adult);
    }

    [Test]
    public void Resolve_CustomHigherLimit_ShiftsBoundary()
    {
        // Club met studententarief tot en met 21.
        ParticipantCategory category = ParticipantCategoryResolver.Resolve(
            new DateOnly(2005, 1, 1), youthMaxAge: 21, onDate: new DateOnly(2026, 7, 20));

        category.Should().Be(ParticipantCategory.Youth);
    }

    [Test]
    public void CalculateAge_FutureBirthDate_ClampsToZero()
    {
        int age = ParticipantCategoryResolver.CalculateAge(
            new DateOnly(2030, 1, 1), new DateOnly(2026, 7, 20));

        age.Should().Be(0);
    }
}
