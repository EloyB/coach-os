using CoachOS.API.Extensions;
using CoachOS.Application.StandaloneLessons;
using CoachOS.Application.StandaloneLessons.DTOs;

namespace CoachOS.API.Endpoints.StandaloneLessons;

public class CancelStandaloneLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/standalone-lessons/{id:guid}/cancel",
            async (Guid id,
                CancelStandaloneLessonRequest? body,
                IStandaloneLessonService service,
                HttpContext ctx,
                CancellationToken ct) =>
            {
                Domain.Models.Result result =
                    await service.CancelAsync(ctx.GetOrganizationId(), id, body?.Reason, ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization()
        .WithTags("StandaloneLessons");
    }
}
