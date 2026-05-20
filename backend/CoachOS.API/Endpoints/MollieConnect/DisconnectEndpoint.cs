using CoachOS.API.Extensions;
using CoachOS.Application.MollieConnect;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.MollieConnect;

public class DisconnectEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/mollie-connect/disconnect", async (
            IMollieConnectService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Result result = await service.DisconnectAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("MollieConnect");
    }
}
