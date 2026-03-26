using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;

namespace CoachOS.API.Endpoints.Trainers;

public class AcceptInviteEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trainers/accept-invite", async (AcceptInviteRequest request, ITrainerService service, CancellationToken ct) =>
        {
            var result = await service.AcceptInviteAsync(request.Token, request.Password, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<AcceptInviteRequest>>()
        .WithTags("Trainers");
    }
}
