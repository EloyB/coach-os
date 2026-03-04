using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.LessonSeries.Commands.DeleteLessonSeries;

public record DeleteLessonSeriesCommand : IRequest<Result>
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
}
