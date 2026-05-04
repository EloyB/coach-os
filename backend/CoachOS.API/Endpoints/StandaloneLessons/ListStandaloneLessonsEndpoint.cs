using CoachOS.API.Extensions;
using CoachOS.Application.StandaloneLessons;
using CoachOS.Application.StandaloneLessons.DTOs;

namespace CoachOS.API.Endpoints.StandaloneLessons;

public class ListStandaloneLessonsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/standalone-lessons",
            async (IStandaloneLessonService service, HttpContext ctx, CancellationToken ct) =>
            {
                Domain.Models.Result<List<StandaloneLessonListItemDto>> result =
                    await service.GetAllAsync(ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization()
        .WithTags("StandaloneLessons");
    }
}
