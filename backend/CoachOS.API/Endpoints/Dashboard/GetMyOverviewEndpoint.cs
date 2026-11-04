using CoachOS.API.Extensions;
using CoachOS.Application.Dashboard;

namespace CoachOS.API.Endpoints.Dashboard;

public class GetMyOverviewEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Samenvatting voor het trainersdashboard (volgende les, uren, openstaande bevestigingen).
        // Bewust enkel op de eigen TrainerId.
        app.MapGet("/dashboard/my-overview", async (IDashboardService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.GetMyOverviewAsync(ctx.GetOrganizationId(), ctx.GetUserId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("Dashboard");
    }
}
