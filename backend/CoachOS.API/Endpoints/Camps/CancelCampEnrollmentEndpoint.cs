using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Camps;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

/// <summary>Annuleert één kamp-inschrijving (soft-cancel). Admin of hoofdtrainer.</summary>
public class CancelCampEnrollmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/camps/{campId:guid}/enrollments/{enrollmentId:guid}",
            async (Guid campId, Guid enrollmentId, ICampService service, HttpContext ctx, CancellationToken ct) =>
            {
                Result write = HeadTrainerAccess.EnsureManualEnrollmentAllowed(ctx);
                if (!write.IsSuccess) return write.ToErrorResult();

                Result result = await service.CancelEnrollmentAsync(
                    campId, enrollmentId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Camps");
    }
}
