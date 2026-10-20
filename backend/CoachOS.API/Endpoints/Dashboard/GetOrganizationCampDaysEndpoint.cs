using CoachOS.API.Extensions;
using CoachOS.Application.Dashboard;

namespace CoachOS.API.Endpoints.Dashboard;

public class GetOrganizationCampDaysEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Alle kampdagen van de organisatie in een datumbereik, voor de agenda op het admin-dashboard.
        app.MapGet("/dashboard/organization-camp-days", async (
            IDashboardService service, HttpContext ctx, DateOnly from, DateOnly to, CancellationToken ct) =>
        {
            var result = await service.GetOrganizationCampDaysAsync(ctx.GetOrganizationId(), from, to, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Dashboard");
    }
}
