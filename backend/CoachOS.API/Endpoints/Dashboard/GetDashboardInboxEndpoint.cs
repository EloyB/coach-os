using CoachOS.API.Extensions;
using CoachOS.Application.Dashboard;

namespace CoachOS.API.Endpoints.Dashboard;

public class GetDashboardInboxEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/dashboard/inbox", async (IDashboardService service, HttpContext ctx, int? limit, CancellationToken ct) =>
        {
            Guid orgId = ctx.GetOrganizationId();
            int effectiveLimit = limit ?? 10;
            var result = await service.GetInboxAsync(orgId, effectiveLimit, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("Dashboard");
    }
}
