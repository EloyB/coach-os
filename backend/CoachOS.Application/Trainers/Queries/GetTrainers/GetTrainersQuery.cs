using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Trainers.Queries.GetTrainers;

public record GetTrainersQuery : IRequest<Result<List<TrainerDto>>>
{
    public Guid OrganizationId { get; set; }
}
