using CoachOS.API.Extensions;
using CoachOS.Application.LessonSerie;

namespace CoachOS.API.Endpoints.LessonSerie;

public class GetLessonSerieByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/{id:guid}", async (Guid id, ILessonSerieService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ctx.GetOrganizationId(), ct);
            if (!result.IsSuccess)
                return Results.NotFound(result.Errors.Select(e => e.Message));

            // Zelfde zichtbaarheid als de reeksenlijst: admin ziet alles, een trainer enkel reeksen
            // waarin hij les geeft of van de club waar hij hoofdtrainer is.
            if (!ctx.IsAdmin())
            {
                Guid userId = ctx.GetUserId();
                bool canView = result.Value!.Lessons.Any(l => l.TrainerId == userId)
                    || ctx.GetHeadTrainerClubIds().Contains(result.Value.TennisClubId);
                if (!canView)
                    return Results.Json(
                        new[] { "Geen toegang tot deze reeks." }, statusCode: StatusCodes.Status403Forbidden);
            }

            return Results.Ok(result.Value);
        })
        .RequireAuthorization()
        .WithTags("LessonSerie");
    }
}
