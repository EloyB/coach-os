using CoachOS.API.Extensions;
using CoachOS.Application.Dashboard;

namespace CoachOS.API.Endpoints.Dashboard;

public class GetOrganizationLessonsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Lessen van alle trainers en clubs van de organisatie, voor de agenda op het admin-dashboard.
        app.MapGet("/dashboard/organization-lessons", async (
            IDashboardService service, HttpContext ctx, DateOnly from, DateOnly to, CancellationToken ct) =>
        {
            var result = await service.GetOrganizationLessonsAsync(ctx.GetOrganizationId(), from, to, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Dashboard");
    }
}
