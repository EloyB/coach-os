using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;
using CoachOS.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CoachOS.Tests.Privacy;

[TestFixture, NonParallelizable]
public class PlayerErasureGroupPostgresTests : PlayerErasurePostgresBase
{
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task GroupMember_ErasedWithoutFamily_LeaderNeedsExplicitNewLeader(bool targetIsLeader, bool chooseLeader)
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment target = Solo(org, Today.AddYears(-3));
        Enrollment family = new() { Organization = target.Organization, LessonSerie = target.LessonSerie, StudentName = "Other family member", ContactEmail = target.ContactEmail, DateOfBirth = new DateOnly(2002, 1, 1), Status = EnrollmentStatus.Confirmed };
        db.Enrollments.AddRange(target, family);
        await db.SaveChangesAsync();
        EnrollmentGroup group = new() { OrganizationId = org, LessonSerieId = target.LessonSerieId!.Value, LeaderEnrollmentId = targetIsLeader ? target.Id : family.Id, Name = "Original group label" };
        db.EnrollmentGroups.Add(group);
        await db.SaveChangesAsync();
        target.EnrollmentGroupId = group.Id;
        family.EnrollmentGroupId = group.Id;
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        Result<PlayerErasureResultDto> result = await Service(db).EraseAsync(org, admin, PrivacyErasureKind.Enrollment, target.Id,
            new ErasePlayerRequest(target.StudentName, chooseLeader ? family.Id : null));

        if (targetIsLeader && !chooseLeader)
        {
            result.Errors.Single().Code.Should().Be(ErrorCodes.Validation);
            (await db.Enrollments.AnyAsync(x => x.Id == target.Id)).Should().BeTrue();
            return;
        }
        result.IsSuccess.Should().BeTrue();
        (await db.Enrollments.AnyAsync(x => x.Id == family.Id)).Should().BeTrue();
        (await db.EnrollmentGroups.AsNoTracking().SingleAsync(x => x.Id == group.Id)).LeaderEnrollmentId.Should().Be(family.Id);
        (await db.Enrollments.AnyAsync(x => x.Id == target.Id)).Should().BeFalse();
    }

    [Test]
    public async Task LastGroupMember_ResolvesCircularLeaderForeignKeyAndRemovesGroup()
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment target = Solo(org, Today.AddYears(-3));
        db.Enrollments.Add(target);
        await db.SaveChangesAsync();
        EnrollmentGroup group = new() { OrganizationId = org, LessonSerieId = target.LessonSerieId!.Value, LeaderEnrollmentId = target.Id, Name = "Only member" };
        db.EnrollmentGroups.Add(group);
        await db.SaveChangesAsync();
        target.EnrollmentGroupId = group.Id;
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        (await Service(db).EraseAsync(org, admin, PrivacyErasureKind.Enrollment, target.Id, new ErasePlayerRequest(target.StudentName)))
            .IsSuccess.Should().BeTrue();

        (await db.EnrollmentGroups.AnyAsync(x => x.Id == group.Id)).Should().BeFalse();
        (await db.Enrollments.AnyAsync(x => x.Id == target.Id)).Should().BeFalse();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CampGroup_ErasesAnswersAndKeepsFamilyOrRemovesEmptyGroup(bool singleton)
    {
        Guid org = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment blueprint = Solo(org, Today.AddYears(-3));
        Camp camp = new() { Organization = blueprint.Organization, TennisClub = blueprint.LessonSerie!.TennisClub, Name = "Free old camp", StartDate = Today.AddYears(-3).AddDays(-5), EndDate = Today.AddYears(-3) };
        CampEnrollment target = new() { OrganizationId = org, Camp = camp, ParticipantName = "Synthetic camper", ParticipantEmail = "family@example.test", Status = EnrollmentStatus.Confirmed };
        CampEnrollment family = new() { OrganizationId = org, Camp = camp, ParticipantName = "Other camper", ParticipantEmail = "other-camper@example.test", Status = EnrollmentStatus.Confirmed };
        db.CampEnrollments.Add(target);
        if (!singleton) db.CampEnrollments.Add(family);
        await db.SaveChangesAsync();
        CampEnrollmentGroup group = new() { OrganizationId = org, CampId = camp.Id, LeaderEnrollmentId = target.Id, Name = "Original label" };
        db.CampEnrollmentGroups.Add(group);
        await db.SaveChangesAsync();
        target.CampEnrollmentGroupId = group.Id;
        if (!singleton) family.CampEnrollmentGroupId = group.Id;
        CampEnrollmentForm form = new() { OrganizationId = org, Camp = camp };
        CampFormField field = new() { CampEnrollmentForm = form, Label = "Private", Type = FormFieldType.Text };
        db.CampFormResponses.Add(new CampFormResponse { CampEnrollment = target, CampFormField = field, Value = "Synthetic sensitive response" });
        await db.SaveChangesAsync();
        Guid admin = await SeedAdminAsync(db, org);

        Result<PlayerErasureResultDto> result = await Service(db).EraseAsync(org, admin, PrivacyErasureKind.CampEnrollment, target.Id,
            new ErasePlayerRequest(target.ParticipantName, singleton ? null : family.Id));

        result.IsSuccess.Should().BeTrue();
        (await db.CampFormResponses.CountAsync(x => x.CampEnrollment.OrganizationId == org)).Should().Be(0);
        (await db.CampEnrollments.CountAsync(x => x.OrganizationId == org)).Should().Be(singleton ? 0 : 1);
        (await db.CampEnrollmentGroups.AnyAsync(x => x.Id == group.Id)).Should().Be(!singleton);
        (await ReceiptCountAsync(db, org, target.Id)).Should().Be(1);
    }

    [Test]
    public async Task Retention_DryRunCountsOnly_ApplyErasesOldPlayerButNotFamilyOrOtherClub()
    {
        Guid org = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        await using ApplicationDbContext db = Db();
        Enrollment expired = Solo(org, Today.AddYears(-3));
        expired.ContactEmail = "family@example.test";
        LessonSerie active = new() { Organization = expired.Organization, TennisClub = expired.LessonSerie!.TennisClub, Name = "Active", StartDate = Today, EndDate = Today.AddDays(30) };
        Enrollment family = new() { Organization = expired.Organization, LessonSerie = active, StudentName = "Synthetic Two", DateOfBirth = new DateOnly(2002, 1, 1), ContactEmail = "family@example.test", Status = EnrollmentStatus.Confirmed };
        Enrollment foreign = Solo(other, Today.AddYears(-3));
        EnrollmentForm form = new() { LessonSerie = expired.LessonSerie, OrganizationId = org };
        FormField field = new() { EnrollmentForm = form, Label = "Question", Type = FormFieldType.Text };
        db.Enrollments.AddRange(expired, family, foreign);
        db.FormResponses.Add(new FormResponse { Enrollment = expired, FormField = field, Value = "Synthetic private answer" });
        await db.SaveChangesAsync();

        PlayerRetentionReport dry = (await Retention(db).RunAsync(org, dryRun: true)).Value!;
        dry.Should().Be(new PlayerRetentionReport(2, 1, 0), "family shares the contact address but is a different player");
        (await db.Enrollments.AnyAsync(x => x.Id == expired.Id)).Should().BeTrue();

        PlayerRetentionReport applied = (await Retention(db).RunAsync(org, dryRun: false)).Value!;
        applied.Erased.Should().Be(1);
        (await db.Enrollments.AnyAsync(x => x.Id == expired.Id)).Should().BeFalse();
        (await db.FormResponses.CountAsync(x => x.EnrollmentId == expired.Id)).Should().Be(0);
        (await db.Enrollments.AnyAsync(x => x.Id == family.Id)).Should().BeTrue();
        (await db.Enrollments.AnyAsync(x => x.Id == foreign.Id)).Should().BeTrue();
    }
}
