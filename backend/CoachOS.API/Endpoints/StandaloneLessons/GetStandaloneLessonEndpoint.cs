using CoachOS.API.Extensions;
using CoachOS.Application.StandaloneLessons;
using CoachOS.Application.StandaloneLessons.DTOs;

namespace CoachOS.API.Endpoints.StandaloneLessons;

public class GetStandaloneLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/standalone-lessons/{id:guid}",
            async (Guid id, IStandaloneLessonService service, HttpContext ctx, CancellationToken ct) =>
            {
                Domain.Models.Result<StandaloneLessonDetailDto> result =
                    await service.GetByIdAsync(ctx.GetOrganizationId(), id, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization()
        .WithTags("StandaloneLessons");
    }
}
