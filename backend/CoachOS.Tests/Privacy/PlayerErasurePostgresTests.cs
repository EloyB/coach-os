using CoachOS.Application.Privacy;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Persistence;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NUnit.Framework;

namespace CoachOS.Tests.Privacy;

[TestFixture, NonParallelizable]
public class PlayerErasurePostgresTests : PlayerErasurePostgresBase
{
    private const PrivacyErasureKind Lesson = PrivacyErasureKind.Enrollment;

    [Test]
    public async Task Erase_RequiresExactNameWritesReceiptAndIsInvisibleToOtherOrganization()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, Today.AddMonths(-6));
        db.Enrollments.Add(player);
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);
        PlayerErasureService service = Service(db);

        Result<PlayerErasurePreviewDto> preview = await service.PreviewAsync(org, admin, Lesson, player.Id);
        preview.Value!.CanErase.Should().BeTrue("a club may erase early on request; only the automatic job waits 24 months");
        preview.Value.RetentionPeriodPassed.Should().BeFalse();
        (await service.PreviewAsync(Guid.NewGuid(), admin, Lesson, player.Id)).Errors.Single().Code.Should().Be(ErrorCodes.Forbidden);
        (await service.EraseAsync(org, admin, Lesson, player.Id, new ErasePlayerRequest("Wrong name"))).IsSuccess.Should().BeFalse();
        (await db.Enrollments.AnyAsync(x => x.Id == player.Id)).Should().BeTrue();

        Result<PlayerErasureResultDto> erased = await service.EraseAsync(org, admin, Lesson, player.Id,
            new ErasePlayerRequest(player.StudentName, null, preview.Value.UpdatedAt));

        erased.IsSuccess.Should().BeTrue();
        (await db.Enrollments.AnyAsync(x => x.Id == player.Id)).Should().BeFalse();
        (await ReceiptCountAsync(db, org, player.Id)).Should().Be(1);
        (await service.PreviewAsync(org, admin, Lesson, player.Id)).Errors.Single().Code.Should().Be(ErrorCodes.NotFound);
    }

    [Test]
    public async Task Preview_FutureParticipationAndUnfinishedMail_Block()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, Today.AddDays(1));
        db.Enrollments.Add(player);
        await db.SaveChangesAsync();
        db.EmailOutboxMessages.Add(new EmailOutboxMessage { OrganizationId = org, EnrollmentId = player.Id, Payload = "{}", Status = EmailOutboxStatuses.Pending });
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        PlayerErasurePreviewDto preview = (await Service(db).PreviewAsync(org, admin, Lesson, player.Id)).Value!;

        preview.CanErase.Should().BeFalse();
        preview.BlockingReasons.Should().Contain([PlayerErasureBlockReasons.ActiveOrFutureParticipation, PlayerErasureBlockReasons.PendingMail]);
    }

    [Test]
    public async Task Erase_ConcurrentWriterOnTarget_IsAwaitedAndRecheckedInsideTransaction()
    {
        Guid org = Guid.NewGuid();
        Guid id;
        string name;
        Guid admin;
        await using (ApplicationDbContext seed = Db())
        {
            Enrollment player = Solo(org, Today.AddYears(-3));
            seed.Enrollments.Add(player);
            await seed.SaveChangesAsync();
            (id, name) = (player.Id, player.StudentName);
            admin = await SeedAdminAsync(seed, org);
        }

        await using ApplicationDbContext writer = Db();
        await using IDbContextTransaction tx = await writer.Database.BeginTransactionAsync();
        await writer.Enrollments.Where(x => x.OrganizationId == org && x.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, EnrollmentStatus.PendingPayment));
        await using ApplicationDbContext eraser = Db();
        Task<Result<PlayerErasureResultDto>> erase = Service(eraser).EraseAsync(org, admin, Lesson, id, new ErasePlayerRequest(name));
        await Task.Delay(300);
        erase.IsCompleted.Should().BeFalse("SELECT ... FOR UPDATE waits for the running writer on this row");
        await tx.CommitAsync();

        Result<PlayerErasureResultDto> result = await erase;
        result.Errors.Single().Should().Be(PlayerErasureErrors.Blocked);
        (await eraser.Enrollments.AnyAsync(x => x.Id == id)).Should().BeTrue();
    }

    [Test]
    public async Task Preview_DoesNotTakeLocks_AndOtherClubsAreNotBlockedByAnErasure()
    {
        Guid org = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, Today.AddYears(-3));
        Enrollment foreign = Solo(other, Today.AddYears(-3));
        db.Enrollments.AddRange(player, foreign);
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        await using ApplicationDbContext locker = Db();
        await using IDbContextTransaction tx = await locker.Database.BeginTransactionAsync();
        await locker.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Enrollments\" WHERE \"Id\" = {player.Id} FOR UPDATE");
        Task<Result<PlayerErasurePreviewDto>> preview = Service(db).PreviewAsync(org, admin, Lesson, player.Id);
        (await Task.WhenAny(preview, Task.Delay(3000))).Should().BeSameAs(preview, "a preview must never wait on locks");

        await using ApplicationDbContext otherWriter = Db();
        Task update = otherWriter.Enrollments.Where(x => x.Id == foreign.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Notes, "still writable"));
        (await Task.WhenAny(update, Task.Delay(3000))).Should().BeSameAs(update, "no table locks: other clubs keep writing");
        await tx.RollbackAsync();
    }

    [Test]
    public async Task Erase_RowLockedLongerThanTimeout_ReturnsLockTimeoutMessage()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, Today.AddYears(-3));
        db.Enrollments.Add(player);
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        await using ApplicationDbContext locker = Db();
        await using IDbContextTransaction tx = await locker.Database.BeginTransactionAsync();
        await locker.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM \"Enrollments\" WHERE \"Id\" = {player.Id} FOR UPDATE");
        await using ApplicationDbContext eraser = Db();

        Result<PlayerErasureResultDto> result = await Service(eraser).EraseAsync(org, admin, Lesson, player.Id, new ErasePlayerRequest(player.StudentName));

        result.Errors.Single().Should().Be(PlayerErasureErrors.LockTimeout);
        await tx.RollbackAsync();
        (await db.Enrollments.AnyAsync(x => x.Id == player.Id)).Should().BeTrue();
    }

    [Test]
    public async Task Erase_ForeignKeyFailure_RollsBackEarlierDeletesAndReturnsDependencyMessage()
    {
        Guid org = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, Today.AddYears(-3));
        Enrollment foreign = Solo(other, Today.AddYears(-3));
        EnrollmentForm ownForm = new() { OrganizationId = org, LessonSerie = player.LessonSerie! };
        EnrollmentForm foreignForm = new() { OrganizationId = other, LessonSerie = foreign.LessonSerie! };
        FormField ownField = new() { EnrollmentForm = ownForm, Label = "Own", Type = FormFieldType.Text };
        FormField foreignField = new() { EnrollmentForm = foreignForm, Label = "Foreign", Type = FormFieldType.Text };
        db.Enrollments.AddRange(player, foreign);
        db.FormResponses.AddRange(
            new FormResponse { Enrollment = player, FormField = ownField, Value = "Own private answer" },
            new FormResponse { Enrollment = player, FormField = foreignField, Value = "Corrupt foreign dependency" });
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        Result<PlayerErasureResultDto> result = await Service(db).EraseAsync(org, admin, Lesson, player.Id, new ErasePlayerRequest(player.StudentName));

        result.Errors.Single().Should().Be(PlayerErasureErrors.DependencyConflict);
        (await ReceiptCountAsync(db, org, player.Id)).Should().Be(0);
        (await db.FormResponses.CountAsync(x => x.EnrollmentId == player.Id)).Should().Be(2, "the first DELETE rolls back with the failed FK");
        (await db.Enrollments.AnyAsync(x => x.Id == foreign.Id)).Should().BeTrue();
    }

    [Test]
    public async Task Preview_StandaloneReschedulingChainToFutureLesson_Blocks()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, Today.AddYears(-3));
        TennisClub club = player.LessonSerie!.TennisClub;
        Lesson replacement = new() { Organization = player.Organization, TennisClub = club, Date = Today.AddDays(10), StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(15, 0), MaxStudents = 4 };
        Lesson original = new() { Organization = player.Organization, TennisClub = club, Date = Today.AddYears(-3), StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(15, 0), MaxStudents = 4, IsCancelled = true, RescheduledToLesson = replacement };
        player.LessonSerie = null;
        player.Lesson = original;
        db.Enrollments.Add(player);
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        PlayerErasurePreviewDto preview = (await Service(db).PreviewAsync(org, admin, Lesson, player.Id)).Value!;

        preview.BlockingReasons.Should().Contain(PlayerErasureBlockReasons.ActiveOrFutureParticipation);
    }

    [Test]
    public async Task Preview_FutureCampWithStudentAddressDifferentFromParentContact_Blocks()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment old = Solo(org, Today.AddYears(-3));
        old.ContactEmail = "parent@example.test";
        old.StudentEmail = "student@example.test";
        Camp camp = new() { Organization = old.Organization, TennisClub = old.LessonSerie!.TennisClub, Name = "Future camp", StartDate = Today.AddDays(10), EndDate = Today.AddDays(15) };
        db.Enrollments.Add(old);
        db.CampEnrollments.Add(new CampEnrollment { OrganizationId = org, Camp = camp, ParticipantName = old.StudentName, ParticipantEmail = old.StudentEmail, Status = EnrollmentStatus.Confirmed });
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        PlayerErasurePreviewDto preview = (await Service(db).PreviewAsync(org, admin, Lesson, old.Id)).Value!;

        preview.BlockingReasons.Should().Contain(PlayerErasureBlockReasons.ActiveOrFutureParticipation);
    }

    [Test]
    public async Task PricedOffering_WithoutPaymentRows_CanBeErased_ButAnyPaymentBlocks()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment free = Solo(org, Today.AddYears(-3));
        free.LessonSerie!.Price = 25;
        Enrollment paid = new() { Organization = free.Organization, LessonSerie = free.LessonSerie, StudentName = "Paid Player", ContactEmail = "paid@example.test", DateOfBirth = new DateOnly(2001, 1, 1), Status = EnrollmentStatus.Confirmed };
        db.Enrollments.AddRange(free, paid);
        db.Payments.Add(new Payment { OrganizationId = org, Enrollment = paid, Amount = 25, Status = PaymentStatus.Paid });
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);
        PlayerErasureService service = Service(db);

        (await service.PreviewAsync(org, admin, Lesson, free.Id)).Value!.CanErase.Should().BeTrue();
        (await service.PreviewAsync(org, admin, Lesson, paid.Id)).Value!.BlockingReasons
            .Should().Contain(PlayerErasureBlockReasons.FinancialRecords);
    }

    [Test]
    public async Task Preview_CampEnrollmentWithPendingMail_Blocks()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment blueprint = Solo(org, Today.AddYears(-3));
        Camp camp = new() { Organization = blueprint.Organization, TennisClub = blueprint.LessonSerie!.TennisClub, Name = "Old camp", StartDate = Today.AddYears(-3).AddDays(-5), EndDate = Today.AddYears(-3) };
        CampEnrollment camper = new() { OrganizationId = org, Camp = camp, ParticipantName = "Camper", ParticipantEmail = "camper@example.test", Status = EnrollmentStatus.Confirmed };
        db.CampEnrollments.Add(camper);
        await db.SaveChangesAsync();
        db.EmailOutboxMessages.Add(new EmailOutboxMessage { OrganizationId = org, EnrollmentId = camper.Id, Payload = "{}", Status = EmailOutboxStatuses.Pending });
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        PlayerErasurePreviewDto preview = (await Service(db).PreviewAsync(org, admin, PrivacyErasureKind.CampEnrollment, camper.Id)).Value!;

        preview.BlockingReasons.Should().Equal(PlayerErasureBlockReasons.PendingMail);
    }

    // ── Mails zonder inschrijving (outbox-mailservice), herkend aan het adres ──

    [Test]
    public async Task Preview_UnlinkedPendingPlatformMailToContactAddressWithoutOrganization_Blocks()
    {
        // Bv. een inloglink naar de ouder: platformmail, hoort bij geen club en heeft dus geen
        // organisatie, maar bevat wel het adres van deze speler.
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, Today.AddYears(-3));
        player.ContactEmail = "Parent@Example.test";
        player.StudentEmail = "student@example.test";
        db.Enrollments.Add(player);
        db.EmailOutboxMessages.Add(UnlinkedMail(null, "parent@example.test", EmailOutboxStatuses.Pending, PlatformMailType));
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        PlayerErasurePreviewDto preview = (await Service(db).PreviewAsync(org, admin, Lesson, player.Id)).Value!;

        preview.BlockingReasons.Should().Equal(PlayerErasureBlockReasons.PendingMail);
    }

    [Test]
    public async Task Preview_UnlinkedClubMailWithoutOrganization_DoesNotBlock()
    {
        // Een clubmail ("Bevestig je lesmoment") krijgt altijd een organisatie, ook uit publieke
        // flows. Zonder organisatie is niet te zeggen van welke club ze is: misschien een andere
        // club met dezelfde ouder. Wissen in deze club blijft er dus af.
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, Today.AddYears(-3));
        player.ContactEmail = "parent@example.test";
        db.Enrollments.Add(player);
        db.EmailOutboxMessages.Add(UnlinkedMail(null, "parent@example.test", EmailOutboxStatuses.Pending));
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        PlayerErasurePreviewDto preview = (await Service(db).PreviewAsync(org, admin, Lesson, player.Id)).Value!;

        preview.CanErase.Should().BeTrue();
    }

    [Test]
    public async Task Preview_UnlinkedMailBeingSent_Blocks()
    {
        // De worker is deze mail net aan het versturen: even wachten, niet wissen.
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, Today.AddYears(-3));
        db.Enrollments.Add(player);
        db.EmailOutboxMessages.Add(UnlinkedMail(org, "solo@example.test", EmailOutboxStatuses.Processing));
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        PlayerErasurePreviewDto preview = (await Service(db).PreviewAsync(org, admin, Lesson, player.Id)).Value!;

        preview.BlockingReasons.Should().Equal(PlayerErasureBlockReasons.PendingMail);
    }

    [Test]
    public async Task Erase_FailedMailToCamper_DoesNotBlockAndIsDeletedWithTheCamper()
    {
        // Een definitief mislukte mail wordt nooit meer verstuurd maar houdt naam en adres bij.
        // Blokkeren zou het wissen voorgoed tegenhouden; daarom gaat hij mee weg.
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment blueprint = Solo(org, Today.AddYears(-3));
        Camp camp = new() { Organization = blueprint.Organization, TennisClub = blueprint.LessonSerie!.TennisClub, Name = "Old camp", StartDate = Today.AddYears(-3).AddDays(-5), EndDate = Today.AddYears(-3) };
        CampEnrollment camper = new() { OrganizationId = org, Camp = camp, ParticipantName = "Camper", ParticipantEmail = "camper@example.test", Status = EnrollmentStatus.Confirmed };
        db.CampEnrollments.Add(camper);
        await db.SaveChangesAsync();
        EmailOutboxMessage unlinked = UnlinkedMail(org, "camper@example.test", EmailOutboxStatuses.Failed);
        EmailOutboxMessage linked = new() { OrganizationId = org, EnrollmentId = camper.Id, Type = "enrollment-confirmation", Payload = "{}", Status = EmailOutboxStatuses.Failed };
        db.EmailOutboxMessages.AddRange(unlinked, linked);
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);
        PlayerErasureService service = Service(db);

        PlayerErasurePreviewDto preview = (await service.PreviewAsync(org, admin, PrivacyErasureKind.CampEnrollment, camper.Id)).Value!;
        preview.CanErase.Should().BeTrue("a failed mail is never retried and must not block erasure forever");

        Result<PlayerErasureResultDto> erased = await service.EraseAsync(org, admin, PrivacyErasureKind.CampEnrollment, camper.Id,
            new ErasePlayerRequest(camper.ParticipantName, null, preview.UpdatedAt));

        erased.IsSuccess.Should().BeTrue();
        (await db.EmailOutboxMessages.IgnoreQueryFilters().AnyAsync(x => x.Id == unlinked.Id || x.Id == linked.Id))
            .Should().BeFalse("the failed mails held the camper's name and address");
    }

    [Test]
    public async Task Preview_UnlinkedPendingMailToOtherAddressOrOtherClub_DoesNotBlock()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, Today.AddYears(-3));
        db.Enrollments.Add(player);
        db.EmailOutboxMessages.AddRange(
            UnlinkedMail(org, "someone-else@example.test", EmailOutboxStatuses.Pending),
            // Zelfde adres, maar ingepland binnen een andere club: hoort niet bij deze speler.
            UnlinkedMail(Guid.NewGuid(), "solo@example.test", EmailOutboxStatuses.Pending));
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        PlayerErasurePreviewDto preview = (await Service(db).PreviewAsync(org, admin, Lesson, player.Id)).Value!;

        preview.CanErase.Should().BeTrue();
    }

    [Test]
    public async Task Erase_DeletesDeliveredUnlinkedMailToPlayerAddresses_AndKeepsOthers()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, Today.AddYears(-3));
        player.ContactEmail = "parent@example.test";
        player.StudentEmail = "student@example.test";
        db.Enrollments.Add(player);
        EmailOutboxMessage toParent = UnlinkedMail(org, "parent@example.test", EmailOutboxStatuses.Completed);
        EmailOutboxMessage toStudent = UnlinkedMail(org, "student@example.test", EmailOutboxStatuses.Completed);
        EmailOutboxMessage platformToParent = UnlinkedMail(null, "parent@example.test", EmailOutboxStatuses.Failed, PlatformMailType);
        EmailOutboxMessage toOther = UnlinkedMail(org, "other@example.test", EmailOutboxStatuses.Completed);
        // Clubmail zonder organisatie naar hetzelfde adres: kan van een andere club zijn, blijft.
        EmailOutboxMessage clubMailWithoutOrganization = UnlinkedMail(null, "parent@example.test", EmailOutboxStatuses.Failed);
        db.EmailOutboxMessages.AddRange(toParent, toStudent, platformToParent, toOther, clubMailWithoutOrganization);
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);
        PlayerErasureService service = Service(db);
        PlayerErasurePreviewDto preview = (await service.PreviewAsync(org, admin, Lesson, player.Id)).Value!;
        preview.CanErase.Should().BeTrue("delivered mail does not block erasure");

        Result<PlayerErasureResultDto> erased = await service.EraseAsync(org, admin, Lesson, player.Id,
            new ErasePlayerRequest(player.StudentName, null, preview.UpdatedAt));

        erased.IsSuccess.Should().BeTrue();
        List<Guid> remaining = await db.EmailOutboxMessages.IgnoreQueryFilters().Select(x => x.Id).ToListAsync();
        remaining.Should().Contain([toOther.Id, clubMailWithoutOrganization.Id]);
        remaining.Should().NotContain([toParent.Id, toStudent.Id, platformToParent.Id]);
    }

    /// <summary>Platformmail (geen clubmail): staat terecht zonder organisatie in de outbox.</summary>
    private const string PlatformMailType = "mail:student-magic-link";

    private static EmailOutboxMessage UnlinkedMail(
        Guid? organizationId, string recipient, string status, string type = "mail:schedule-confirmation") => new()
    {
        OrganizationId = organizationId,
        EnrollmentId = null,
        RecipientEmail = recipient,
        Type = type,
        Payload = "{}",
        Status = status,
    };
}
