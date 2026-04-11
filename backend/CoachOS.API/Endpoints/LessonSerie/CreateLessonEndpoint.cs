using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.LessonSerie;
using CoachOS.Application.LessonSerie.DTOs;

namespace CoachOS.API.Endpoints.LessonSerie;

public class CreateLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries/{id:guid}/lessons", async (Guid id, CreateLessonRequest request, ILessonSerieService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.AddLessonAsync(id, ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/lessonseries/{id}", result.Value)
                : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<CreateLessonRequest>>()
        .WithTags("LessonSerie");
    }
}
