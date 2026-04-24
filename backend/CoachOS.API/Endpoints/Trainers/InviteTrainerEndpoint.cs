using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Configuration;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;
using Microsoft.Extensions.Options;

namespace CoachOS.API.Endpoints.Trainers;

public class InviteTrainerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trainers/invite", async (InviteTrainerRequest request, ITrainerService service, IOptions<AppOptions> appOptions, HttpContext ctx, CancellationToken ct) =>
        {
            string inviteBaseUrl = appOptions.Value.FrontendBaseUrl;
            var result = await service.InviteAsync(ctx.GetOrganizationId(), request.FirstName, request.LastName, request.Email, inviteBaseUrl, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<InviteTrainerRequest>>()
        .WithTags("Trainers");
    }
}
