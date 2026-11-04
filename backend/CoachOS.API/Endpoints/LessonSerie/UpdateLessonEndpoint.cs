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
            // Admin: geen beperking. Trainer (ook hoofdtrainer): enkel de eigen les.
            Guid? actingTrainerId = ctx.IsAdmin() ? null : ctx.GetUserId();
            var result = await service.UpdateLessonAsync(
                seriesId, lessonId, ctx.GetOrganizationId(), request, ct, actingTrainerId);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .AddEndpointFilter<ValidationFilter<UpdateLessonRequest>>()
        .WithTags("LessonSeries");
    }
}
