using CoachOS.API.Extensions;
using CoachOS.Application.Configuration;
using CoachOS.Application.Trainers;
using Microsoft.Extensions.Options;

namespace CoachOS.API.Endpoints.Trainers;

public class ResendTrainerInviteEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trainers/{id:guid}/resend-invite", async (Guid id, ITrainerService service, IOptions<AppOptions> appOptions, HttpContext ctx, CancellationToken ct) =>
        {
            string inviteBaseUrl = appOptions.Value.FrontendBaseUrl;
            var result = await service.ResendInviteAsync(id, ctx.GetOrganizationId(), inviteBaseUrl, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Trainers");
    }
}
