using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Trainers.Commands.RemoveTrainer;

public class RemoveTrainerCommandHandler : IRequestHandler<RemoveTrainerCommand, Result>
{
    private readonly ITrainerService _trainerService;

    public RemoveTrainerCommandHandler(ITrainerService trainerService)
    {
        _trainerService = trainerService;
    }

    public Task<Result> Handle(RemoveTrainerCommand request, CancellationToken ct) =>
        _trainerService.RemoveAsync(request.TrainerId, request.OrganizationId, ct);
}
