using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Enrollments;
using CoachOS.Application.Enrollments.DTOs;

namespace CoachOS.API.Endpoints.LessonSerie;

public class UpdateBasicEnrollmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/lessonseries/{id:guid}/enrollments/{enrollmentId:guid}",
            async (Guid id, Guid enrollmentId, UpdateBasicEnrollmentRequest request,
                IEnrollmentService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.UpdateBasicEnrollmentAsync(
                    id, enrollmentId, ctx.GetOrganizationId(), request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<UpdateBasicEnrollmentRequest>>()
        .WithTags("Enrollments");
    }
}
