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
                var result = await service.CancelGroupAsync(id, groupId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Enrollments");
    }
}
