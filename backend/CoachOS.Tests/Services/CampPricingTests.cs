using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class CampPricingTests
{
    private static Camp PricedCamp() => new() { Id = Guid.NewGuid(), DayPrice = 32.5m, HalfDayPrice = 20m };

    [Test]
    public void AmountFor_FullDay_IsDayPrice()
        => CampPricing.AmountFor(PricedCamp(), [new(Guid.NewGuid(), true, true)]).Should().Be(32.5m);

    [Test]
    public void AmountFor_SinglePart_IsHalfDayPrice()
        => CampPricing.AmountFor(PricedCamp(), [new(Guid.NewGuid(), true, false)]).Should().Be(20m);

    [Test]
    public void AmountFor_ThreeFullDaysAndOneMorning_SumsPerDay()
        => CampPricing.AmountFor(PricedCamp(),
        [
            new(Guid.NewGuid(), true, true), new(Guid.NewGuid(), true, true),
            new(Guid.NewGuid(), true, true), new(Guid.NewGuid(), true, false),
        ]).Should().Be(117.5m);

    [Test]
    public void AmountFor_FreeCamp_IsZero()
        => CampPricing.AmountFor(new Camp(), [new(Guid.NewGuid(), true, true)]).Should().Be(0m);

    [Test]
    public void Total_OfParticipants_SumsTotalAmountAndSkipsCancelled()
        => CampPricing.Total(
        [
            new CampEnrollment { TotalAmount = 40m, Status = EnrollmentStatus.Confirmed },
            new CampEnrollment { TotalAmount = 20m, Status = EnrollmentStatus.PendingPayment },
            new CampEnrollment { TotalAmount = 99m, Status = EnrollmentStatus.Cancelled },
        ]).Should().Be(60m);
}
