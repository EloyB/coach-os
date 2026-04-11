using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.LessonSerie;
using CoachOS.Application.LessonSerie.DTOs;

namespace CoachOS.API.Endpoints.LessonSerie;

public class UpdateLessonSerieEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/lessonseries/{id:guid}", async (Guid id, UpdateLessonSerieRequest request, ILessonSerieService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<UpdateLessonSerieRequest>>()
        .WithTags("LessonSerie");
    }
}
