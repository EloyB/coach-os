using CoachOS.API.Extensions;
using CoachOS.Application.RescheduleRequests;

namespace CoachOS.API.Endpoints.RescheduleRequests;

public class GetRescheduleRequestsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/reschedule-requests", async (
            IRescheduleRequestService service,
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
