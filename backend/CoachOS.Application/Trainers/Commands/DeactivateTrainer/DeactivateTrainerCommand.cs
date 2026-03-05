using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Trainers.Commands.DeactivateTrainer;

public record DeactivateTrainerCommand : IRequest<Result>
{
    public Guid TrainerId { get; set; }
    public Guid OrganizationId { get; set; }
}
