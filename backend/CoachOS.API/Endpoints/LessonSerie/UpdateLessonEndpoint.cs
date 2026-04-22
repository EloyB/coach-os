using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.LessonSerie;
using CoachOS.Application.LessonSerie.DTOs;

namespace CoachOS.API.Endpoints.LessonSerie;

public class UpdateLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("/lessonseries/{seriesId:guid}/lessons/{lessonId:guid}", async (
            Guid seriesId,
            Guid lessonId,
            UpdateLessonRequest request,
            ILessonSerieService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.UpdateLessonAsync(seriesId, lessonId, ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<UpdateLessonRequest>>()
        .WithTags("LessonSeries");
    }
}
