using CoachOS.Application.Abstractions;
using CoachOS.Application.Camps;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CoachOS.Tests.Repositories;

/// <summary>
/// De betaalpaden (online en cash) laden het kamp via <c>GetByIdPublicAsync</c> en de inschrijving via
/// <c>GetByIdWithGroupAsync</c>. Gemockte servicetests verbergen een ontbrekende <c>Include</c>; deze test
/// laadt beide met een echte (InMemory) DbContext en rekent het bedrag met <see cref="CampPricing"/> uit.
/// </summary>
[TestFixture]
public class CampPricingLoadingTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private sealed class NoTenantContext : ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }

    private ApplicationDbContext _db = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options, new NoTenantContext());
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task PaymentLoadPath_SelectedTariffIsUsedForSoloAndGroupTotals()
    {
        Guid campId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        CampPrice members = new() { Id = Guid.NewGuid(), OrganizationId = OrgId, CampId = campId, Label = "Leden", Discount = 40m };
        TennisClub club = new() { Id = Guid.NewGuid(), OrganizationId = OrgId, Name = "TC De Aces" };
        _db.TennisClubs.Add(club);
        _db.Camps.Add(new Camp
        {
            Id = campId, OrganizationId = OrgId, TennisClubId = club.Id, Name = "Paaskamp", Price = 120m,
            IsActive = true, Prices = { members },
        });
        CampEnrollment solo = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, CampId = campId, CampPriceId = members.Id,
            Status = EnrollmentStatus.PendingPayment,
        };
        CampEnrollment leader = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, CampId = campId, CampPriceId = members.Id,
            Status = EnrollmentStatus.PendingPayment, CampEnrollmentGroupId = groupId,
        };
        CampEnrollment other = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, CampId = campId,
            Status = EnrollmentStatus.PendingPayment, CampEnrollmentGroupId = groupId,
        };
        _db.CampEnrollmentGroups.Add(new CampEnrollmentGroup
        {
            Id = groupId, OrganizationId = OrgId, CampId = campId, LeaderEnrollmentId = leader.Id,
        });
        _db.CampEnrollments.AddRange(solo, leader, other);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        CampRepository camps = new(_db);
        CampEnrollmentRepository enrollments = new(_db);

        Camp? camp = await camps.GetByIdPublicAsync(campId);
        CampEnrollment? loadedSolo = await enrollments.GetByIdWithGroupAsync(solo.Id);
        CampEnrollment? loadedLeader = await enrollments.GetByIdWithGroupAsync(leader.Id);

        camp!.Prices.Should().ContainSingle();
        CampPricing.Total(camp, [loadedSolo!]).Should().Be(80m);
        CampPricing.Total(camp, loadedLeader!.Group!.Members).Should().Be(80m + 120m);
    }

    [Test]
    public async Task PaymentLoadPath_PackagePriceAndDiscountAreUsed()
    {
        Guid campId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        CampPrice members = new() { Id = Guid.NewGuid(), OrganizationId = OrgId, CampId = campId, Label = "Leden", Discount = 10m };
        CampPackage mornings = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, CampId = campId, Name = "Ochtenden", Price = 60m,
            Slots = { new CampPackageSlot { Id = Guid.NewGuid(), OrganizationId = OrgId, Date = new DateOnly(2026, 7, 14), PartOrder = 0 } },
        };
        TennisClub club = new() { Id = Guid.NewGuid(), OrganizationId = OrgId, Name = "TC De Aces" };
        _db.TennisClubs.Add(club);
        _db.Camps.Add(new Camp
        {
            Id = campId, OrganizationId = OrgId, TennisClubId = club.Id, Name = "Zomerkamp", Price = 120m,
            IsActive = true, Prices = { members }, Packages = { mornings },
        });
        CampEnrollment solo = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, CampId = campId, CampPackageId = mornings.Id,
            CampPriceId = members.Id, Status = EnrollmentStatus.PendingPayment,
        };
        CampEnrollment leader = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, CampId = campId, CampPackageId = mornings.Id,
            CampPriceId = members.Id, Status = EnrollmentStatus.PendingPayment, CampEnrollmentGroupId = groupId,
        };
        CampEnrollment other = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, CampId = campId,
            Status = EnrollmentStatus.PendingPayment, CampEnrollmentGroupId = groupId,
        };
        _db.CampEnrollmentGroups.Add(new CampEnrollmentGroup
        {
            Id = groupId, OrganizationId = OrgId, CampId = campId, LeaderEnrollmentId = leader.Id,
        });
        _db.CampEnrollments.AddRange(solo, leader, other);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        CampRepository camps = new(_db);
        CampEnrollmentRepository enrollments = new(_db);

        Camp? camp = await camps.GetByIdPublicAsync(campId);
        CampEnrollment? loadedSolo = await enrollments.GetByIdWithGroupAsync(solo.Id);
        CampEnrollment? loadedLeader = await enrollments.GetByIdWithGroupAsync(leader.Id);

        camp!.Packages.Should().ContainSingle().Which.Slots.Should().ContainSingle();
        CampPricing.Total(camp, [loadedSolo!]).Should().Be(50m);
        CampPricing.Total(camp, loadedLeader!.Group!.Members).Should().Be(50m + 120m);
    }
}
