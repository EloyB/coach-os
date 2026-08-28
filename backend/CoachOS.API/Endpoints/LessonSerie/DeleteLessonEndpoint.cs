using CoachOS.API.Extensions;
using CoachOS.Application.LessonSerie;

namespace CoachOS.API.Endpoints.LessonSerie;

public class DeleteLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessonseries/{seriesId:guid}/lessons/{lessonId:guid}", async (Guid seriesId, Guid lessonId, string? applyTo, ILessonSerieService service, HttpContext ctx, CancellationToken ct) =>
        {
            bool wholeSlot = string.Equals(applyTo, "slot", StringComparison.OrdinalIgnoreCase);
            var result = await service.DeleteLessonAsync(seriesId, lessonId, ctx.GetOrganizationId(), wholeSlot, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("LessonSerie");
    }
}
