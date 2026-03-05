using CoachOS.Application.Common.Models;
using CoachOS.Application.LessonSeries.DTOs;
using MediatR;

namespace CoachOS.Application.LessonSeries.Queries.GetLessonSeries;

public record GetLessonSeriesQuery : IRequest<Result<List<LessonSeriesDto>>>
{
    public Guid OrganizationId { get; set; }
    public Guid? TrainerId { get; set; }
}
