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
    private static Camp CampWith(out CampPrice option)
    {
        Camp camp = new() { Id = Guid.NewGuid(), Price = 100m };
        option = new CampPrice { Id = Guid.NewGuid(), CampId = camp.Id, Amount = 75m };
        camp.Prices.Add(option);
        return camp;
    }

    private static CampEnrollment Enrollment(Guid? priceId = null, EnrollmentStatus status = EnrollmentStatus.Confirmed)
        => new() { Id = Guid.NewGuid(), CampPriceId = priceId, Status = status };

    [Test]
    public void UnitPrice_WithoutOption_IsMainPrice()
        => CampPricing.UnitPrice(CampWith(out _), Enrollment()).Should().Be(100m);

    [Test]
    public void UnitPrice_WithOption_IsOptionAmount()
    {
        Camp camp = CampWith(out CampPrice option);
        CampPricing.UnitPrice(camp, Enrollment(option.Id)).Should().Be(75m);
    }

    [Test]
    public void UnitPrice_UnknownOption_FallsBackToMainPrice()
        => CampPricing.UnitPrice(CampWith(out _), Enrollment(Guid.NewGuid())).Should().Be(100m);

    [Test]
    public void Total_SumsPerParticipantAndSkipsCancelled()
    {
        Camp camp = CampWith(out CampPrice option);
        decimal total = CampPricing.Total(camp,
        [
            Enrollment(), Enrollment(option.Id), Enrollment(option.Id, EnrollmentStatus.Cancelled),
        ]);
        total.Should().Be(175m);
    }

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
