using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

/// <summary>Voegt een deelnemer toe aan een bestaande kampgroep. Admin of hoofdtrainer.</summary>
public class AddCampGroupMemberEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/camps/{campId:guid}/enrollment-groups/{groupId:guid}/members",
            async (Guid campId, Guid groupId, AddCampGroupMemberRequest request, ICampService service,
                HttpContext ctx, CancellationToken ct) =>
            {
                Result write = HeadTrainerAccess.EnsureManualEnrollmentAllowed(ctx);
                if (!write.IsSuccess) return write.ToErrorResult();

                Result<Guid> result = await service.AddGroupMemberAsync(
                    campId, groupId, ctx.GetOrganizationId(), request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/camps/{campId}/enrollments/{result.Value}", result.Value)
                    : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .AddEndpointFilter<ValidationFilter<AddCampGroupMemberRequest>>()
        .WithTags("Camps");
    }
}
