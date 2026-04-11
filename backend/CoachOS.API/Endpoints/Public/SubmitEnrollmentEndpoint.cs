using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Enrollments;
using CoachOS.Application.Enrollments.DTOs;

namespace CoachOS.API.Endpoints.Public;

public class SubmitEnrollmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/public/lessonseries/{id:guid}/enroll",
            async (Guid id, SubmitEnrollmentRequest request, IEnrollmentService service, CancellationToken ct) =>
            {
                var result = await service.SubmitEnrollmentAsync(id, request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<SubmitEnrollmentRequest>>()
        .RequireRateLimiting("public")
        .WithTags("Public");
    }
}
