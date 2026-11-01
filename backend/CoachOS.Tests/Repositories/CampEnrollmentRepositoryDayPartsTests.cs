using CoachOS.Application.Abstractions;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CoachOS.Tests.Repositories;

[TestFixture]
public class CampEnrollmentRepositoryDayPartsTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private sealed class NoTenantContext : ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }

    private ApplicationDbContext _db = null!;
    private CampEnrollmentRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options, new NoTenantContext());
        _sut = new CampEnrollmentRepository(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private async Task<(Guid CampId, Guid DayId)> CampWithDay()
    {
        TennisClub club = new() { Id = Guid.NewGuid(), OrganizationId = OrgId, Name = "TC" };
        Camp camp = new() { Id = Guid.NewGuid(), OrganizationId = OrgId, TennisClubId = club.Id, Name = "Kamp", IsActive = true };
        CampDay day = new() { Id = Guid.NewGuid(), CampId = camp.Id, OrganizationId = OrgId, Date = new DateOnly(2026, 12, 21) };
        _db.TennisClubs.Add(club);
        _db.Camps.Add(camp);
        _db.CampDays.Add(day);
        await _db.SaveChangesAsync();
        return (camp.Id, day.Id);
    }

    private async Task Enroll(Guid campId, Guid dayId, EnrollmentStatus status, bool morning, bool afternoon)
    {
        CampEnrollment e = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, CampId = campId, Status = status,
            ParticipantName = "X", ParticipantEmail = $"{Guid.NewGuid()}@x.be",
        };
        e.Days.Add(new CampEnrollmentDay { Id = Guid.NewGuid(), OrganizationId = OrgId, CampDayId = dayId, Morning = morning, Afternoon = afternoon });
        _db.CampEnrollments.Add(e);
        await _db.SaveChangesAsync();
    }

    [Test]
    public async Task CountActivePerDayPart_FullDayCountsInBothPartsAndCancelledIsIgnored()
    {
        (Guid campId, Guid dayId) = await CampWithDay();
        await Enroll(campId, dayId, EnrollmentStatus.Confirmed, true, true);
        await Enroll(campId, dayId, EnrollmentStatus.PendingPayment, true, false);
        await Enroll(campId, dayId, EnrollmentStatus.Cancelled, true, true);

        Dictionary<(Guid CampDayId, CampDayPart Part), int> counts = await _sut.CountActivePerDayPartAsync(campId);

        counts[(dayId, CampDayPart.Morning)].Should().Be(2);
        counts[(dayId, CampDayPart.Afternoon)].Should().Be(1);
    }

    [Test]
    public async Task CountActivePerDayPart_OtherCampIsNotCounted()
    {
        (Guid campId, Guid dayId) = await CampWithDay();
        (Guid otherCampId, Guid otherDayId) = await CampWithDay();
        await Enroll(otherCampId, otherDayId, EnrollmentStatus.Confirmed, true, true);

        Dictionary<(Guid CampDayId, CampDayPart Part), int> counts = await _sut.CountActivePerDayPartAsync(campId);

        counts.Should().BeEmpty();
        dayId.Should().NotBe(otherDayId);
    }

    [Test]
    public async Task AnyHalfDayEnrollment_CountsOnlySelectionsThatDoNotCoverTheWholeDay()
    {
        (Guid campId, Guid dayId) = await CampWithDay();
        CampDay day = _db.CampDays.Single(d => d.Id == dayId);
        day.MorningStart = new TimeOnly(9, 15);
        day.MorningEnd = new TimeOnly(12, 0);
        day.AfternoonStart = new TimeOnly(13, 15);
        day.AfternoonEnd = new TimeOnly(16, 0);
        await _db.SaveChangesAsync();
        CampRepository camps = new(_db);

        await Enroll(campId, dayId, EnrollmentStatus.Confirmed, true, true);
        (await camps.AnyHalfDayEnrollmentAsync(campId)).Should().BeFalse();

        await Enroll(campId, dayId, EnrollmentStatus.Confirmed, true, false);
        (await camps.AnyHalfDayEnrollmentAsync(campId)).Should().BeTrue();
    }

    [Test]
    public async Task AnyHalfDayEnrollment_ADayWithOnePartChosenInFullIsNotAHalfDay()
    {
        (Guid campId, Guid dayId) = await CampWithDay();
        CampDay day = _db.CampDays.Single(d => d.Id == dayId);
        day.MorningStart = new TimeOnly(9, 15);
        day.MorningEnd = new TimeOnly(12, 0);
        await _db.SaveChangesAsync();
        await Enroll(campId, dayId, EnrollmentStatus.Confirmed, true, false);

        (await new CampRepository(_db).AnyHalfDayEnrollmentAsync(campId)).Should().BeFalse();
    }
}
