using CoachOS.API.Extensions;
using CoachOS.Application.StandaloneLessons;

namespace CoachOS.API.Endpoints.StandaloneLessons;

public class ResendInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/standalone-lessons/{id:guid}/invitations/{invId:guid}/resend",
            async (Guid id, Guid invId,
                   IStandaloneLessonService service, HttpContext ctx, CancellationToken ct) =>
            {
                Domain.Models.Result result =
                    await service.ResendInvitationAsync(ctx.GetOrganizationId(), id, invId, ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization()
        .WithTags("StandaloneLessons");
    }
}
