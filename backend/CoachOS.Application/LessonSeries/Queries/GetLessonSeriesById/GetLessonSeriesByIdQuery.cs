using CoachOS.Application.Common.Models;
using CoachOS.Application.LessonSeries.DTOs;
using MediatR;

namespace CoachOS.Application.LessonSeries.Queries.GetLessonSeriesById;

public record GetLessonSeriesByIdQuery : IRequest<Result<LessonSeriesDto>>
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
}
