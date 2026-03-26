using CoachOS.Application.Common.Models;
using CoachOS.Application.Enrollments.Commands.SubmitEnrollment;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.Enrollments.Queries.GetEnrollmentForm;
using CoachOS.Application.LessonSeries.Queries.GetPublicLessonSeries;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoachOS.API.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/public")]
public class PublicController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("lessonseries/{id:guid}")]
    public async Task<ActionResult<PublicLessonSeriesDto>> GetLessonSeries(Guid id, CancellationToken ct)
    {
        Result<PublicLessonSeriesDto> result = await _mediator.Send(
            new GetPublicLessonSeriesQuery { LessonSeriesId = id }, ct);
        return result.Succeeded ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpGet("lessonseries/{id:guid}/form")]
    public async Task<ActionResult<EnrollmentFormDto?>> GetEnrollmentForm(Guid id, CancellationToken ct)
    {
        Result<EnrollmentFormDto?> result = await _mediator.Send(
            new GetEnrollmentFormQuery { LessonSeriesId = id }, ct);
        return result.Succeeded ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpPost("lessonseries/{id:guid}/enroll")]
    public async Task<ActionResult<Guid>> Enroll(Guid id, [FromBody] SubmitEnrollmentCommand command, CancellationToken ct)
    {
        try
        {
            Result<Guid> result = await _mediator.Send(command with { LessonSeriesId = id }, ct);
            return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors.Select(e => e.ErrorMessage));
        }
    }
}
