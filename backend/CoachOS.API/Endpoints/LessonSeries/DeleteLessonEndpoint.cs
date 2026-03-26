using CoachOS.API.Extensions;
using CoachOS.Application.LessonSeries;

namespace CoachOS.API.Endpoints.LessonSeries;

public class DeleteLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessonseries/{seriesId:guid}/lessons/{lessonId:guid}", async (Guid seriesId, Guid lessonId, ILessonSeriesService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.DeleteLessonAsync(seriesId, lessonId, ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("LessonSeries");
    }
}
