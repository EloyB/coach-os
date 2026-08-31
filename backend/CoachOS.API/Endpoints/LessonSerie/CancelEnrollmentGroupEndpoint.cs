using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Enrollments;
using CoachOS.Application.LessonSerie;
using CoachOS.Domain.Models;
namespace CoachOS.API.Endpoints.LessonSerie;

public class CancelEnrollmentGroupEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessonseries/{id:guid}/enrollment-groups/{groupId:guid}",
            async (Guid id, Guid groupId, IEnrollmentService service, ILessonSerieService series, HttpContext ctx, CancellationToken ct) =>
            {
                Result access = await HeadTrainerAccess.EnsureSerieAccessAsync(ctx, series, id, ct);
                if (!access.IsSuccess) return access.ToErrorResult();
                Result writeAccess = HeadTrainerAccess.EnsureManualEnrollmentAllowed(ctx);
                if (!writeAccess.IsSuccess) return writeAccess.ToErrorResult();

                var result = await service.CancelGroupAsync(id, groupId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Enrollments");
    }
}
