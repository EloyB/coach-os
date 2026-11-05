using CoachOS.API.Extensions;
using CoachOS.Application.Privacy;

namespace CoachOS.API.Endpoints.Privacy;

public sealed class PlayerErasureEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        Map(app, "/privacy/enrollments/{enrollmentId:guid}/erasure", false);
        Map(app, "/privacy/camp-enrollments/{enrollmentId:guid}/erasure", true);
    }

    private static void Map(IEndpointRouteBuilder app, string route, bool camp)
    {
        app.MapPost(route, async (Guid enrollmentId, PlayerErasureRequest request, IPlayerErasureService service, HttpContext context, CancellationToken ct) =>
        {
            if (request.Confirmation?.Length > 250 || request.PreviewHash?.Length > 64) return Results.BadRequest();
            PlayerErasureResult result = await service.ProcessAsync(context.GetOrganizationId(), enrollmentId, camp, request, ct, context.GetUserId());
            if (result.Holds.Contains("forbidden")) return Results.Forbid();
            if (result.Holds.Contains("not_found")) return Results.NotFound();
            return Results.Ok(result);
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Privacy");
    }
}
