using CoachOS.API.Extensions;
using CoachOS.Application.Dashboard;

namespace CoachOS.API.Endpoints.Dashboard;

public class GetMyClubOverviewEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Bezetting en aandachtspunten van de club(s) waar de ingelogde gebruiker hoofdtrainer is.
        // Bewust zonder financiële gegevens. Zonder hoofdtrainer-club geeft de service een 403.
        app.MapGet("/dashboard/club-overview", async (
            IDashboardService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.GetMyClubOverviewAsync(
                ctx.GetOrganizationId(), ctx.GetHeadTrainerClubIds(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Trainer"))
        .WithTags("Dashboard");
    }
}
