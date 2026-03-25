using CoachOS.API.Extensions;
using CoachOS.Application.LessonSeries;

namespace CoachOS.API.Endpoints.LessonSeries;

public class DeleteLessonSeriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessonseries/{id:guid}", async (Guid id, ILessonSeriesService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.DeleteAsync(id, ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("LessonSeries");
    }
}
