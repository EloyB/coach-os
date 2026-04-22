using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Reschedule;
using CoachOS.Application.Reschedule.DTOs;

namespace CoachOS.API.Endpoints.Reschedule;

public class ResolveRescheduleRequestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("/reschedule-requests/{id:guid}", async (
            Guid id,
            ResolveRescheduleRequest request,
            IRescheduleService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.ResolveAsync(
                id, ctx.GetOrganizationId(), ctx.GetUserId(), request, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<ResolveRescheduleRequest>>()
        .WithTags("Reschedule");
    }
}
