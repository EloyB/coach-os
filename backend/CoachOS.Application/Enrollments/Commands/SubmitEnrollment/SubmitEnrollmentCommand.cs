using CoachOS.Application.Common.Models;
using CoachOS.Application.Enrollments.DTOs;
using MediatR;

namespace CoachOS.Application.Enrollments.Commands.SubmitEnrollment;

public record SubmitEnrollmentCommand : IRequest<Result<Guid>>
{
    public Guid LessonSeriesId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public string StudentEmail { get; init; } = string.Empty;
    public List<FormResponseValueDto> Responses { get; init; } = new();
}
