using CoachOS.Application.Privacy;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Repositories;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CoachOS.Tests.Privacy;

[TestFixture, NonParallelizable]
public class PlayerRetentionAnonymizationPostgresTests : PlayerErasurePostgresBase
{
    [Test]
    public async Task Retention_PaidOldPlayer_IsAnonymizedPaymentKeptAndNotPickedAgain()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment paid = Solo(org, Today.AddYears(-3));
        paid.StudentEmail = "player@example.test";
        paid.StudentPhone = "+32 470 00 00 00";
        paid.Notes = "Synthetic private note";
        EnrollmentForm form = new() { LessonSerie = paid.LessonSerie, OrganizationId = org };
        FormField field = new() { EnrollmentForm = form, Label = "Question", Type = FormFieldType.Text };
        db.Enrollments.Add(paid);
        db.FormResponses.Add(new FormResponse { Enrollment = paid, FormField = field, Value = "Synthetic private answer" });
        db.Payments.Add(new Payment
        {
            OrganizationId = org, Enrollment = paid, Amount = 120, Status = PaymentStatus.Paid,
            MolliePaymentId = "tr_synthetic", PaidAt = DateTime.UtcNow.AddYears(-3),
            Description = "Inschrijving Synthetic series – Synthetic Private",
        });
        await db.SaveChangesAsync();
        db.EmailOutboxMessages.Add(new EmailOutboxMessage
        {
            OrganizationId = org, EnrollmentId = paid.Id, Payload = "{\"name\":\"Synthetic Private\"}",
            Status = EmailOutboxStatuses.Completed,
        });
        await db.SaveChangesAsync();

        PlayerRetentionReport dry = (await Retention(db).RunAsync(org, dryRun: true)).Value!;
        dry.Should().Be(new PlayerRetentionReport(1, 0, 1, 0, 0, 0));

        PlayerRetentionReport applied = (await Retention(db).RunAsync(org, dryRun: false)).Value!;
        applied.Anonymized.Should().Be(1);

        Enrollment row = await db.Enrollments.AsNoTracking().SingleAsync(x => x.Id == paid.Id);
        row.StudentName.Should().Be("Geanonimiseerd");
        row.ContactEmail.Should().NotContain("example.test").And.EndWith(".invalid");
        row.StudentEmail.Should().BeNull();
        row.StudentPhone.Should().BeNull();
        row.DateOfBirth.Should().BeNull();
        row.Notes.Should().BeNull();
        row.AnonymizedAt.Should().NotBeNull();

        Payment payment = await db.Payments.AsNoTracking().SingleAsync(x => x.EnrollmentId == paid.Id);
        payment.Amount.Should().Be(120);
        payment.Status.Should().Be(PaymentStatus.Paid);
        payment.MolliePaymentId.Should().Be("tr_synthetic");
        payment.Description.Should().NotContain("Synthetic Private");

        (await db.FormResponses.CountAsync(x => x.EnrollmentId == paid.Id)).Should().Be(0);
        (await db.EmailOutboxMessages.CountAsync(x => x.EnrollmentId == paid.Id)).Should().Be(0);
        PrivacyErasureReceipt receipt = await db.PrivacyErasureReceipts.AsNoTracking().SingleAsync(x => x.SubjectId == paid.Id);
        receipt.Method.Should().Be(PrivacyErasureMethod.Anonymized);

        PlayerRetentionReport again = (await Retention(db).RunAsync(org, dryRun: false)).Value!;
        again.Should().Be(new PlayerRetentionReport(0, 0, 0, 0, 0, 0), "an anonymized row holds no personal data anymore");
    }

    [Test]
    public async Task Retention_TwoPaidCampersInSameCamp_BothAnonymizedWithoutUniqueIndexClash()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment blueprint = Solo(org, Today.AddYears(-3));
        Camp camp = new()
        {
            Organization = blueprint.Organization, TennisClub = blueprint.LessonSerie!.TennisClub, Name = "Old camp",
            StartDate = Today.AddYears(-3).AddDays(-5), EndDate = Today.AddYears(-3),
        };
        CampEnrollment first = new() { OrganizationId = org, Camp = camp, ParticipantName = "Camper One", ParticipantEmail = "one@example.test", Status = EnrollmentStatus.Confirmed };
        CampEnrollment second = new() { OrganizationId = org, Camp = camp, ParticipantName = "Camper Two", ParticipantEmail = "two@example.test", Status = EnrollmentStatus.Confirmed };
        db.CampEnrollments.AddRange(first, second);
        db.Payments.Add(new Payment { OrganizationId = org, CampEnrollment = first, Amount = 90, Status = PaymentStatus.Paid });
        db.Payments.Add(new Payment { OrganizationId = org, CampEnrollment = second, Amount = 90, Status = PaymentStatus.Paid });
        await db.SaveChangesAsync();

        PlayerRetentionReport applied = (await Retention(db).RunAsync(org, dryRun: false)).Value!;

        applied.Anonymized.Should().Be(2);
        List<CampEnrollment> rows = await db.CampEnrollments.AsNoTracking().Where(x => x.OrganizationId == org).ToListAsync();
        rows.Should().OnlyContain(x => x.ParticipantName == "Geanonimiseerd" && x.AnonymizedAt != null);
        rows.Select(x => x.ParticipantEmail).Should().OnlyHaveUniqueItems();
        (await db.Payments.CountAsync(x => x.OrganizationId == org)).Should().Be(2);
    }

    [Test]
    public async Task Retention_ManualErasureOfPaidPlayer_IsStillBlocked()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment paid = Solo(org, Today.AddYears(-3));
        db.Enrollments.Add(paid);
        db.Payments.Add(new Payment { OrganizationId = org, Enrollment = paid, Amount = 25, Status = PaymentStatus.Paid });
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        PlayerErasurePreviewDto preview = (await Service(db).PreviewAsync(org, admin, PrivacyErasureKind.Enrollment, paid.Id)).Value!;

        preview.CanErase.Should().BeFalse("manual erasure (#290) keeps blocking on payments");
        preview.BlockingReasons.Should().Contain(PlayerErasureBlockReasons.FinancialRecords);
    }

    [Test]
    public async Task Cleanup_DeletesOnlyOldCompletedMailAndExpiredLinks()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, Today.AddDays(30));
        db.Enrollments.Add(player);
        await db.SaveChangesAsync();
        DateTime now = DateTime.UtcNow;
        EmailOutboxMessage oldSent = Mail(org, player.Id, EmailOutboxStatuses.Completed, now.AddDays(-31));
        EmailOutboxMessage recentSent = Mail(org, player.Id, EmailOutboxStatuses.Completed, now.AddDays(-5));
        EmailOutboxMessage oldFailed = Mail(org, player.Id, EmailOutboxStatuses.Failed, now.AddDays(-60));
        MagicLinkToken expiredLink = new() { Email = "a@example.test", TokenHash = Guid.NewGuid().ToString("N"), ExpiresAt = now.AddMinutes(-1) };
        MagicLinkToken validLink = new() { Email = "b@example.test", TokenHash = Guid.NewGuid().ToString("N"), ExpiresAt = now.AddMinutes(30) };
        db.EmailOutboxMessages.AddRange(oldSent, recentSent, oldFailed);
        db.MagicLinkTokens.AddRange(expiredLink, validLink);
        await db.SaveChangesAsync();
        ScheduleAssignment assignment = await SeedAssignmentAsync(db, player, ScheduleAssignmentStatus.Confirmed);
        ScheduleAssignment waiting = await SeedAssignmentAsync(db, player, ScheduleAssignmentStatus.AwaitingConfirmation);
        AssignmentConfirmationToken oldToken = Token(org, assignment, player.Id, now.AddDays(-31));
        AssignmentConfirmationToken recentToken = Token(org, assignment, player.Id, now.AddDays(-5));
        AssignmentConfirmationToken oldUnanswered = Token(org, waiting, player.Id, now.AddDays(-60));
        db.AssignmentConfirmationTokens.AddRange(oldToken, recentToken, oldUnanswered);
        await db.SaveChangesAsync();
        PrivacyCleanupService service = new(new PrivacyCleanupRepository(db), new UnitOfWork(db), TimeProvider.System);

        PrivacyCleanupReport dry = (await service.RunAsync(dryRun: true)).Value!;
        dry.CompletedMails.Should().BeGreaterThanOrEqualTo(1);
        (await db.EmailOutboxMessages.AnyAsync(x => x.Id == oldSent.Id)).Should().BeTrue("a dry-run only counts");

        await service.RunAsync(dryRun: false);

        (await db.EmailOutboxMessages.AnyAsync(x => x.Id == oldSent.Id)).Should().BeFalse();
        (await db.EmailOutboxMessages.AnyAsync(x => x.Id == recentSent.Id)).Should().BeTrue();
        (await db.EmailOutboxMessages.AnyAsync(x => x.Id == oldFailed.Id)).Should().BeTrue("failed mail stays for investigation");
        (await db.MagicLinkTokens.AnyAsync(x => x.Id == expiredLink.Id)).Should().BeFalse();
        (await db.MagicLinkTokens.AnyAsync(x => x.Id == validLink.Id)).Should().BeTrue();
        (await db.AssignmentConfirmationTokens.AnyAsync(x => x.Id == oldToken.Id)).Should().BeFalse();
        (await db.AssignmentConfirmationTokens.AnyAsync(x => x.Id == recentToken.Id)).Should().BeTrue();
        (await db.AssignmentConfirmationTokens.AnyAsync(x => x.Id == oldUnanswered.Id))
            .Should().BeTrue("the non-responder list still needs unanswered links of a waiting assignment");
    }

    private static EmailOutboxMessage Mail(Guid org, Guid enrollmentId, string status, DateTime processedAt) => new()
    {
        OrganizationId = org, EnrollmentId = enrollmentId, Payload = "{}", Status = status,
        ProcessedAt = processedAt, CreatedAt = processedAt, UpdatedAt = processedAt,
    };

    private static AssignmentConfirmationToken Token(Guid org, ScheduleAssignment assignment, Guid enrollmentId, DateTime expiresAt) => new()
    {
        OrganizationId = org, ScheduleAssignment = assignment, EnrollmentId = enrollmentId,
        TokenHash = Guid.NewGuid().ToString("N"), ExpiresAt = expiresAt,
    };

    private static async Task<ScheduleAssignment> SeedAssignmentAsync(
        ApplicationDbContext db, Enrollment player, ScheduleAssignmentStatus status = ScheduleAssignmentStatus.Confirmed)
    {
        WeeklyTemplateEntry entry = new()
        {
            LessonSerieId = player.LessonSerieId!.Value, DayOfWeek = 1,
            StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0), MaxStudents = 4,
        };
        ScheduleAssignment assignment = new()
        {
            OrganizationId = player.OrganizationId, LessonSerieId = player.LessonSerieId!.Value,
            WeeklyTemplateEntry = entry, EnrollmentId = player.Id, Status = status,
        };
        db.ScheduleAssignments.Add(assignment);
        await db.SaveChangesAsync();
        return assignment;
    }
}
