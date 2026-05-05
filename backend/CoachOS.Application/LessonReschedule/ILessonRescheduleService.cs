using CoachOS.Application.LessonReschedule.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.LessonReschedule;

/// <summary>
/// Verplaats een lesmoment naar een nieuwe datum/tijd. Maakt een nieuwe Lesson aan,
/// markt het origineel als geannuleerd met link naar de vervanger, draagt invitations
/// (standalone) of single-lesson enrollments (serie-instance) over en notificeert
/// alle betrokkenen per e-mail.
/// </summary>
public interface ILessonRescheduleService
{
    Task<Result<RescheduleLessonResultDto>> RescheduleAsync(
        Guid organizationId,
        Guid lessonId,
        RescheduleLessonRequest request,
        CancellationToken ct = default);
}
