using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Enrollments.Commands.SaveEnrollmentForm;

public record SaveFormFieldDto
{
    public Guid? Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public int Type { get; init; }
    public bool IsRequired { get; init; }
    public int Order { get; init; }
    public List<string>? Options { get; init; }
}

public record SaveEnrollmentFormCommand : IRequest<Result<Guid>>
{
    public Guid LessonSeriesId { get; init; }
    public Guid OrganizationId { get; init; }
    public List<SaveFormFieldDto> Fields { get; init; } = new();
}
