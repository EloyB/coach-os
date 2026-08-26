using CoachOS.API.Extensions;
using CoachOS.Application.LessonSerie;

namespace CoachOS.API.Endpoints.LessonSerie;

public class DeleteWeeklyTemplateEntryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessonseries/{seriesId:guid}/weekly-template/{entryId:guid}", async (
            Guid seriesId, Guid entryId, ILessonSerieService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.DeleteWeekSlotAsync(seriesId, entryId, ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("LessonSerie");
    }
}
