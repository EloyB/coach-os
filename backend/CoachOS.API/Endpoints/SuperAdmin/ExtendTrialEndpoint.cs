using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.SuperAdmin;
using CoachOS.Application.SuperAdmin.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class ExtendTrialEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/super-admin/organizations/{organizationId:guid}/extend-trial", async (
            Guid organizationId,
            ExtendTrialRequest request,
            ISuperAdminService service,
            CancellationToken ct) =>
        {
            Result result = await service.ExtendTrialAsync(organizationId, request.Days, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
        .AddEndpointFilter<ValidationFilter<ExtendTrialRequest>>()
        .WithTags("SuperAdmin");
    }
}
