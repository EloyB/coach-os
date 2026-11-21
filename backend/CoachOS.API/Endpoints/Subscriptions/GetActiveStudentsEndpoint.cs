using CoachOS.API.Extensions;
using CoachOS.Application.Subscriptions;
using CoachOS.Application.Subscriptions.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Subscriptions;

public class GetActiveStudentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/subscription/active-students", async (
            IActiveStudentCountService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Result<ActiveStudentsDto> result = await service.GetActiveStudentsAsync(ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Subscription");
    }
}
