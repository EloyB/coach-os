using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.LessonSeries.Commands.CreateLessonSeries;

public record CreateLessonSeriesCommand : IRequest<Result<Guid>>
{
    public Guid OrganizationId { get; set; }
    public Guid TrainerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Level { get; set; }
    public decimal Price { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public Guid TennisClubId { get; set; }
}
