using CoachOS.Application.Common.Models;
using CoachOS.Application.Enrollments.DTOs;
using MediatR;

namespace CoachOS.Application.Enrollments.Queries.GetLessonSeriesEnrollments;

public record GetLessonSeriesEnrollmentsQuery : IRequest<Result<List<LessonSeriesEnrollmentDto>>>
{
    public Guid LessonSeriesId { get; init; }
    public Guid OrganizationId { get; init; }
}
