using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Trainers.Commands.DeactivateTrainer;

public class DeactivateTrainerCommandHandler : IRequestHandler<DeactivateTrainerCommand, Result>
{
    private readonly ITrainerService _trainerService;

    public DeactivateTrainerCommandHandler(ITrainerService trainerService)
    {
        _trainerService = trainerService;
    }

    public Task<Result> Handle(DeactivateTrainerCommand request, CancellationToken ct) =>
        _trainerService.DeactivateAsync(request.TrainerId, request.OrganizationId, ct);
}
