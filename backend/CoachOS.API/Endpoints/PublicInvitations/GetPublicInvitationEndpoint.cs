using CoachOS.API.Extensions;
using CoachOS.Application.StandaloneLessons;
using CoachOS.Application.StandaloneLessons.DTOs;

namespace CoachOS.API.Endpoints.PublicInvitations;

public class GetPublicInvitationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/public/invitations/{token}",
            async (string token, IInvitationPublicService service, CancellationToken ct) =>
            {
                Domain.Models.Result<PublicInvitationDto> result =
                    await service.GetByTokenAsync(token, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .WithTags("PublicInvitations");
    }
}
