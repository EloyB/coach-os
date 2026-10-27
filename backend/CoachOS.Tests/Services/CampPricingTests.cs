using CoachOS.Application.Camps;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using FluentAssertions;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

[TestFixture]
public class CampPricingTests
{
    private static Camp CampWith(out CampPrice discount, out CampPackage package)
    {
        Camp camp = new() { Id = Guid.NewGuid(), Price = 100m };
        discount = new CampPrice { Id = Guid.NewGuid(), CampId = camp.Id, Discount = 25m };
        package = new CampPackage { Id = Guid.NewGuid(), CampId = camp.Id, Name = "Ochtenden", Price = 40m };
        camp.Prices.Add(discount);
        camp.Packages.Add(package);
        return camp;
    }

    private static CampEnrollment Enrollment(
        Guid? priceId = null, Guid? packageId = null, EnrollmentStatus status = EnrollmentStatus.Confirmed)
        => new() { Id = Guid.NewGuid(), CampPriceId = priceId, CampPackageId = packageId, Status = status };

    [Test]
    public void UnitPrice_WithoutPackageOrDiscount_IsMainPrice()
        => CampPricing.UnitPrice(CampWith(out _, out _), Enrollment()).Should().Be(100m);

    [Test]
    public void UnitPrice_WithDiscount_SubtractsFromMainPrice()
    {
        Camp camp = CampWith(out CampPrice discount, out _);
        CampPricing.UnitPrice(camp, Enrollment(discount.Id)).Should().Be(75m);
    }

    [Test]
    public void UnitPrice_WithPackage_IsPackagePrice()
    {
        Camp camp = CampWith(out _, out CampPackage package);
        CampPricing.UnitPrice(camp, Enrollment(packageId: package.Id)).Should().Be(40m);
    }

    [Test]
    public void UnitPrice_WithPackageAndDiscount_SubtractsDiscountFromPackagePrice()
    {
        Camp camp = CampWith(out CampPrice discount, out CampPackage package);
        CampPricing.UnitPrice(camp, Enrollment(discount.Id, package.Id)).Should().Be(15m);
    }

    [Test]
    public void UnitPrice_DiscountLargerThanBase_IsZero()
    {
        Camp camp = CampWith(out CampPrice discount, out CampPackage package);
        discount.Discount = 60m;
        CampPricing.UnitPrice(camp, Enrollment(discount.Id, package.Id)).Should().Be(0m);
    }

    [Test]
    public void UnitPrice_UnknownPackageOrDiscount_FallsBackToMainPrice()
        => CampPricing.UnitPrice(CampWith(out _, out _), Enrollment(Guid.NewGuid(), Guid.NewGuid())).Should().Be(100m);

    [Test]
    public void Total_SumsMixedPackagesAndSkipsCancelled()
    {
        Camp camp = CampWith(out CampPrice discount, out CampPackage package);
        decimal total = CampPricing.Total(camp,
        [
            Enrollment(), Enrollment(discount.Id), Enrollment(packageId: package.Id),
            Enrollment(packageId: package.Id, status: EnrollmentStatus.Cancelled),
        ]);
        total.Should().Be(100m + 75m + 40m);
    }
}
