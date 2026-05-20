using CoachOS.API.Extensions;
using CoachOS.Application.MollieConnect;
using CoachOS.Application.MollieConnect.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.MollieConnect;

public class GetStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/mollie-connect/status", async (
            IMollieConnectService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Result<MollieConnectionStatusDto> result = await service.GetStatusAsync(
                ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("MollieConnect");
    }
}
