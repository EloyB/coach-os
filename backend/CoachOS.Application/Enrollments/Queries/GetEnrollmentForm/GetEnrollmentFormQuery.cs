using CoachOS.Application.Common.Models;
using CoachOS.Application.Enrollments.DTOs;
using MediatR;

namespace CoachOS.Application.Enrollments.Queries.GetEnrollmentForm;

public record GetEnrollmentFormQuery : IRequest<Result<EnrollmentFormDto?>>
{
    public Guid LessonSeriesId { get; init; }
}
