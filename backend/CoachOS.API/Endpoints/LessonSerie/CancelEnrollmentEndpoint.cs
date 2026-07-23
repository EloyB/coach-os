using CoachOS.API.Extensions;
using CoachOS.Application.Enrollments;

namespace CoachOS.API.Endpoints.LessonSerie;

public class CancelEnrollmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessonseries/{id:guid}/enrollments/{enrollmentId:guid}",
            async (Guid id, Guid enrollmentId, IEnrollmentService service, HttpContext ctx, CancellationToken ct) =>
            {
                var result = await service.CancelEnrollmentAsync(id, enrollmentId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Enrollments");
    }
}
