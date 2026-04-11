using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Enrollments;
using CoachOS.Application.Enrollments.DTOs;

namespace CoachOS.API.Endpoints.LessonSerie;

public class SaveEnrollmentFormEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/lessonseries/{id:guid}/form",
            async (Guid id, SaveEnrollmentFormRequest request, IEnrollmentService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.SaveFormAsync(id, ctx.GetOrganizationId(), request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .AddEndpointFilter<ValidationFilter<SaveEnrollmentFormRequest>>()
        .WithTags("Enrollments");
    }
}
