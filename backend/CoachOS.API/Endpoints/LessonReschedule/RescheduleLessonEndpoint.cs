using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.LessonReschedule;
using CoachOS.Application.LessonReschedule.DTOs;

namespace CoachOS.API.Endpoints.LessonReschedule;

public class RescheduleLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessons/{id:guid}/reschedule",
            async (Guid id,
                RescheduleLessonRequest request,
                ILessonRescheduleService service,
                HttpContext ctx,
                CancellationToken ct) =>
            {
                Domain.Models.Result<RescheduleLessonResultDto> result =
                    await service.RescheduleAsync(
                        ctx.GetOrganizationId(), id, request, ct, ctx.IsAdmin() ? null : ctx.GetUserId());
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .AddEndpointFilter<ValidationFilter<RescheduleLessonRequest>>()
        .WithTags("Lessons");
    }
}
