using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Trainers.Commands.ReassignTrainerSeries;

public class ReassignTrainerSeriesCommandHandler : IRequestHandler<ReassignTrainerSeriesCommand, Result>
{
    private readonly ITrainerService _trainerService;

    public ReassignTrainerSeriesCommandHandler(ITrainerService trainerService)
    {
        _trainerService = trainerService;
    }

    public Task<Result> Handle(ReassignTrainerSeriesCommand request, CancellationToken ct) =>
        _trainerService.ReassignSeriesAsync(request.FromTrainerId, request.ToTrainerId, request.OrganizationId, ct);
}
