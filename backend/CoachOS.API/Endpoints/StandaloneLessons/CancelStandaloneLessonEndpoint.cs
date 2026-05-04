using CoachOS.API.Extensions;
using CoachOS.Application.StandaloneLessons;

namespace CoachOS.API.Endpoints.StandaloneLessons;

public class CancelStandaloneLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/standalone-lessons/{id:guid}",
            async (Guid id, IStandaloneLessonService service, HttpContext ctx, CancellationToken ct) =>
            {
                Domain.Models.Result result =
                    await service.CancelAsync(ctx.GetOrganizationId(), id, ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization()
        .WithTags("StandaloneLessons");
    }
}
