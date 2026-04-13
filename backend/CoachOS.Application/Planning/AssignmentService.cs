using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Planning;

public class AssignmentService(
    ILessonSerieRepository lessonSeriesRepo,
    IEnrollmentRepository enrollmentRepo,
    IEnrollmentGroupRepository enrollmentGroupRepo,
    IScheduleAssignmentRepository scheduleAssignmentRepo) : IAssignmentService
{
    public async Task<Result<bool>> CreateAssignmentAsync(
        Guid seriesId, CreateAssignmentRequest request,
        Guid organizationId, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        var slot = series.WeeklyTemplate.FirstOrDefault(s => s.Id == request.WeeklyTemplateEntryId);
        if (slot is null)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Tijdslot niet gevonden."));

        var existingAssignments = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);

        int addSize;
        Guid? enrollmentId = null;
        Guid? groupId = null;

        if (request.GroupId.HasValue)
        {
            var group = await enrollmentGroupRepo.GetByIdAsync(request.GroupId.Value, organizationId, ct);
            if (group is null || group.LessonSerieId != seriesId)
                return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Groep niet gevonden."));

            if (existingAssignments.Any(a => a.EnrollmentGroupId == request.GroupId))
                return Result<bool>.Fail(new Error(ErrorCodes.Validation, "Groep is al toegewezen."));

            groupId = group.Id;
            addSize = group.Members.Count;
        }
        else
        {
            var enrollment = await enrollmentRepo.GetByIdAsync(request.EnrollmentId!.Value, organizationId, ct);
            if (enrollment is null || enrollment.LessonSerieId != seriesId)
                return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Inschrijving niet gevonden."));

            if (existingAssignments.Any(a => a.EnrollmentId == request.EnrollmentId))
                return Result<bool>.Fail(new Error(ErrorCodes.Validation, "Inschrijving is al toegewezen."));

            enrollmentId = enrollment.Id;
            addSize = 1;
        }

        var capacityError = await EnsureSlotCapacityAsync(
            seriesId, organizationId, request.WeeklyTemplateEntryId, addSize, excludeAssignmentId: null, ct);
        if (capacityError is not null)
            return Result<bool>.Fail(capacityError);

        ScheduleAssignment assignment = new()
        {
            OrganizationId = organizationId,
            LessonSerieId = seriesId,
            WeeklyTemplateEntryId = request.WeeklyTemplateEntryId,
            EnrollmentId = enrollmentId,
            EnrollmentGroupId = groupId,
            Status = ScheduleAssignmentStatus.Proposed,
            IsAutoMerged = false,
            IsLocked = true,
        };

        await scheduleAssignmentRepo.AddRangeAsync([assignment], ct);
        await scheduleAssignmentRepo.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> UpdateAssignmentAsync(
        Guid seriesId, Guid assignmentId, UpdateAssignmentRequest request,
        Guid organizationId, CancellationToken ct = default)
    {
        var assignment = await scheduleAssignmentRepo.GetByIdAsync(assignmentId, organizationId, ct);
        if (assignment is null || assignment.LessonSerieId != seriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Toewijzing niet gevonden."));

        if (assignment.Status == ScheduleAssignmentStatus.Confirmed)
            return Result<bool>.Fail(
                new Error(ErrorCodes.Validation, "Bevestigde toewijzingen kunnen niet verplaatst worden."));

        var capacityError = await EnsureSlotCapacityAsync(
            seriesId, organizationId, request.WeeklyTemplateEntryId,
            addSize: assignment.EnrollmentGroup?.Members.Count ?? 1,
            excludeAssignmentId: assignment.Id, ct);
        if (capacityError is not null)
            return Result<bool>.Fail(capacityError);

        assignment.WeeklyTemplateEntryId = request.WeeklyTemplateEntryId;
        assignment.IsLocked = true;
        await scheduleAssignmentRepo.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteAssignmentAsync(
        Guid seriesId, Guid assignmentId, Guid organizationId, CancellationToken ct = default)
    {
        var assignment = await scheduleAssignmentRepo.GetByIdAsync(assignmentId, organizationId, ct);
        if (assignment is null || assignment.LessonSerieId != seriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Toewijzing niet gevonden."));

        if (assignment.Status == ScheduleAssignmentStatus.Confirmed)
            return Result<bool>.Fail(
                new Error(ErrorCodes.Validation, "Bevestigde toewijzingen kunnen niet verwijderd worden."));

        scheduleAssignmentRepo.RemoveRange([assignment]);
        await scheduleAssignmentRepo.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<Guid>> CreateGroupAsync(
        Guid seriesId, CreateGroupRequest request, Guid organizationId, CancellationToken ct = default)
    {
        var exists = await lessonSeriesRepo.ExistsAsync(seriesId, organizationId, ct);
        if (!exists)
            return Result<Guid>.Fail(new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        var enrollments = await enrollmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var selected = enrollments.Where(e => request.EnrollmentIds.Contains(e.Id)).ToList();

        if (selected.Count != request.EnrollmentIds.Count)
            return Result<Guid>.Fail(
                new Error(ErrorCodes.Validation, "Eén of meer inschrijvingen niet gevonden."));

        if (selected.Any(e => e.EnrollmentGroupId.HasValue))
            return Result<Guid>.Fail(
                new Error(ErrorCodes.Validation, "Eén of meer inschrijvingen zitten al in een groep."));

        var existingGroupCount = await enrollmentGroupRepo.CountBySeriesAsync(seriesId, organizationId, ct);
        var groupLetter = (char)('A' + existingGroupCount);

        EnrollmentGroup group = new()
        {
            OrganizationId = organizationId,
            LessonSerieId = seriesId,
            Name = $"Groep {groupLetter}",
            LeaderEnrollmentId = selected[0].Id,
        };

        await enrollmentGroupRepo.AddAsync(group, ct);

        foreach (var enrollment in selected)
            enrollment.EnrollmentGroupId = group.Id;

        await enrollmentGroupRepo.SaveChangesAsync(ct);

        return Result<Guid>.Ok(group.Id);
    }

    public async Task<Result<bool>> DissolveGroupAsync(
        Guid seriesId, Guid groupId, Guid organizationId, CancellationToken ct = default)
    {
        var group = await enrollmentGroupRepo.GetByIdAsync(groupId, organizationId, ct);
        if (group is null || group.LessonSerieId != seriesId)
            return Result<bool>.Fail(new Error(ErrorCodes.NotFound, "Groep niet gevonden."));

        foreach (var member in group.Members)
            member.EnrollmentGroupId = null;

        var assignments = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var groupAssignments = assignments.Where(a => a.EnrollmentGroupId == groupId).ToList();
        if (groupAssignments.Count > 0)
            scheduleAssignmentRepo.RemoveRange(groupAssignments);

        enrollmentGroupRepo.Delete(group);
        await enrollmentGroupRepo.SaveChangesAsync(ct);

        return Result<bool>.Ok(true);
    }

    private async Task<Error?> EnsureSlotCapacityAsync(
        Guid seriesId, Guid organizationId, Guid slotId, int addSize,
        Guid? excludeAssignmentId, CancellationToken ct)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        var slot = series?.WeeklyTemplate.FirstOrDefault(s => s.Id == slotId);
        if (slot is null) return null;

        var existing = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var currentCount = existing
            .Where(a => a.WeeklyTemplateEntryId == slotId && a.Id != excludeAssignmentId)
            .Sum(a => a.EnrollmentGroup?.Members.Count ?? 1);

        if (currentCount + addSize > slot.MaxStudents)
            return new Error(ErrorCodes.Validation,
                $"Tijdslot heeft geen plaats meer ({currentCount}/{slot.MaxStudents}).");

        return null;
    }
}
