using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.LessonSeries.Commands.CreateLesson;

public record CreateLessonCommand : IRequest<Result<Guid>>
{
    public Guid LessonSeriesId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string CourtName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
