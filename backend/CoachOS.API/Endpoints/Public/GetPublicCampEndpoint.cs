using CoachOS.API.Extensions;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Public;

public class GetPublicCampEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/public/camps/{id:guid}", async (Guid id, ICampEnrollmentService service, CancellationToken ct) =>
        {
            Result<PublicCampDto> result = await service.GetPublicCampAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .AllowAnonymous()
        .RequireRateLimiting("public")
        .WithTags("Public");
    }
}
