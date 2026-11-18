using AwesomeAssertions;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using NUnit.Framework;

namespace CoachOS.Tests.Privacy;

/// <summary>
/// Opt-in (zet <c>COACHOS_TEST_PG</c>): draait de teller van actieve leerlingen tegen een echte PostgreSQL,
/// zodat de EF-vertaling (lower/btrim/DISTINCT, datumvenster) bewezen is en niet enkel in InMemory werkt.
/// </summary>
[TestFixture]
public class ActiveStudentCountPostgresTests : PlayerErasurePostgresBase
{
    [Test]
    public async Task Counts_unique_confirmed_people_across_lessons_and_camps_in_the_period()
    {
        Guid orgId = Guid.NewGuid();
        Guid otherOrgId = Guid.NewGuid();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateTime inPeriod = DateTime.UtcNow.AddDays(-10);
        DateTime beforePeriod = DateTime.UtcNow.AddDays(-400);

        await using (ApplicationDbContext db = Db())
        {
            Organization org = new() { Id = orgId, Name = "Club A", Email = $"a-{orgId:N}@example.test" };
            Organization other = new() { Id = otherOrgId, Name = "Club B", Email = $"b-{otherOrgId:N}@example.test" };
            TennisClub club = new() { Organization = org, Name = "Baan A" };
            TennisClub otherClub = new() { Organization = other, Name = "Baan B" };
            LessonSerie series = new() { Organization = org, TennisClub = club, Name = "Reeks", StartDate = today.AddDays(-30), EndDate = today.AddDays(30) };
            LessonSerie otherSeries = new() { Organization = other, TennisClub = otherClub, Name = "Reeks B", StartDate = today.AddDays(-30), EndDate = today.AddDays(30) };
            Camp camp = new() { Organization = org, TennisClub = club, Name = "Kamp", StartDate = today, EndDate = today.AddDays(3) };
            db.Subscriptions.Add(new Subscription
            {
                OrganizationId = orgId, Organization = org, Status = SubscriptionStatus.Active, StartDate = today.AddDays(-100),
                CurrentPeriodStart = today.AddDays(-100), CurrentPeriodEnd = today.AddDays(265), StudentLimit = 50,
            });

            Enrollment Lesson(Organization o, LessonSerie s, string name, DateOnly? dob, string contact, string? own, DateTime at,
                EnrollmentStatus status = EnrollmentStatus.Confirmed, DateTime? anonymizedAt = null) => new()
            {
                Organization = o, LessonSerie = s, StudentName = name, DateOfBirth = dob, ContactEmail = contact,
                StudentEmail = own, Status = status, EnrolledAt = at, AnonymizedAt = anonymizedAt,
            };

            db.Enrollments.AddRange(
                // Sophie: twee keer, andere hoofdletters/spaties/contactadres-fallback -> 1 persoon.
                Lesson(org, series, "Sophie De Vries", new DateOnly(1990, 5, 12), "x@example.be", "sophie@example.be", inPeriod),
                Lesson(org, series, "  SOPHIE   de vries ", new DateOnly(1990, 5, 12), " Sophie@Example.be ", null, inPeriod),
                // Zelfde naam, andere geboortedatum -> aparte persoon.
                Lesson(org, series, "Sophie De Vries", new DateOnly(2012, 1, 1), "sophie@example.be", null, inPeriod),
                // Tellen niet mee.
                Lesson(org, series, "Pending Piet", new DateOnly(1980, 1, 1), "piet@example.be", null, inPeriod, EnrollmentStatus.Pending),
                Lesson(org, series, "Oud Olaf", new DateOnly(1980, 1, 1), "olaf@example.be", null, beforePeriod),
                Lesson(org, series, "Weg Wim", new DateOnly(1980, 1, 1), "wim@example.be", null, inPeriod, anonymizedAt: DateTime.UtcNow),
                Lesson(other, otherSeries, "Vreemd Vera", new DateOnly(1980, 1, 1), "vera@example.be", null, inPeriod));

            db.CampEnrollments.AddRange(
                new CampEnrollment { OrganizationId = orgId, Camp = camp, ParticipantName = "Kamp Kim", ParticipantEmail = "kim@example.be", Status = EnrollmentStatus.Confirmed, EnrolledAt = inPeriod },
                new CampEnrollment { OrganizationId = orgId, Camp = camp, ParticipantName = " kamp  KIM", ParticipantEmail = "KIM@example.be", Status = EnrollmentStatus.Confirmed, EnrolledAt = inPeriod },
                new CampEnrollment { OrganizationId = orgId, Camp = camp, ParticipantName = "Wacht Wout", ParticipantEmail = "wout@example.be", Status = EnrollmentStatus.Waitlisted, EnrolledAt = inPeriod });
            await db.SaveChangesAsync();
        }

        await using ApplicationDbContext query = Db();
        ActiveStudentCountService sut = new(
            new SubscriptionRepository(query), new EnrollmentRepository(query), new CampEnrollmentRepository(query), TimeProvider.System);

        Result<ActiveStudentsDto> result = await sut.GetActiveStudentsAsync(orgId);

        result.IsSuccess.Should().BeTrue();
        // Sophie (1990), Sophie (2012), Kamp Kim.
        result.Value.Count.Should().Be(3);
        result.Value.Limit.Should().Be(50);
    }
}
