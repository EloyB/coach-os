using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.StandaloneLessons;
using CoachOS.Application.StandaloneLessons.DTOs;

namespace CoachOS.API.Endpoints.StandaloneLessons;

public class AddInvitationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/standalone-lessons/{id:guid}/invitations",
            async (Guid id, AddInvitationsRequest request,
                   IStandaloneLessonService service, HttpContext ctx, CancellationToken ct) =>
            {
                Domain.Models.Result result =
                    await service.AddInvitationsAsync(ctx.GetOrganizationId(), id, request.Emails, ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<AddInvitationsRequest>>()
        .WithTags("StandaloneLessons");
    }
}
