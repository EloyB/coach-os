using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.LessonSeries.Commands.DeleteLesson;

public record DeleteLessonCommand : IRequest<Result>
{
    public Guid LessonId { get; set; }
    public Guid SeriesId { get; set; }
    public Guid OrganizationId { get; set; }
}
