using CoachOS.API.Extensions;
using CoachOS.Application.StandaloneLessons;
using CoachOS.Application.StandaloneLessons.DTOs;

namespace CoachOS.API.Endpoints.PublicInvitations;

public class AcceptPublicInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/public/invitations/{token}/accept",
            async (string token, IInvitationPublicService service, CancellationToken ct) =>
            {
                Domain.Models.Result<PublicInvitationDto> result =
                    await service.AcceptAsync(token, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .WithTags("PublicInvitations");
    }
}
