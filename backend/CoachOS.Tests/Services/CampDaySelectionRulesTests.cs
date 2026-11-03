using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Models;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class CampDaySelectionRulesTests
{
    private static readonly TimeOnly T0915 = new(9, 15), T1200 = new(12, 0), T1315 = new(13, 15), T1600 = new(16, 0);

    private static CampDay FullDay(DateOnly date) => new()
    {
        Id = Guid.NewGuid(), Date = date,
        MorningStart = T0915, MorningEnd = T1200, AfternoonStart = T1315, AfternoonEnd = T1600,
    };

    private static CampDay MorningOnly(DateOnly date) => new()
    {
        Id = Guid.NewGuid(), Date = date, MorningStart = T0915, MorningEnd = T1200,
    };

    private static Camp CampOf(bool allowHalfDays, params CampDay[] days)
    {
        Camp camp = new() { Id = Guid.NewGuid(), AllowHalfDays = allowHalfDays };
        foreach (CampDay d in days) camp.Days.Add(d);
        return camp;
    }

    private static readonly DateOnly Monday = new(2026, 12, 21);

    [Test]
    public void Validate_EmptySelection_IsRejected()
    {
        Camp camp = CampOf(true, FullDay(Monday));
        Error? error = CampDaySelectionRules.Validate(camp, []);
        error.Should().NotBeNull();
        error!.Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public void Validate_SameDayTwice_IsRejected()
    {
        CampDay day = FullDay(Monday);
        Camp camp = CampOf(true, day);
        CampDaySelectionRules.Validate(camp,
            [new(day.Id, true, true), new(day.Id, true, false)])!.Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public void Validate_DayOfAnotherCamp_IsRejected()
    {
        Camp camp = CampOf(true, FullDay(Monday));
        CampDaySelectionRules.Validate(camp, [new(Guid.NewGuid(), true, true)])!.Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public void Validate_PartThatDoesNotExistOnThatDay_IsRejected()
    {
        CampDay day = MorningOnly(Monday);
        Camp camp = CampOf(true, day);
        Error? error = CampDaySelectionRules.Validate(camp, [new(day.Id, false, true)]);
        error!.Code.Should().Be(ErrorCodes.Validation);
        error.Message.Should().Contain("namiddag");
    }

    [Test]
    public void Validate_NoPartChosen_IsRejected()
    {
        CampDay day = FullDay(Monday);
        Camp camp = CampOf(true, day);
        CampDaySelectionRules.Validate(camp, [new(day.Id, false, false)])!.Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public void Validate_HalfDayWhenHalfDaysNotAllowed_IsRejected()
    {
        CampDay day = FullDay(Monday);
        Camp camp = CampOf(false, day);
        CampDaySelectionRules.Validate(camp, [new(day.Id, true, false)])!.Code.Should().Be(ErrorCodes.Validation);
    }

    [Test]
    public void Validate_SinglePartDayFullyChosenWhenHalfDaysNotAllowed_IsAccepted()
    {
        CampDay day = MorningOnly(Monday);
        Camp camp = CampOf(false, day);
        CampDaySelectionRules.Validate(camp, [new(day.Id, true, false)]).Should().BeNull();
    }

    [Test]
    public void Validate_ValidSelection_ReturnsNull()
    {
        CampDay full = FullDay(Monday), half = MorningOnly(Monday.AddDays(3));
        Camp camp = CampOf(true, full, half);
        CampDaySelectionRules.Validate(camp, [new(full.Id, true, false), new(half.Id, true, false)]).Should().BeNull();
    }
}
