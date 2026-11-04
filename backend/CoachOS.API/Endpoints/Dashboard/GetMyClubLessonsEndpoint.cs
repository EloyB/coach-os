using CoachOS.API.Extensions;
using CoachOS.Application.Dashboard;

namespace CoachOS.API.Endpoints.Dashboard;

public class GetMyClubLessonsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Lessen van alle trainers in de club(s) waar de ingelogde gebruiker hoofdtrainer is, voor het
        // hoofdtrainer-dashboard. Zonder hoofdtrainer-club geeft de service een 403.
        app.MapGet("/dashboard/club-lessons", async (
            IDashboardService service, HttpContext ctx, DateOnly from, DateOnly to, CancellationToken ct) =>
        {
            var result = await service.GetMyClubLessonsAsync(
                ctx.GetOrganizationId(), ctx.GetHeadTrainerClubIds(), from, to, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Trainer"))
        .WithTags("Dashboard");
    }
}
