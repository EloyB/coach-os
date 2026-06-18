using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Public;

public class SubmitCampEnrollmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/public/camps/{id:guid}/enroll",
            async (Guid id, SubmitCampEnrollmentRequest request, ICampEnrollmentService service, CancellationToken ct) =>
            {
                Result<SubmitCampEnrollmentResultDto> result = await service.SubmitAsync(id, request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<SubmitCampEnrollmentRequest>>()
        .RequireRateLimiting("public")
        .WithTags("Public");
    }
}
