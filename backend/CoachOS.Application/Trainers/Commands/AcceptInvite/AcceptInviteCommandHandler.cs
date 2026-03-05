using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Common.Models;
using MediatR;

namespace CoachOS.Application.Trainers.Commands.AcceptInvite;

public class AcceptInviteCommandHandler : IRequestHandler<AcceptInviteCommand, Result<AuthResponseDto>>
{
    private readonly ITrainerService _trainerService;

    public AcceptInviteCommandHandler(ITrainerService trainerService)
    {
        _trainerService = trainerService;
    }

    public Task<Result<AuthResponseDto>> Handle(AcceptInviteCommand request, CancellationToken ct) =>
        _trainerService.AcceptInviteAsync(request.Token, request.Password, ct);
}
