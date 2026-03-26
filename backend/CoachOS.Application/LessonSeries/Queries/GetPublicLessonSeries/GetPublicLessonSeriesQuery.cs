using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.LessonSeries.Queries.GetPublicLessonSeries;

public record GetPublicLessonSeriesQuery : IRequest<Result<PublicLessonSeriesDto>>
{
    public Guid LessonSeriesId { get; init; }
}
