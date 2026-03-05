using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Common.Models;
using CoachOS.Application.Trainers.Commands.AcceptInvite;
using CoachOS.Application.Trainers.Commands.DeactivateTrainer;
using CoachOS.Application.Trainers.Commands.InviteTrainer;
using CoachOS.Application.Trainers.Queries.GetTrainers;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachOS.API.Controllers;

[ApiController]
[Route("api/trainers")]
public class TrainersController : ControllerBase
{
    private readonly IMediator _mediator;

    public TrainersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetOrganizationId() =>
        Guid.Parse(User.FindFirst("organizationId")!.Value);

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<TrainerDto>>> GetTrainers(CancellationToken ct)
    {
        Result<List<TrainerDto>> result = await _mediator.Send(
            new GetTrainersQuery { OrganizationId = GetOrganizationId() }, ct);

        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("invite")]
    public async Task<ActionResult<Guid>> InviteTrainer([FromBody] InviteTrainerCommand command, CancellationToken ct)
    {
        try
        {
            string inviteBaseUrl = $"{Request.Scheme}://{Request.Host}";
            Result<Guid> result = await _mediator.Send(
                command with { OrganizationId = GetOrganizationId(), InviteBaseUrl = inviteBaseUrl }, ct);

            return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors.Select(e => e.ErrorMessage));
        }
    }

    [AllowAnonymous]
    [HttpPost("accept-invite")]
    public async Task<ActionResult<AuthResponseDto>> AcceptInvite([FromBody] AcceptInviteCommand command, CancellationToken ct)
    {
        try
        {
            Result<AuthResponseDto> result = await _mediator.Send(command, ct);
            return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors.Select(e => e.ErrorMessage));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeactivateTrainer(Guid id, CancellationToken ct)
    {
        Result result = await _mediator.Send(
            new DeactivateTrainerCommand { TrainerId = id, OrganizationId = GetOrganizationId() }, ct);

        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }
}
