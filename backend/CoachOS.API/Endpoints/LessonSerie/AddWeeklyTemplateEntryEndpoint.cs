using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.LessonSerie;
using CoachOS.Application.LessonSerie.DTOs;

namespace CoachOS.API.Endpoints.LessonSerie;

public class AddWeeklyTemplateEntryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries/{id:guid}/weekly-template", async (Guid id, AddWeeklyTemplateEntryRequest request, ILessonSerieService service, HttpContext ctx, CancellationToken ct) =>
        {
            Domain.Models.Result<Guid> result =
                await service.AddWeeklyTemplateEntryAsync(id, ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/lessonseries/{id}", result.Value)
                : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<AddWeeklyTemplateEntryRequest>>()
        .WithTags("LessonSerie");
    }
}
