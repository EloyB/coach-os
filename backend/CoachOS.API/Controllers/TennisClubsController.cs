using CoachOS.Application.TennisClubs.Commands.CreateTennisClub;
using CoachOS.Application.TennisClubs.Commands.DeleteTennisClub;
using CoachOS.Application.TennisClubs.DTOs;
using CoachOS.Application.TennisClubs.Queries.GetTennisClubs;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoachOS.API.Controllers;

[Authorize]
[ApiController]
[Route("api/tennisclubs")]
public class TennisClubsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TennisClubsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetOrganizationId() =>
        Guid.Parse(User.FindFirst("organizationId")!.Value);

    [HttpGet]
    public async Task<ActionResult<List<TennisClubDto>>> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTennisClubsQuery { OrganizationId = GetOrganizationId() }, ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateTennisClubCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command with { OrganizationId = GetOrganizationId() }, ct);
            return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors.Select(e => e.ErrorMessage));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteTennisClubCommand { Id = id, OrganizationId = GetOrganizationId() }, ct);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }
}
