using CoachOS.Application.LessonSeries.Commands.CreateLesson;
using CoachOS.Application.LessonSeries.Commands.CreateLessonSeries;
using CoachOS.Application.LessonSeries.Commands.DeleteLesson;
using CoachOS.Application.LessonSeries.Commands.DeleteLessonSeries;
using CoachOS.Application.LessonSeries.Commands.UpdateLessonSeries;
using CoachOS.Application.LessonSeries.DTOs;
using CoachOS.Application.LessonSeries.Queries.GetLessonSeries;
using CoachOS.Application.LessonSeries.Queries.GetLessonSeriesById;
using CoachOS.Application.LessonSeries.Queries.GetOrganizationMembers;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CoachOS.API.Controllers;

[Authorize]
[ApiController]
[Route("api/lessonseries")]
public class LessonSeriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public LessonSeriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetOrganizationId() =>
        Guid.Parse(User.FindFirst("organizationId")!.Value);

    [HttpGet]
    public async Task<ActionResult<List<LessonSeriesDto>>> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLessonSeriesQuery { OrganizationId = GetOrganizationId() }, ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("members")]
    public async Task<ActionResult<List<LessonSeriesMemberDto>>> GetMembers(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetOrganizationMembersQuery { OrganizationId = GetOrganizationId() }, ct);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Errors);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LessonSeriesDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetLessonSeriesByIdQuery { Id = id, OrganizationId = GetOrganizationId() }, ct);
        return result.Succeeded ? Ok(result.Data) : NotFound(result.Errors);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateLessonSeriesCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command with { OrganizationId = GetOrganizationId() }, ct);
            return result.Succeeded ? CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data) : BadRequest(result.Errors);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors.Select(e => e.ErrorMessage));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LessonSeriesDto>> Update(Guid id, [FromBody] UpdateLessonSeriesCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command with { Id = id, OrganizationId = GetOrganizationId() }, ct);
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
        var result = await _mediator.Send(new DeleteLessonSeriesCommand { Id = id, OrganizationId = GetOrganizationId() }, ct);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }

    [HttpPost("{id:guid}/lessons")]
    public async Task<ActionResult<Guid>> AddLesson(Guid id, [FromBody] CreateLessonCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(command with { LessonSeriesId = id, OrganizationId = GetOrganizationId() }, ct);
            return result.Succeeded ? CreatedAtAction(nameof(GetById), new { id }, result.Data) : BadRequest(result.Errors);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors.Select(e => e.ErrorMessage));
        }
    }

    [HttpDelete("{seriesId:guid}/lessons/{lessonId:guid}")]
    public async Task<ActionResult> DeleteLesson(Guid seriesId, Guid lessonId, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteLessonCommand
        {
            LessonId = lessonId,
            SeriesId = seriesId,
            OrganizationId = GetOrganizationId(),
        }, ct);
        return result.Succeeded ? NoContent() : BadRequest(result.Errors);
    }
}
