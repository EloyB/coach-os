using CoachOS.API.Extensions;
using CoachOS.Application.Dashboard;

namespace CoachOS.API.Endpoints.Dashboard;

public class GetDashboardMetricsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/dashboard/metrics", async (IDashboardService service, HttpContext ctx, int? weeks, CancellationToken ct) =>
        {
            Guid orgId = ctx.GetOrganizationId();
            int effectiveWeeks = weeks ?? 7;
            var result = await service.GetMetricsAsync(orgId, effectiveWeeks, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("Dashboard");
    }
}
