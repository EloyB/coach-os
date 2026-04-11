using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.LessonSerie;
using CoachOS.Application.LessonSerie.DTOs;

namespace CoachOS.API.Endpoints.LessonSerie;

public class CreateLessonSerieEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries", async (CreateLessonSerieRequest request, ILessonSerieService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/lessonseries/{result.Value}", result.Value)
                : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<CreateLessonSerieRequest>>()
        .WithTags("LessonSerie");
    }
}
