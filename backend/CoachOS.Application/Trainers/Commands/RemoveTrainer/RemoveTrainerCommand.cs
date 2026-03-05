using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Trainers.Commands.RemoveTrainer;

public record RemoveTrainerCommand : IRequest<Result>
{
    public Guid TrainerId { get; set; }
    public Guid OrganizationId { get; set; }
}
