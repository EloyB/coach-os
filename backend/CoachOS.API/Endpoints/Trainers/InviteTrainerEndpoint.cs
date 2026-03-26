using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;

namespace CoachOS.API.Endpoints.Trainers;

public class InviteTrainerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trainers/invite", async (InviteTrainerRequest request, ITrainerService service, HttpContext ctx, CancellationToken ct) =>
        {
            string inviteBaseUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
            var result = await service.InviteAsync(ctx.GetOrganizationId(), request.FirstName, request.LastName, request.Email, inviteBaseUrl, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<InviteTrainerRequest>>()
        .WithTags("Trainers");
    }
}
