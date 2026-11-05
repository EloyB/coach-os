using System.Text.Json;
using CoachOS.Application.Abstractions;
using CoachOS.Domain.Entities;
using CoachOS.Infrastructure.Persistence;
using CoachOS.Infrastructure.Privacy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using NUnit.Framework;

namespace CoachOS.Tests.Privacy;

[TestFixture, NonParallelizable]
public class PrivacyExecutionPostgresTests
{
    private string? _admin;
    private string? _database;
    private DbContextOptions<ApplicationDbContext> _options = null!;
    private sealed class NoTenant : ITenantContext
    {
        public Guid OrganizationId => Guid.Empty;
        public Guid UserId => Guid.Empty;
        public bool IsAuthenticated => false;
    }
    private ApplicationDbContext Db() => new(_options, new NoTenant());

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        _admin = Environment.GetEnvironmentVariable("COACHOS_TEST_PG");
        if (string.IsNullOrWhiteSpace(_admin)) Assert.Ignore("COACHOS_TEST_PG required for actual PostgreSQL execution tests");
        _database = "privacy_execution_" + Guid.NewGuid().ToString("N");
        await using NpgsqlConnection admin = new(_admin);
        await admin.OpenAsync();
        await using NpgsqlCommand command = new($"CREATE DATABASE \"{_database}\"", admin);
        await command.ExecuteNonQueryAsync();
        _options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(new NpgsqlConnectionStringBuilder(_admin) { Database = _database }.ConnectionString).Options;
        await using ApplicationDbContext db = Db();
        await db.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task DropDatabase()
    {
        if (_database is null) return;
        NpgsqlConnection.ClearAllPools();
        await using NpgsqlConnection admin = new(_admin);
        await admin.OpenAsync();
        await using NpgsqlCommand command = new($"DROP DATABASE IF EXISTS \"{_database}\" WITH (FORCE)", admin);
        await command.ExecuteNonQueryAsync();
    }

    [SetUp]
    public async Task ResetTechnicalFixtures()
    {
        await using ApplicationDbContext db = Db();
        await db.MagicLinkTokens.ExecuteDeleteAsync();
        await db.EmailOutboxMessages.ExecuteDeleteAsync();
    }

    [Test]
    public async Task PreviewIsOptInTenantScopedAndNeverChangesOldOrActiveData()
    {
        await using ApplicationDbContext db = Db();
        Guid org = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        Enrollment expired = Solo(org, today.AddYears(-3));
        Enrollment active = new() { Organization = expired.Organization, LessonSerie = new LessonSerie { Organization = expired.Organization, TennisClub = expired.LessonSerie!.TennisClub, Name = "Active", StartDate = today, EndDate = today.AddDays(1) }, StudentName = "Active player", DateOfBirth = new DateOnly(2002,1,1), ContactEmail = "active@example.test" };
        Enrollment foreign = Solo(other, today.AddYears(-3));
        expired.ContactEmail = "old@example.test";
        db.Enrollments.AddRange(expired, active, foreign);
        await db.SaveChangesAsync();
        PlayerRetentionOptions options = new();
        PlayerRetentionPreview service = new(db, Options.Create(options), NullLogger<PlayerRetentionPreview>.Instance);
        NUnit.Framework.Legacy.ClassicAssert.AreEqual(0, await service.RunAsync(org));
        options.Enabled = true;
        options.OrganizationIds = [org];
        NUnit.Framework.Legacy.ClassicAssert.AreEqual(1, await service.RunAsync(org));
        NUnit.Framework.Legacy.ClassicAssert.AreEqual(0, await service.RunAsync(other));
        NUnit.Framework.Legacy.ClassicAssert.AreEqual(3, await db.Enrollments.CountAsync(x => x.OrganizationId == org || x.OrganizationId == other));
        NUnit.Framework.Legacy.ClassicAssert.AreEqual("old@example.test", (await db.Enrollments.SingleAsync(e => e.Id == expired.Id)).ContactEmail);
        NUnit.Framework.Legacy.ClassicAssert.IsFalse(db.ChangeTracker.HasChanges());
        db.Enrollments.Add(new Enrollment { OrganizationId = org, LessonSerieId = active.LessonSerieId,
            StudentName = expired.StudentName, DateOfBirth = expired.DateOfBirth, ContactEmail = " OLD@example.test " });
        await db.SaveChangesAsync();
        NUnit.Framework.Legacy.ClassicAssert.AreEqual(0, await service.RunAsync(org), "The same player with recent participation is not a deletion candidate");
        NUnit.Framework.Legacy.ClassicAssert.AreEqual(4, await db.Enrollments.CountAsync(x => x.OrganizationId == org || x.OrganizationId == other));
    }

    [TestCase(false, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public async Task TechnicalCleanupRequiresExplicitEnabledApplyAndNonDryRun(bool enabled, bool apply, bool dryRun)
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        db.MagicLinkTokens.Add(new MagicLinkToken { Email = "retained@example.test", TokenHash = new string('d',64), ExpiresAt = DateTime.UtcNow.AddDays(-1) });
        await db.SaveChangesAsync();
        PlayerRetentionOptions options = new() { Enabled = enabled, Apply = apply, DryRun = dryRun, OrganizationIds = [org] };
        await new PlayerRetentionPreview(db, Options.Create(options), NullLogger<PlayerRetentionPreview>.Instance).RunAsync(org);
        (await db.MagicLinkTokens.CountAsync()).Should().Be(1);
    }

    private static Enrollment Solo(Guid org, DateOnly end, bool hold = false)
    {
        Organization organization = new() { Id = org, Name = "Synthetic club", Email = $"club-{org:N}@example.test" };
        TennisClub club = new() { Organization = organization, Name = "Synthetic court" };
        LessonSerie series = new() { Organization = organization, TennisClub = club, Name = "Synthetic free series", StartDate = end.AddDays(-10), EndDate = end };
        return new Enrollment { Organization = organization, LessonSerie = series, StudentName = "Synthetic Private", ContactEmail = "solo@example.test", DateOfBirth = new DateOnly(2000,1,1), PrivacyHold = hold, Status = CoachOS.Domain.Enums.EnrollmentStatus.Confirmed };
    }

    [Test]
    public async Task EarlyDeletionNeedsVerifiedExactCurrentPreviewAndKeepsOtherOrganizationUnobservable()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-6));
        db.Enrollments.Add(player); await db.SaveChangesAsync();
        PlayerErasureService service = new(db);
        (await service.ProcessAsync(org, player.Id, false, new())).Holds.Should().Contain("retention_not_due");
        CoachOS.Application.Privacy.PlayerErasureResult preview = await service.ProcessAsync(org, player.Id, false, new(Early:true));
        preview.Eligible.Should().BeTrue();
        (await db.Enrollments.AnyAsync(x => x.Id == player.Id)).Should().BeTrue();
        (await service.ProcessAsync(Guid.NewGuid(), player.Id, false, new(Early:true))).Holds.Should().Equal("not_found");
        (await service.ProcessAsync(org, player.Id, false, new(true,true,preview.PreviewHash,player.StudentName,false))).Deleted.Should().BeFalse();
        (await service.ProcessAsync(org, player.Id, false, new(true,true,preview.PreviewHash,"Wrong name",true))).Deleted.Should().BeFalse();
        player.Notes = "Changed after preview"; await db.SaveChangesAsync();
        (await service.ProcessAsync(org, player.Id, false, new(true,true,preview.PreviewHash,player.StudentName,true))).Deleted.Should().BeFalse();
        preview = await service.ProcessAsync(org, player.Id, false, new(Early:true));
        (await service.ProcessAsync(org, player.Id, false, new(true,true,preview.PreviewHash,player.StudentName,true))).Deleted.Should().BeTrue();
        (await db.Enrollments.AnyAsync(x => x.Id == player.Id)).Should().BeFalse();
        (await db.Database.SqlQuery<int>($"""SELECT count(*)::int AS "Value" FROM "PrivacyErasureReceipts" WHERE "OrganizationId"={org} AND "TargetId"={player.Id} AND "TargetKind"='enrollment'""").SingleAsync()).Should().Be(1,"completed erasure must produce a minimal transactional replay receipt");
        (await service.ProcessAsync(org, player.Id, false, new(Early:true))).Holds.Should().Equal("not_found");
    }

    [Test]
    public async Task FutureParticipationAndUnfinishedMailBlockEvenVerifiedEarlyDeletion()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));
        db.Enrollments.Add(player); await db.SaveChangesAsync();
        db.EmailOutboxMessages.Add(new EmailOutboxMessage { OrganizationId=org, EnrollmentId=player.Id, Payload="{}", Status=EmailOutboxStatuses.Failed });
        await db.SaveChangesAsync();
        CoachOS.Application.Privacy.PlayerErasureResult result = await new PlayerErasureService(db).ProcessAsync(org, player.Id, false, new(Early:true));
        result.Holds.Should().Contain("active_or_future_participation").And.Contain("delivery_pending_or_failed");
        result.Eligible.Should().BeFalse();
    }

    [Test]
    public async Task ConcurrentHoldWriterIsRecheckedAfterRealPostgresLockBeforeDeletion()
    {
        Guid org = Guid.NewGuid(); Guid id; string name;
        await using (ApplicationDbContext seed = Db())
        {
            Enrollment player = Solo(org, DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-3));
            seed.Enrollments.Add(player); await seed.SaveChangesAsync(); id=player.Id; name=player.StudentName;
        }
        CoachOS.Application.Privacy.PlayerErasureResult preview;
        await using (ApplicationDbContext read = Db()) preview = await new PlayerErasureService(read).ProcessAsync(org,id,false,new());
        await using ApplicationDbContext writer = Db();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx = await writer.Database.BeginTransactionAsync();
        await writer.Enrollments.Where(x=>x.OrganizationId==org && x.Id==id).ExecuteUpdateAsync(set=>set.SetProperty(x=>x.PrivacyHold,true));
        await using ApplicationDbContext eraser = Db();
        Task<CoachOS.Application.Privacy.PlayerErasureResult> erase = new PlayerErasureService(eraser).ProcessAsync(org,id,false,new(true,false,preview.PreviewHash,name,true));
        await Task.Delay(100);
        erase.IsCompleted.Should().BeFalse("the actual table-write lock must wait for an already-running hold mutation");
        await tx.CommitAsync();
        CoachOS.Application.Privacy.PlayerErasureResult result = await erase;
        result.Deleted.Should().BeFalse(); result.Holds.Should().Contain("privacy_hold_review_required");
        (await eraser.Enrollments.AnyAsync(x=>x.OrganizationId==org && x.Id==id)).Should().BeTrue();
    }

    [Test]
    public async Task ForeignFormDependencyMakesActualFkFailureRollbackAllEarlierPersonalAnswerDeletes()
    {
        Guid org=Guid.NewGuid(); Guid other=Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player=Solo(org, DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-3));
        Enrollment foreign=Solo(other, DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-3));
        EnrollmentForm ownForm=new(){OrganizationId=org,LessonSerie=player.LessonSerie!};
        EnrollmentForm foreignForm=new(){OrganizationId=other,LessonSerie=foreign.LessonSerie!};
        FormField ownField=new(){EnrollmentForm=ownForm,Label="Own",Type=CoachOS.Domain.Enums.FormFieldType.Text};
        FormField foreignField=new(){EnrollmentForm=foreignForm,Label="Foreign",Type=CoachOS.Domain.Enums.FormFieldType.Text};
        db.Enrollments.AddRange(player,foreign);
        db.FormResponses.AddRange(new FormResponse{Enrollment=player,FormField=ownField,Value="Own private answer"},new FormResponse{Enrollment=player,FormField=foreignField,Value="Corrupt foreign dependency"});
        await db.SaveChangesAsync();
        PlayerErasureService service=new(db);
        CoachOS.Application.Privacy.PlayerErasureResult preview=await service.ProcessAsync(org,player.Id,false,new());
        CoachOS.Application.Privacy.PlayerErasureResult result=await service.ProcessAsync(org,player.Id,false,new(true,false,preview.PreviewHash,player.StudentName,true));
        result.Deleted.Should().BeFalse(); result.Holds.Should().Contain("concurrent_change_or_dependency_review_required");
        (await db.Database.SqlQuery<int>($"""SELECT count(*)::int AS "Value" FROM "PrivacyErasureReceipts" WHERE "OrganizationId"={org} AND "TargetId"={player.Id}""").SingleAsync()).Should().Be(0);
        (await db.Enrollments.AnyAsync(x=>x.OrganizationId==org && x.Id==player.Id)).Should().BeTrue();
        (await db.FormResponses.CountAsync(x=>x.EnrollmentId==player.Id)).Should().Be(2,"the first DELETE must have rolled back with the failed FK");
        (await db.Enrollments.AnyAsync(x=>x.OrganizationId==other && x.Id==foreign.Id)).Should().BeTrue();
    }

    [Test]
    public async Task StandaloneReschedulingChainExtendsParticipationAndCannotBeErasedBeforeFutureReplacement()
    {
        Guid org=Guid.NewGuid(); DateOnly today=DateOnly.FromDateTime(DateTime.UtcNow);
        await using ApplicationDbContext db=Db();
        Enrollment player=Solo(org,today.AddYears(-3));
        TennisClub club=player.LessonSerie!.TennisClub;
        Lesson replacement=new(){Organization=player.Organization,TennisClub=club,Date=today.AddDays(10),StartTime=new TimeOnly(14,0),EndTime=new TimeOnly(15,0),MaxStudents=4};
        Lesson original=new(){Organization=player.Organization,TennisClub=club,Date=today.AddYears(-3),StartTime=new TimeOnly(14,0),EndTime=new TimeOnly(15,0),MaxStudents=4,IsCancelled=true,RescheduledToLesson=replacement};
        player.LessonSerie=null; player.Lesson=original;
        db.Enrollments.Add(player); await db.SaveChangesAsync();
        CoachOS.Application.Privacy.PlayerErasureResult result=await new PlayerErasureService(db).ProcessAsync(org,player.Id,false,new(Early:true));
        result.Eligible.Should().BeFalse(); result.Holds.Should().Contain("active_or_future_participation");
    }

    [TestCase(false,false)]
    [TestCase(true,false)]
    [TestCase(true,true)]
    public async Task GroupMemberDeletionPreservesFamilyAndLeaderTransferNeedsExplicitPreview(bool targetIsLeader,bool transfer)
    {
        Guid org=Guid.NewGuid();
        await using ApplicationDbContext db=Db();
        Enrollment target=Solo(org,DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-3));
        Enrollment family=new(){Organization=target.Organization,LessonSerie=target.LessonSerie,StudentName="Other family member",ContactEmail=target.ContactEmail,DateOfBirth=new DateOnly(2002,1,1),Status=CoachOS.Domain.Enums.EnrollmentStatus.Confirmed};
        db.Enrollments.AddRange(target,family); await db.SaveChangesAsync();
        EnrollmentGroup group=new(){OrganizationId=org,LessonSerieId=target.LessonSerieId!.Value,LeaderEnrollmentId=targetIsLeader?target.Id:family.Id,Name="Original group label"};
        db.EnrollmentGroups.Add(group);await db.SaveChangesAsync();
        target.EnrollmentGroupId=group.Id;family.EnrollmentGroupId=group.Id;await db.SaveChangesAsync();
        PlayerErasureService service=new(db);
        Guid? replacement=transfer?family.Id:null;
        CoachOS.Application.Privacy.PlayerErasureResult preview=await service.ProcessAsync(org,target.Id,false,new(NewGroupLeaderId:replacement));
        if(targetIsLeader&&!transfer)
        {
            preview.Eligible.Should().BeFalse();preview.Holds.Should().Contain("group_leader_transfer_required");return;
        }
        preview.Eligible.Should().BeTrue();
        (await service.ProcessAsync(org,target.Id,false,new(true,false,preview.PreviewHash,target.StudentName,true,replacement))).Deleted.Should().BeTrue();
        (await db.Enrollments.AnyAsync(x=>x.OrganizationId==org&&x.Id==family.Id)).Should().BeTrue();
        (await db.EnrollmentGroups.AsNoTracking().SingleAsync(x=>x.OrganizationId==org&&x.Id==group.Id)).LeaderEnrollmentId.Should().Be(family.Id);
        (await db.Enrollments.AnyAsync(x=>x.OrganizationId==org&&x.Id==target.Id)).Should().BeFalse();
    }

    [Test]
    public async Task LastFreeGroupMemberDeletionResolvesCircularLeaderForeignKeysAtomically()
    {
        Guid org=Guid.NewGuid();await using ApplicationDbContext db=Db();
        Enrollment target=Solo(org,DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-3));db.Enrollments.Add(target);await db.SaveChangesAsync();
        EnrollmentGroup group=new(){OrganizationId=org,LessonSerieId=target.LessonSerieId!.Value,LeaderEnrollmentId=target.Id,Name="Only member"};db.EnrollmentGroups.Add(group);await db.SaveChangesAsync();
        target.EnrollmentGroupId=group.Id;await db.SaveChangesAsync();
        PlayerErasureService service=new(db);CoachOS.Application.Privacy.PlayerErasureResult preview=await service.ProcessAsync(org,target.Id,false,new());
        preview.Eligible.Should().BeTrue();
        (await service.ProcessAsync(org,target.Id,false,new(true,false,preview.PreviewHash,target.StudentName,true))).Deleted.Should().BeTrue();
        (await db.EnrollmentGroups.AnyAsync(x=>x.OrganizationId==org&&x.Id==group.Id)).Should().BeFalse();
        (await db.Enrollments.AnyAsync(x=>x.OrganizationId==org&&x.Id==target.Id)).Should().BeFalse();
    }

    [Test]
    public async Task FutureCampUsesStudentAddressWhenParentContactAddressDiffers()
    {
        Guid org=Guid.NewGuid();DateOnly today=DateOnly.FromDateTime(DateTime.UtcNow);
        await using ApplicationDbContext db=Db();Enrollment old=Solo(org,today.AddYears(-3));
        old.ContactEmail="parent@example.test";old.StudentEmail="student@example.test";
        Camp camp=new(){Organization=old.Organization,TennisClub=old.LessonSerie!.TennisClub,Name="Future camp",StartDate=today.AddDays(10),EndDate=today.AddDays(15)};
        db.Enrollments.Add(old);db.CampEnrollments.Add(new CampEnrollment{OrganizationId=org,Camp=camp,ParticipantName=old.StudentName,ParticipantEmail=old.StudentEmail,Status=CoachOS.Domain.Enums.EnrollmentStatus.Confirmed});await db.SaveChangesAsync();
        CoachOS.Application.Privacy.PlayerErasureResult result=await new PlayerErasureService(db).ProcessAsync(org,old.Id,false,new());
        result.Eligible.Should().BeFalse();result.Holds.Should().Contain("active_or_future_participation");
    }

    [Test]
    public async Task PaidOfferingWithoutPaymentRowStillRequiresFinancialReview()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-3));
        player.LessonSerie!.Price = 25;
        db.Enrollments.Add(player); await db.SaveChangesAsync();
        CoachOS.Application.Privacy.PlayerErasureResult result = await new PlayerErasureService(db).ProcessAsync(org, player.Id, false, new());
        result.Eligible.Should().BeFalse("absence of a payment row is not proof that a priced registration has no financial obligations");
        result.Holds.Should().Contain("financial_review_required");
    }

    [Test]
    public async Task VerifiedEarlyRequestCannotOverrideExplicitPrivacyHold()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment player = Solo(org, DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-3), true);
        db.Enrollments.Add(player); await db.SaveChangesAsync();
        CoachOS.Application.Privacy.PlayerErasureResult result = await new PlayerErasureService(db).ProcessAsync(org, player.Id, false, new(Early: true));
        result.Eligible.Should().BeFalse("a verified early request is not legal-hold bypass authority");
        result.Holds.Should().Contain("privacy_hold_review_required");
        (await db.Enrollments.AnyAsync(x => x.Id == player.Id)).Should().BeTrue();
    }

    [Test]
    public async Task ExplicitApplyReallyDeletesOldNonFinancialPlayerAndAnswersWithoutMergingFamilyOrOtherClub()
    {
        Guid org = Guid.NewGuid(); Guid other = Guid.NewGuid();
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        await using ApplicationDbContext db = Db();
        Organization organization = new() { Id = org, Name = "Synthetic club", Email = $"club-{org:N}@example.test" };
        Organization foreign = new() { Id = other, Name = "Other synthetic club", Email = "other@example.test" };
        TennisClub club = new() { Organization = organization, Name = "Synthetic court" };
        TennisClub foreignClub = new() { Organization = foreign, Name = "Other synthetic court" };
        LessonSerie old = new() { Organization = organization, TennisClub = club, Name = "Old free series", StartDate = today.AddYears(-4), EndDate = today.AddYears(-3) };
        LessonSerie active = new() { Organization = organization, TennisClub = club, Name = "Active free series", StartDate = today, EndDate = today.AddDays(30) };
        LessonSerie foreignSeries = new() { Organization = foreign, TennisClub = foreignClub, Name = "Other club series", StartDate = today, EndDate = today.AddDays(30) };
        Enrollment expired = new() { Organization = organization, LessonSerie = old, StudentName = "Synthetic One", DateOfBirth = new DateOnly(2000,1,1), ContactEmail = "family@example.test", Notes = "Sensitive free text", Status = CoachOS.Domain.Enums.EnrollmentStatus.Confirmed };
        Enrollment family = new() { Organization = organization, LessonSerie = active, StudentName = "Synthetic Two", DateOfBirth = new DateOnly(2002,1,1), ContactEmail = "family@example.test", Status = CoachOS.Domain.Enums.EnrollmentStatus.Confirmed };
        Enrollment foreignPlayer = new() { Organization = foreign, LessonSerie = foreignSeries, StudentName = expired.StudentName, DateOfBirth = expired.DateOfBirth, ContactEmail = expired.ContactEmail, Status = CoachOS.Domain.Enums.EnrollmentStatus.Confirmed };
        EnrollmentForm form = new() { LessonSerie = old, OrganizationId = org };
        FormField field = new() { EnrollmentForm = form, Label = "Synthetic question", Type = CoachOS.Domain.Enums.FormFieldType.Text };
        db.Enrollments.AddRange(expired, family, foreignPlayer);
        db.FormResponses.Add(new FormResponse { Enrollment = expired, FormField = field, Value = "Synthetic private answer" });
        await db.SaveChangesAsync();
        PlayerRetentionOptions options = new() { Enabled = true, Apply = false, DryRun = true, OrganizationIds = [org] };
        PlayerRetentionPreview processor=new(db,Options.Create(options),NullLogger<PlayerRetentionPreview>.Instance);
        (await processor.RunAsync(org)).Should().Be(1,"dry-run must evaluate the same candidates as actual execution, without merging family email");
        (await db.Enrollments.AnyAsync(x=>x.OrganizationId==org&&x.Id==expired.Id)).Should().BeTrue();
        options.Apply=true;options.DryRun=false;
        await processor.RunAsync(org);
        (await db.Enrollments.AnyAsync(x => x.Id == expired.Id)).Should().BeFalse("approved non-financial retention must actually execute, not merely log a preview");
        (await db.FormResponses.CountAsync()).Should().Be(0);
        (await db.Enrollments.AnyAsync(x => x.Id == family.Id)).Should().BeTrue();
        (await db.Enrollments.AnyAsync(x => x.Id == foreignPlayer.Id)).Should().BeTrue();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CampGroupErasureDeletesPersonalAnswersAndPreservesRemainingFamilyOrRemovesEmptyGroup(bool singleton)
    {
        Guid org=Guid.NewGuid();DateOnly today=DateOnly.FromDateTime(DateTime.UtcNow);
        await using ApplicationDbContext db=Db();
        Enrollment blueprint=Solo(org,today.AddYears(-3));
        Camp camp=new(){Organization=blueprint.Organization,TennisClub=blueprint.LessonSerie!.TennisClub,Name="Free old camp",StartDate=today.AddYears(-3).AddDays(-5),EndDate=today.AddYears(-3)};
        CampEnrollment target=new(){OrganizationId=org,Camp=camp,ParticipantName="Synthetic camper",ParticipantEmail="family@example.test",Status=CoachOS.Domain.Enums.EnrollmentStatus.Confirmed};
        CampEnrollment family=new(){OrganizationId=org,Camp=camp,ParticipantName="Other camper",ParticipantEmail="other-camper@example.test",Status=CoachOS.Domain.Enums.EnrollmentStatus.Confirmed};
        db.CampEnrollments.Add(target);if(!singleton)db.CampEnrollments.Add(family);await db.SaveChangesAsync();
        CampEnrollmentGroup group=new(){OrganizationId=org,CampId=camp.Id,LeaderEnrollmentId=target.Id,Name="Original label"};db.CampEnrollmentGroups.Add(group);await db.SaveChangesAsync();
        target.CampEnrollmentGroupId=group.Id;if(!singleton)family.CampEnrollmentGroupId=group.Id;
        CampEnrollmentForm form=new(){OrganizationId=org,Camp=camp};CampFormField field=new(){CampEnrollmentForm=form,Label="Private",Type=CoachOS.Domain.Enums.FormFieldType.Text};
        db.CampFormResponses.Add(new CampFormResponse{CampEnrollment=target,CampFormField=field,Value="Synthetic sensitive response"});await db.SaveChangesAsync();
        Guid? replacement=singleton?null:family.Id;
        PlayerErasureService service=new(db);CoachOS.Application.Privacy.PlayerErasureResult preview=await service.ProcessAsync(org,target.Id,true,new(NewGroupLeaderId:replacement));
        preview.Eligible.Should().BeTrue();
        (await service.ProcessAsync(org,target.Id,true,new(true,false,preview.PreviewHash,target.ParticipantName,true,replacement))).Deleted.Should().BeTrue();
        (await db.CampFormResponses.CountAsync(x=>x.CampEnrollment.OrganizationId==org)).Should().Be(0);
        (await db.CampEnrollments.CountAsync(x=>x.OrganizationId==org)).Should().Be(singleton?0:1);
        (await db.CampEnrollmentGroups.AnyAsync(x=>x.OrganizationId==org&&x.Id==group.Id)).Should().Be(!singleton);
    }

    [Test]
    public async Task ExpiredAccountInvitationAndVerificationCapabilitiesAreClearedWithoutAccountsOrRateLimits()
    {
        Guid org=Guid.NewGuid();DateTime now=DateTime.UtcNow;
        await using ApplicationDbContext db=Db();
        CoachOS.Infrastructure.Identity.ApplicationUser expired=new(){Id=Guid.NewGuid(),UserName="expired-"+Guid.NewGuid(),Email="expired-"+Guid.NewGuid()+"@example.test",InviteToken="synthetic-expired-invite",InviteTokenExpiry=now.AddDays(-1),EmailVerificationCodeHash="synthetic-expired-hash",EmailVerificationCodeExpiry=now.AddDays(-1),EmailVerificationAttempts=3,EmailVerificationSendCount=4,CreatedAt=now,UpdatedAt=now};
        CoachOS.Infrastructure.Identity.ApplicationUser valid=new(){Id=Guid.NewGuid(),UserName="valid-"+Guid.NewGuid(),Email="valid-"+Guid.NewGuid()+"@example.test",InviteToken="synthetic-valid-invite",InviteTokenExpiry=now.AddDays(1),EmailVerificationCodeHash="synthetic-valid-hash",EmailVerificationCodeExpiry=now.AddDays(1),CreatedAt=now,UpdatedAt=now};
        db.Users.AddRange(expired,valid);await db.SaveChangesAsync();
        await new PlayerRetentionPreview(db,Options.Create(new PlayerRetentionOptions{Enabled=true,Apply=true,DryRun=false,OrganizationIds=[org]}),NullLogger<PlayerRetentionPreview>.Instance).RunAsync(org);
        await db.Entry(expired).ReloadAsync();await db.Entry(valid).ReloadAsync();
        expired.InviteToken.Should().BeNull();expired.EmailVerificationCodeHash.Should().BeNull();
        expired.EmailVerificationAttempts.Should().Be(3);expired.EmailVerificationSendCount.Should().Be(4);expired.IsActive.Should().BeTrue();
        valid.InviteToken.Should().Be("synthetic-valid-invite");valid.EmailVerificationCodeHash.Should().Be("synthetic-valid-hash");valid.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task ExplicitApplyCleansOnlyExpiredOrUsedSecurityLinksAndCompletedTenantMailAfterThirtyDays()
    {
        Guid org = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        await using ApplicationDbContext db = Db();
        db.MagicLinkTokens.AddRange(
            new MagicLinkToken { Email = "used@example.test", TokenHash = new string('a',64), ExpiresAt = now.AddDays(1), UsedAt = now.AddSeconds(-1) },
            new MagicLinkToken { Email = "expired@example.test", TokenHash = new string('b',64), ExpiresAt = now.AddSeconds(-1) },
            new MagicLinkToken { Email = "valid@example.test", TokenHash = new string('c',64), ExpiresAt = now.AddDays(1) });
        db.EmailOutboxMessages.AddRange(
            new EmailOutboxMessage { OrganizationId = org, Payload = "{}", Status = EmailOutboxStatuses.Completed, ProcessedAt = now.AddDays(-31) },
            new EmailOutboxMessage { OrganizationId = org, Payload = "{}", Status = EmailOutboxStatuses.Pending, ProcessedAt = now.AddDays(-31) },
            new EmailOutboxMessage { OrganizationId = org, Payload = "{}", Status = EmailOutboxStatuses.Failed, ProcessedAt = now.AddDays(-31) },
            new EmailOutboxMessage { OrganizationId = org, Payload = "{}", Status = EmailOutboxStatuses.Completed, ProcessedAt = now.AddDays(-29) },
            new EmailOutboxMessage { OrganizationId = other, Payload = "{}", Status = EmailOutboxStatuses.Completed, ProcessedAt = now.AddDays(-31) });
        await db.SaveChangesAsync();
        PlayerRetentionOptions options = JsonSerializer.Deserialize<PlayerRetentionOptions>(JsonSerializer.Serialize(new { Enabled = true, OrganizationIds = new[] { org }, Apply = true, DryRun = false }))!;
        await new PlayerRetentionPreview(db, Options.Create(options), NullLogger<PlayerRetentionPreview>.Instance).RunAsync(org);
        (await db.MagicLinkTokens.Select(x => x.Email).ToListAsync()).Should().Equal("valid@example.test");
        (await db.EmailOutboxMessages.CountAsync()).Should().Be(4);
        (await db.EmailOutboxMessages.CountAsync(x => x.OrganizationId == other)).Should().Be(1);
        (await db.EmailOutboxMessages.CountAsync(x => x.Status == EmailOutboxStatuses.Pending || x.Status == EmailOutboxStatuses.Failed)).Should().Be(2);
    }
}
