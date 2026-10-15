using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Enrollments;
using CoachOS.Application.LessonSerie;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.LessonSerie;

public class CancelEnrollmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessonseries/{id:guid}/enrollments/{enrollmentId:guid}",
            async (Guid id, Guid enrollmentId, IEnrollmentService service, ILessonSerieService series,
                HttpContext ctx, CancellationToken ct) =>
            {
                Result access = await HeadTrainerAccess.EnsureSerieAccessAsync(ctx, series, id, ct);
                if (!access.IsSuccess) return access.ToErrorResult();
                Result write = HeadTrainerAccess.EnsureManualEnrollmentAllowed(ctx);
                if (!write.IsSuccess) return write.ToErrorResult();

                Result<bool> result = await service.CancelEnrollmentAsync(
                    id, enrollmentId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Enrollments");
    }
}
