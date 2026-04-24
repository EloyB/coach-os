using CoachOS.API.Extensions;
using CoachOS.Application.Reschedule;

namespace CoachOS.API.Endpoints.Reschedule;

public class GetRescheduleRequestsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/reschedule-requests", async (
            IRescheduleService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.GetPendingAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("Reschedule");
    }
}
