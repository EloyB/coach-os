using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Trainers.Commands.InviteTrainer;

public class InviteTrainerCommandHandler : IRequestHandler<InviteTrainerCommand, Result<Guid>>
{
    private readonly ITrainerService _trainerService;

    public InviteTrainerCommandHandler(ITrainerService trainerService)
    {
        _trainerService = trainerService;
    }

    public Task<Result<Guid>> Handle(InviteTrainerCommand request, CancellationToken ct) =>
        _trainerService.InviteAsync(
            request.OrganizationId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.InviteBaseUrl,
            ct);
}
