using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

/// <summary>Beheerder schrijft handmatig een deelnemer in op een kamp. Admin of hoofdtrainer.</summary>
public class CreateManualCampEnrollmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/camps/{campId:guid}/enrollments/manual",
            async (Guid campId, CreateManualCampEnrollmentRequest request, ICampEnrollmentService service,
                HttpContext ctx, CancellationToken ct) =>
            {
                Result write = HeadTrainerAccess.EnsureManualEnrollmentAllowed(ctx);
                if (!write.IsSuccess) return write.ToErrorResult();

                Result<Guid> result = await service.CreateManualAsync(
                    campId, ctx.GetOrganizationId(), request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .AddEndpointFilter<ValidationFilter<CreateManualCampEnrollmentRequest>>()
        .WithTags("Camps");
    }
}
