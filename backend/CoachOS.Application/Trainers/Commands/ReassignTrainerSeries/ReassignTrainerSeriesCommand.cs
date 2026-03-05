using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Trainers.Commands.ReassignTrainerSeries;

public record ReassignTrainerSeriesCommand : IRequest<Result>
{
    public Guid OrganizationId { get; set; }
    public Guid FromTrainerId { get; set; }
    public Guid ToTrainerId { get; set; }
}
