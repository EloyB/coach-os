using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.RescheduleRequests;
using CoachOS.Application.RescheduleRequests.DTOs;

namespace CoachOS.API.Endpoints.RescheduleRequests;

public class CreateRescheduleRequestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/student/lessons/{assignmentId:guid}/reschedule", async (
            Guid assignmentId,
            CreateRescheduleRequest request,
            IRescheduleRequestService service,
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
