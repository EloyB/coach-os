using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Reschedule;
using CoachOS.Application.Reschedule.DTOs;

namespace CoachOS.API.Endpoints.Reschedule;

public class CreateRescheduleRequestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/student/lessons/{assignmentId:guid}/reschedule", async (
            Guid assignmentId,
            CreateRescheduleRequest request,
            IRescheduleService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.RequestAsync(
                assignmentId, ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess
                ? Results.Created($"/reschedule-requests/{result.Value}", result.Value)
                : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<CreateRescheduleRequest>>()
        .WithTags("Reschedule");
    }
}
