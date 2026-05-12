using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Configuration;
using CoachOS.Application.SuperAdmin;
using CoachOS.Application.SuperAdmin.DTOs;
using Microsoft.Extensions.Options;

namespace CoachOS.API.Endpoints.SuperAdmin;

public class CreateAdminEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/super-admin/admins", async (
            CreateAdminRequest request,
            ISuperAdminService service,
            IOptions<AppOptions> appOptions,
            CancellationToken ct) =>
        {
            string inviteBaseUrl = appOptions.Value.FrontendBaseUrl;
            var result = await service.CreateAdminWithOrganizationAsync(request, inviteBaseUrl, ct);
            return result.IsSuccess
                ? Results.Created($"/super-admin/admins/{result.Value}", new { userId = result.Value })
                : result.ToErrorResult();
        })
        .RequireAuthorization(AuthorizationPolicies.SuperAdmin)
        .AddEndpointFilter<ValidationFilter<CreateAdminRequest>>()
        .WithTags("SuperAdmin");
    }
}
