using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Enrollments;

namespace CoachOS.API.Endpoints.LessonSerie;

public class CancelEnrollmentGroupEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/lessonseries/{id:guid}/enrollment-groups/{groupId:guid}",
            async (Guid id, Guid groupId, IEnrollmentService service, HttpContext ctx, CancellationToken ct) =>
            {
                // Hoofdtrainers zijn read-only: de Trainer-rol alleen volstaat niet als
                // autorisatiegrens (UI-verbergen is dat niet). Blokkeer de niet-admin hoofdtrainer.
                var writeAccess = HeadTrainerAccess.EnsureWriteAllowed(ctx);
                if (!writeAccess.IsSuccess) return writeAccess.ToErrorResult();

                var result = await service.CancelGroupAsync(id, groupId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Enrollments");
    }
}
