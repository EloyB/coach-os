using CoachOS.API.Extensions;
using CoachOS.Application.Dashboard;

namespace CoachOS.API.Endpoints.Dashboard;

public class GetMyLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Detail van één eigen les (spelers, club, baan, uur, ...) voor de popup op het trainersdashboard.
        // Een les van een andere trainer geeft 404, geen 403, zodat het bestaan niet lekt.
        app.MapGet("/dashboard/my-lessons/{lessonId:guid}", async (
            Guid lessonId, IDashboardService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.GetMyLessonAsync(
                ctx.GetOrganizationId(), ctx.GetUserId(), lessonId, ctx.GetHeadTrainerClubIds(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .WithTags("Dashboard");
    }
}
