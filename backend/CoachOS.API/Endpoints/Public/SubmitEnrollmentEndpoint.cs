using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.Enrollments.Submit;

namespace CoachOS.API.Endpoints.Public;

public class SubmitEnrollmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/public/lessonseries/{id:guid}/enroll",
            async (Guid id, SubmitEnrollmentRequest request, IEnrollmentSubmissionService service, CancellationToken ct) =>
            {
                var result = await service.SubmitAsync(id, request, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<SubmitEnrollmentRequest>>()
        .RequireRateLimiting("public")
        .WithTags("Public");
    }
}
