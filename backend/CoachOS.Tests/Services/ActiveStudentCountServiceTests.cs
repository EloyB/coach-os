using CoachOS.Application.Abstractions;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using CoachOS.Tests.TestHelpers;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CoachOS.Tests.Services;

/// <summary>
/// Service + echte repositories op een InMemory-DbContext (de service rekent op de repo-projecties).
/// De SQL-vertaling zelf wordt in <see cref="Repositories_TranslateToSqlWithoutClientEvaluation"/> gecontroleerd.
/// </summary>
[TestFixture]
public class ActiveStudentCountServiceTests
{
    private static readonly Guid OrgId = Guid.NewGuid();
    private static readonly Guid OtherOrgId = Guid.NewGuid();

    // Periode 1/3/2026 t/m 28/2/2027; "vandaag" = 15/10/2026.
    private static readonly DateOnly PeriodStart = new(2026, 3, 1);
    private static readonly DateOnly PeriodEnd = new(2027, 2, 28);
    private static readonly FixedTimeProvider Clock = new(new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.Zero));
    private static readonly DateTime InPeriod = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

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
        AddSubscription(new Subscription
        {
            OrganizationId = OrgId, Status = SubscriptionStatus.Active, StartDate = PeriodStart,
            CurrentPeriodStart = PeriodStart, CurrentPeriodEnd = PeriodEnd, StudentLimit = 200,
        });
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    private void AddSubscription(Subscription subscription)
    {
        _db.Subscriptions.Add(subscription);
        _db.SaveChanges();
    }

    private ActiveStudentCountService CreateSut() => new(
        new SubscriptionRepository(_db), new EnrollmentRepository(_db), new CampEnrollmentRepository(_db), Clock);

    private void Lesson(
        string name, DateOnly? dob, string contactEmail, string? studentEmail = null,
        DateTime? enrolledAt = null, EnrollmentStatus status = EnrollmentStatus.Confirmed,
        Guid? org = null, DateTime? anonymizedAt = null)
    {
        _db.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(), OrganizationId = org ?? OrgId, StudentName = name, DateOfBirth = dob,
            ContactEmail = contactEmail, StudentEmail = studentEmail, Status = status,
            EnrolledAt = enrolledAt ?? InPeriod, AnonymizedAt = anonymizedAt,
            ActiveSince = status is EnrollmentStatus.Confirmed or EnrollmentStatus.PendingPayment ? enrolledAt ?? InPeriod : null,
        });
        _db.SaveChanges();
    }

    private void Camp(
        string name, string email, DateTime? enrolledAt = null,
        EnrollmentStatus status = EnrollmentStatus.Confirmed, Guid? org = null, DateTime? anonymizedAt = null,
        DateOnly? dob = null)
    {
        _db.CampEnrollments.Add(new CampEnrollment
        {
            Id = Guid.NewGuid(), OrganizationId = org ?? OrgId, CampId = Guid.NewGuid(), ParticipantName = name,
            ParticipantEmail = email, DateOfBirth = dob, Status = status, EnrolledAt = enrolledAt ?? InPeriod,
            AnonymizedAt = anonymizedAt, ActiveSince = status is EnrollmentStatus.Confirmed or EnrollmentStatus.PendingPayment ? enrolledAt ?? InPeriod : null,
        });
        _db.SaveChanges();
    }

    private async Task<ActiveStudentsDto> Run()
    {
        Result<ActiveStudentsDto> result = await CreateSut().GetActiveStudentsAsync(OrgId);
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    private static readonly DateOnly Dob = new(2012, 5, 4);

    [Test]
    public async Task SamePersonEnrolledTwice_CountsOnce_IgnoringCaseAndSpaces()
    {
        Lesson("Jan Peeters", Dob, "ouder@x.be");
        Lesson("  jan   PEETERS ", Dob, "OUDER@x.be ");

        (await Run()).Count.Should().Be(1);
    }

    [Test]
    public async Task SameNameDifferentDobOrEmail_AreSeparatePeople()
    {
        Lesson("Jan Peeters", Dob, "a@x.be");
        Lesson("Jan Peeters", Dob.AddDays(1), "a@x.be");
        Lesson("Jan Peeters", Dob, "b@x.be");

        (await Run()).Count.Should().Be(3);
    }

    [Test]
    public async Task StudentEmail_TakesPrecedenceOverContactEmail()
    {
        Lesson("Kind Een", Dob, "ouder@x.be", studentEmail: "kind@x.be");
        Lesson("Kind Een", Dob, "ander@x.be", studentEmail: "kind@x.be");
        Lesson("Kind Twee", Dob, "ouder@x.be");

        (await Run()).Count.Should().Be(2);
    }

    [TestCase(EnrollmentStatus.Pending)]
    [TestCase(EnrollmentStatus.Waitlisted)]
    [TestCase(EnrollmentStatus.Cancelled)]
    public async Task StatusesWithoutPersonAction_AreNotCounted(EnrollmentStatus status)
    {
        Lesson("Les Persoon", Dob, "a@x.be", status: status);
        Camp("Kamp Persoon", "b@x.be", status: status);

        (await Run()).Count.Should().Be(0);
    }

    [Test]
    public async Task PeriodBoundaries_FirstAndLastDayInclusive_DayBeforeAndAfterExcluded()
    {
        Lesson("Eerste Dag", Dob, "1@x.be", enrolledAt: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        Lesson("Laatste Dag", Dob, "2@x.be", enrolledAt: new DateTime(2027, 2, 28, 23, 59, 59, DateTimeKind.Utc));
        Lesson("Dag Ervoor", Dob, "3@x.be", enrolledAt: new DateTime(2026, 2, 28, 23, 59, 59, DateTimeKind.Utc));
        Lesson("Dag Erna", Dob, "4@x.be", enrolledAt: new DateTime(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        Camp("Kamp Eerste", "5@x.be", enrolledAt: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        Camp("Kamp Erna", "6@x.be", enrolledAt: new DateTime(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc));

        (await Run()).Count.Should().Be(3);
    }

    [Test]
    public async Task LessonsAndCamps_AreCountedTogether()
    {
        Lesson("Les Persoon", Dob, "a@x.be");
        Camp("Kamp Persoon", "b@x.be");
        Camp("kamp  persoon", "B@x.be");

        (await Run()).Count.Should().Be(2);
    }

    [Test]
    public async Task SamePersonInCampAndSeries_CountsOnce_WhenBothHaveSameDob()
    {
        Lesson("Sam Janssens", Dob, "s@x.be");
        Camp("sam  janssens", "S@x.be", dob: Dob);
        (await Run()).Count.Should().Be(1);
    }

    [Test]
    public async Task CampWithoutDob_IsSeparateFromSeriesWithDob_ButCampsWithSameDobMerge()
    {
        Lesson("Sam Janssens", Dob, "s@x.be");
        Camp("Sam Janssens", "s@x.be"); // oude kampinschrijving zonder geboortedatum: bewuste overschatting
        (await Run()).Count.Should().Be(2);

        Camp("Kim Peeters", "k@x.be", dob: Dob);
        Camp("Kim Peeters", "k@x.be", dob: Dob);
        Camp("Kim Peeters", "k@x.be", dob: new DateOnly(2013, 1, 1));
        (await Run()).Count.Should().Be(4);
    }

    [Test]
    public async Task CountsFromActiveSince_NotEnrollmentMoment()
    {
        DateTime before = new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime inside = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        // Ingeschreven voor het jaar, bevestigd erin: telt.
        _db.Enrollments.Add(new Enrollment
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, StudentName = "Laat Bevestigd", DateOfBirth = Dob,
            ContactEmail = "l@x.be", Status = EnrollmentStatus.Confirmed, EnrolledAt = before, ActiveSince = inside,
        });
        // Ingeschreven in het jaar, nog niet bevestigd: telt niet.
        Lesson("Nog Pending", Dob, "p@x.be", status: EnrollmentStatus.Pending);
        // Ingeschreven in het jaar, bevestigd na het jaar: telt niet.
        _db.CampEnrollments.Add(new CampEnrollment
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, CampId = Guid.NewGuid(), ParticipantName = "Te Laat",
            ParticipantEmail = "t@x.be", Status = EnrollmentStatus.Confirmed, EnrolledAt = inside,
            ActiveSince = new DateTime(2027, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        _db.SaveChanges();

        (await Run()).Count.Should().Be(1);
    }

    [Test]
    public async Task PendingPaymentAfterPersonConfirmed_CountsEvenWhenCashNotMarkedPaid()
    {
        Lesson("Wacht Op Cash", Dob, "w@x.be", status: EnrollmentStatus.PendingPayment);
        Camp("Kamp Cash", "k@x.be", status: EnrollmentStatus.PendingPayment);

        (await Run()).Count.Should().Be(2);
    }

    [Test]
    public void ActiveSince_IsOnlyStampedOnDirectInserts_NotOnStatusChanges()
    {
        Enrollment pending = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, StudentName = "Wacht", ContactEmail = "w@x.be",
            Status = EnrollmentStatus.Pending, EnrolledAt = InPeriod,
        };
        CampEnrollment campPending = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, CampId = Guid.NewGuid(), ParticipantName = "Wacht Kamp",
            ParticipantEmail = "wk@x.be", Status = EnrollmentStatus.Pending, EnrolledAt = InPeriod,
        };
        CampEnrollment campPaid = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, CampId = Guid.NewGuid(), ParticipantName = "Kamper",
            ParticipantEmail = "k@x.be", Status = EnrollmentStatus.PendingPayment, EnrolledAt = InPeriod,
        };
        Enrollment direct = new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, StudentName = "Direct", ContactEmail = "d@x.be",
            Status = EnrollmentStatus.Confirmed, EnrolledAt = InPeriod,
        };
        _db.Enrollments.AddRange(pending, direct);
        _db.CampEnrollments.AddRange(campPending, campPaid);
        _db.SaveChanges();

        pending.ActiveSince.Should().BeNull();
        campPending.ActiveSince.Should().BeNull();
        campPaid.ActiveSince.Should().NotBeNull(); // kamp: persoon schreef zichzelf in
        direct.ActiveSince.Should().NotBeNull();   // handmatig bevestigd

        // Een statuswijziging door de beheerder (bv. cash betaald) zet het moment niet.
        pending.Status = EnrollmentStatus.Confirmed;
        _db.SaveChanges();
        pending.ActiveSince.Should().BeNull();
    }

    [Test]
    public async Task OtherOrganization_IsNotCounted()
    {
        Lesson("Eigen", Dob, "a@x.be");
        Lesson("Vreemd", Dob, "b@x.be", org: OtherOrgId);
        Camp("Vreemd Kamp", "c@x.be", org: OtherOrgId);

        (await Run()).Count.Should().Be(1);
    }

    [Test]
    public async Task AnonymizedRows_AreNotCounted()
    {
        Lesson("Weg", Dob, "a@x.be", anonymizedAt: InPeriod);
        Camp("Weg Kamp", "b@x.be", anonymizedAt: InPeriod);
        Lesson("Blijft", Dob, "c@x.be");

        (await Run()).Count.Should().Be(1);
    }

    [Test]
    public async Task SubscriptionWithCurrentPeriod_UsesThatPeriodAndLimit()
    {
        ActiveStudentsDto dto = await Run();

        dto.PeriodStart.Should().Be(PeriodStart);
        dto.PeriodEnd.Should().Be(PeriodEnd);
        dto.Limit.Should().Be(200);
    }

    [Test]
    public async Task NoLimit_IsPassedAsNull()
    {
        Guid org = Guid.NewGuid();
        AddSubscription(new Subscription
        {
            OrganizationId = org, Status = SubscriptionStatus.Legacy, StartDate = new DateOnly(2026, 1, 1), StudentLimit = null,
        });

        Result<ActiveStudentsDto> result = await CreateSut().GetActiveStudentsAsync(org);

        result.Value!.Limit.Should().BeNull();
    }

    [Test]
    public async Task TrialWithoutCurrentPeriod_UsesStartDateYear()
    {
        Guid org = Guid.NewGuid();
        AddSubscription(new Subscription
        {
            OrganizationId = org, Status = SubscriptionStatus.Trialing, StartDate = new DateOnly(2026, 9, 20),
            TrialEndsAt = new DateOnly(2026, 10, 19),
        });

        ActiveStudentsDto dto = (await CreateSut().GetActiveStudentsAsync(org)).Value!;

        dto.PeriodStart.Should().Be(new DateOnly(2026, 9, 20));
        dto.PeriodEnd.Should().Be(new DateOnly(2027, 9, 19));
    }

    [Test]
    public async Task LegacyAfterMoreThanOneYear_UsesSecondYear()
    {
        Guid org = Guid.NewGuid();
        AddSubscription(new Subscription
        {
            OrganizationId = org, Status = SubscriptionStatus.Legacy, StartDate = new DateOnly(2024, 11, 1),
        });

        // Vandaag 15/10/2026: jaar 1 = 1/11/2024..31/10/2025, jaar 2 = 1/11/2025..31/10/2026.
        ActiveStudentsDto dto = (await CreateSut().GetActiveStudentsAsync(org)).Value!;

        dto.PeriodStart.Should().Be(new DateOnly(2025, 11, 1));
        dto.PeriodEnd.Should().Be(new DateOnly(2026, 10, 31));
    }

    [Test]
    public void LegacyPeriodEndsToday_StillInCurrentYear_NextDayRollsOver()
    {
        Subscription sub = new() { StartDate = new DateOnly(2025, 10, 16), Status = SubscriptionStatus.Legacy };

        ActiveStudentCountService.ResolvePeriod(sub, new DateOnly(2026, 10, 15)).End.Should().Be(new DateOnly(2026, 10, 15));
        ActiveStudentCountService.ResolvePeriod(sub, new DateOnly(2026, 10, 16)).Start.Should().Be(new DateOnly(2026, 10, 16));
    }

    [Test]
    public async Task NoSubscription_ReturnsNotFound()
    {
        Result<ActiveStudentsDto> result = await CreateSut().GetActiveStudentsAsync(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be(ErrorCodes.NotFound);
    }

    [Test]
    public void Repositories_TranslateToSqlWithoutClientEvaluation()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused").Options;
        using ApplicationDbContext pg = new(options, new NoTenantContext());
        DateTime from = InPeriod;

        string lessons = pg.Enrollments
            .Where(e => e.OrganizationId == OrgId && e.Status == EnrollmentStatus.Confirmed
                && e.AnonymizedAt == null && e.ActiveSince >= from && e.ActiveSince < from)
            .Select(e => new StudentIdentityKey(
                e.StudentName.Trim().ToLower(), e.DateOfBirth,
                e.StudentEmail == null ? null : e.StudentEmail.Trim().ToLower(), e.ContactEmail.Trim().ToLower()))
            .Distinct().ToQueryString();

        lessons.Should().Contain("DISTINCT").And.Contain("lower(btrim(");
    }
}
