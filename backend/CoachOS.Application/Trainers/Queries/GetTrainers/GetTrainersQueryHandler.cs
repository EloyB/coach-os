using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Trainers.Queries.GetTrainers;

public class GetTrainersQueryHandler : IRequestHandler<GetTrainersQuery, Result<List<TrainerDto>>>
{
    private readonly ITrainerService _trainerService;

    public GetTrainersQueryHandler(ITrainerService trainerService)
    {
        _trainerService = trainerService;
    }

    public Task<Result<List<TrainerDto>>> Handle(GetTrainersQuery request, CancellationToken ct) =>
        _trainerService.GetTrainersAsync(request.OrganizationId, ct);
}
