using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Entities;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Planning;

public class PlanningService(
    ILessonSerieRepository lessonSeriesRepo,
    IEnrollmentRepository enrollmentRepo,
    IEnrollmentGroupRepository enrollmentGroupRepo,
    ITimeSlotPreferenceRepository timeSlotPreferenceRepo,
    IScheduleAssignmentRepository scheduleAssignmentRepo,
    IUserLookupService userLookup) : IPlanningService
{
    public async Task<Result<PlanningOverviewDto>> GenerateProposalAsync(
        Guid seriesId, Guid organizationId, bool force = false, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<PlanningOverviewDto>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        // Protect manual edits: after the first generate the status flips to Planning.
        // Re-running then would wipe admin tweaks — require an explicit force flag.
        if (series.PlanningStatus == PlanningStatus.Planning && !force)
            return await GetPlanningOverviewAsync(seriesId, organizationId, ct);

        var enrollments = await enrollmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var activeEnrollments = enrollments
            .Where(e => e.Status is EnrollmentStatus.Confirmed or EnrollmentStatus.Pending)
            .ToList();
        var groups = await enrollmentGroupRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var preferences = await timeSlotPreferenceRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var slots = series.WeeklyTemplate.ToList();

        if (slots.Count == 0)
            return Result<PlanningOverviewDto>.Fail(
                new Error(ErrorCodes.Validation, "Er zijn nog geen tijdslots aangemaakt."));

        var prefsByEnrollment = PlanningProposalBuilder.BuildPreferencesLookup(preferences);

        // On force-regenerate, preserve manually locked assignments: exclude their
        // units from the algorithm and reduce the affected slots' remaining capacity.
        var existingAssignments = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var lockedAssignments = existingAssignments
            .Where(a => a.IsLocked && a.Status == ScheduleAssignmentStatus.Proposed)
            .ToList();

        var lockedGroupIds = lockedAssignments
            .Where(a => a.EnrollmentGroupId.HasValue)
            .Select(a => a.EnrollmentGroupId!.Value)
            .ToHashSet();
        var lockedEnrollmentIds = lockedAssignments
            .Where(a => a.EnrollmentId.HasValue)
            .Select(a => a.EnrollmentId!.Value)
            .ToHashSet();

        var capacityUsedBySlot = lockedAssignments
            .GroupBy(a => a.WeeklyTemplateEntryId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(a => a.EnrollmentGroup?.Members.Count ?? 1));

        var (units, _) = PlanningProposalBuilder.BuildUnits(
            activeEnrollments, groups, prefsByEnrollment, lockedGroupIds, lockedEnrollmentIds);

        var slotInfos = slots
            .Select(s => new SlotInfo(
                s.Id,
                Math.Max(0, s.MaxStudents - capacityUsedBySlot.GetValueOrDefault(s.Id, 0))))
            .ToList();
        var input = new SchedulingInput(units, slotInfos);

        var result = SchedulingAlgorithm.Generate(input);

        // Clear existing proposed assignments (uses ExecuteDelete to avoid tracking conflicts)
        await scheduleAssignmentRepo.RemoveProposedBySeriesAsync(seriesId, organizationId, ct);

        var newAssignments = result.Assignments.Select(a => new ScheduleAssignment
        {
            OrganizationId = organizationId,
            LessonSerieId = seriesId,
            WeeklyTemplateEntryId = a.WeeklyTemplateEntryId,
            EnrollmentGroupId = a.GroupId,
            EnrollmentId = a.EnrollmentId,
            Status = ScheduleAssignmentStatus.Proposed,
            IsAutoMerged = a.IsAutoMerged,
        });

        await scheduleAssignmentRepo.AddRangeAsync(newAssignments, ct);

        series.PlanningStatus = PlanningStatus.Planning;
        await lessonSeriesRepo.SaveChangesAsync(ct);

        return await GetPlanningOverviewAsync(seriesId, organizationId, ct);
    }

    public async Task<Result<PlanningOverviewDto>> GetPlanningOverviewAsync(
        Guid seriesId, Guid organizationId, CancellationToken ct = default)
    {
        var series = await lessonSeriesRepo.GetByIdAsync(seriesId, organizationId, ct);
        if (series is null)
            return Result<PlanningOverviewDto>.Fail(
                new Error(ErrorCodes.NotFound, "Lessenreeks niet gevonden."));

        var assignments = await scheduleAssignmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var enrollments = await enrollmentRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var groups = await enrollmentGroupRepo.GetBySeriesAsync(seriesId, organizationId, ct);
        var preferences = await timeSlotPreferenceRepo.GetBySeriesAsync(seriesId, organizationId, ct);

        var activeEnrollments = enrollments
            .Where(e => e.Status is EnrollmentStatus.Confirmed or EnrollmentStatus.Pending)
            .ToList();

        var prefsRawByEnrollment = PlanningProposalBuilder.BuildPreferencesLookup(preferences);
        var prefsByEnrollment = prefsRawByEnrollment
            .ToDictionary(
                kv => kv.Key,
                kv => kv.Value.ToDictionary(p => p.Key, p => p.Value.ToString()));

        var trainerIds = series.WeeklyTemplate
            .Where(s => s.TrainerId.HasValue)
            .Select(s => s.TrainerId!.Value)
            .Distinct()
            .ToList();
        var trainerNames = trainerIds.Count > 0
            ? await userLookup.GetUserNamesByIdsAsync(trainerIds, ct)
            : new Dictionary<Guid, string>();

        var timeSlotDtos = series.WeeklyTemplate
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(slot => new PlanningTimeSlotDto
            {
                Id = slot.Id,
                DayOfWeek = slot.DayOfWeek,
                StartTime = slot.StartTime.ToString("HH:mm"),
                EndTime = slot.EndTime.ToString("HH:mm"),
                CourtName = slot.CourtName,
                TrainerId = slot.TrainerId,
                TrainerName = slot.TrainerId.HasValue
                    ? trainerNames.GetValueOrDefault(slot.TrainerId.Value)
                    : null,
                MaxCapacity = slot.MaxStudents,
            })
            .ToList();

        var enrollmentDtos = activeEnrollments
            .Select(e => new PlanningEnrollmentDto
            {
                Id = e.Id,
                StudentName = e.StudentName,
                StudentEmail = e.StudentEmail,
                StudentPhone = e.StudentPhone,
                IsOpenToGrouping = e.IsOpenToGrouping,
                GroupId = e.EnrollmentGroupId,
                Preferences = prefsByEnrollment.GetValueOrDefault(e.Id, new()),
            })
            .ToList();

        var groupDtos = groups
            .Select(g => new PlanningGroupDto
            {
                Id = g.Id,
                Name = g.Name,
                LeaderEnrollmentId = g.LeaderEnrollmentId,
                MemberEnrollmentIds = activeEnrollments
                    .Where(e => e.EnrollmentGroupId == g.Id)
                    .Select(e => e.Id)
                    .ToList(),
            })
            .ToList();

        var assignmentDtos = assignments
            .Select(a => new PlanningAssignmentDto
            {
                Id = a.Id,
                TimeSlotId = a.WeeklyTemplateEntryId,
                EnrollmentId = a.EnrollmentId,
                GroupId = a.EnrollmentGroupId,
                Status = a.Status.ToString(),
                IsAutoMerged = a.IsAutoMerged,
                IsLocked = a.IsLocked,
            })
            .ToList();

        DateTime? lastEditedAt = assignments.Count > 0
            ? assignments.Max(a => a.UpdatedAt == default ? a.CreatedAt : a.UpdatedAt)
            : null;

        var conflicts = PlanningProposalBuilder.BuildConflicts(
            activeEnrollments, assignments, series.WeeklyTemplate.ToList(), prefsRawByEnrollment);

        return Result<PlanningOverviewDto>.Ok(new PlanningOverviewDto
        {
            PlanningStatus = series.PlanningStatus.ToString(),
            PlanningLastEditedAt = lastEditedAt,
            TimeSlots = timeSlotDtos,
            Enrollments = enrollmentDtos,
            Groups = groupDtos,
            Assignments = assignmentDtos,
            Conflicts = conflicts,
        });
    }
}
