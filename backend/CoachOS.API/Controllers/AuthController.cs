using CoachOS.Application.Auth.Commands.Login;
using CoachOS.Application.Auth.Commands.Register;
using CoachOS.Application.Auth.DTOs;
using CoachOS.Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachOS.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterCommand command,
        CancellationToken ct)
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

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginCommand command,
        CancellationToken ct)
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
}
