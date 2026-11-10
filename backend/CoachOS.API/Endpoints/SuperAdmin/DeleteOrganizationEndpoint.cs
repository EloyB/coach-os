using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Privacy;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.SuperAdmin;

/// <summary>Definitieve verwijdering van een organisatie, na bevestiging met de exacte naam.</summary>
public class DeleteOrganizationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/super-admin/organizations/{organizationId:guid}/offboarding/delete", async (
                Guid organizationId, DeleteOrganizationRequest request,
                IOrganizationOffboardingService service, CancellationToken ct) =>
            {
                Result<OrganizationDeletedDto> result = await service.DeleteAsync(organizationId, request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
            .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
            .AddEndpointFilter<ValidationFilter<DeleteOrganizationRequest>>()
            .WithTags("SuperAdmin");
    }
}
