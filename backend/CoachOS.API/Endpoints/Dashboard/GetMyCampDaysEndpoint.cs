using CoachOS.API.Extensions;
using CoachOS.Application.Dashboard;

namespace CoachOS.API.Endpoints.Dashboard;

public class GetMyCampDaysEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Kampdagen waarvoor de ingelogde gebruiker als trainer is ingepland, in een datumbereik.
        app.MapGet("/dashboard/my-camp-days", async (
            IDashboardService service, HttpContext ctx, DateOnly from, DateOnly to, CancellationToken ct) =>
        {
            var result = await service.GetMyCampDaysAsync(
                ctx.GetOrganizationId(), ctx.GetUserId(), from, to, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("Dashboard");
    }
}
