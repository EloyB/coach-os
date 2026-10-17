using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Camps;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

/// <summary>Annuleert alle actieve leden van een kampgroep atomair. Admin of hoofdtrainer.</summary>
public class CancelCampEnrollmentGroupEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/camps/{campId:guid}/enrollment-groups/{groupId:guid}",
            async (Guid campId, Guid groupId, ICampService service, HttpContext ctx, CancellationToken ct) =>
            {
                Result write = HeadTrainerAccess.EnsureManualEnrollmentAllowed(ctx);
                if (!write.IsSuccess) return write.ToErrorResult();

                Result result = await service.CancelGroupAsync(
                    campId, groupId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Camps");
    }
}
