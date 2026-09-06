using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Interfaces;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Planning;

public class TrainerPlanningService(
    ILessonSerieRepository lessonSerieRepository,
    IPlanningService planningService) : ITrainerPlanningService
{
    public async Task<Result<List<TrainerPlanningDto>>> GetAllAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var series = await lessonSerieRepository.GetByOrganizationAsync(organizationId, null, ct);
        var result = new List<TrainerPlanningDto>(series.Count);

        foreach (var lessonSerie in series)
        {
            var planning = await planningService.GetPlanningOverviewAsync(lessonSerie.Id, organizationId, ct);
            if (!planning.IsSuccess)
                return Result<List<TrainerPlanningDto>>.Fail(planning.Errors);

            var overview = planning.Value!;
            result.Add(new TrainerPlanningDto
            {
                LessonSerieId = lessonSerie.Id,
                LessonSerieName = lessonSerie.Name,
                PlanningStatus = overview.PlanningStatus,
                PlanningLastEditedAt = overview.PlanningLastEditedAt,
                TimeSlots = overview.TimeSlots,
                Enrollments = overview.Enrollments
                    .Select(e => new TrainerPlanningEnrollmentDto
                    {
                        Id = e.Id,
                        StudentName = e.StudentName,
                        IsOpenToGrouping = e.IsOpenToGrouping,
                        GroupId = e.GroupId,
                    })
                    .ToList(),
                Groups = overview.Groups,
                Assignments = overview.Assignments,
                Conflicts = overview.Conflicts,
            });
        }

        return Result<List<TrainerPlanningDto>>.Ok(result);
    }
}
