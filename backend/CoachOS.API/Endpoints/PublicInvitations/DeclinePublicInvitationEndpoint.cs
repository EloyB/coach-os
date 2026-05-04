using CoachOS.API.Extensions;
using CoachOS.Application.StandaloneLessons;
using CoachOS.Application.StandaloneLessons.DTOs;

namespace CoachOS.API.Endpoints.PublicInvitations;

public class DeclinePublicInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/public/invitations/{token}/decline",
            async (string token, IInvitationPublicService service, CancellationToken ct) =>
            {
                Domain.Models.Result<PublicInvitationDto> result =
                    await service.DeclineAsync(token, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .WithTags("PublicInvitations");
    }
}
