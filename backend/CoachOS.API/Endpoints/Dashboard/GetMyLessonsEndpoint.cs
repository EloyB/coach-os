using CoachOS.API.Extensions;
using CoachOS.Application.Dashboard;

namespace CoachOS.API.Endpoints.Dashboard;

public class GetMyLessonsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Eigen lessen van de ingelogde gebruiker (reeks + los) in een datumbereik, voor het
        // trainersdashboard. Bewust enkel op de eigen TrainerId: er is geen org-brede variant.
        app.MapGet("/dashboard/my-lessons", async (
            IDashboardService service, HttpContext ctx, DateOnly from, DateOnly to, CancellationToken ct) =>
        {
            Guid orgId = ctx.GetOrganizationId();
            Guid userId = ctx.GetUserId();
            var result = await service.GetMyLessonsAsync(orgId, userId, from, to, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("Dashboard");
    }
}
