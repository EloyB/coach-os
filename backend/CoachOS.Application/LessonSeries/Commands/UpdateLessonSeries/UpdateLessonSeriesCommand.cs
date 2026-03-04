using CoachOS.Application.Common.Models;
using CoachOS.Application.LessonSeries.DTOs;
using MediatR;

namespace CoachOS.Application.LessonSeries.Commands.UpdateLessonSeries;

public record UpdateLessonSeriesCommand : IRequest<Result<LessonSeriesDto>>
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TrainerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Level { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public Guid TennisClubId { get; set; }
}
