using CoachOS.API.Extensions;
using CoachOS.Application.Enrollments;

namespace CoachOS.API.Endpoints.Public;

public class GetEnrollmentFormEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/public/lessonseries/{id:guid}/form",
            async (Guid id, IEnrollmentService service, CancellationToken ct) =>
            {
                var result = await service.GetEnrollmentFormAsync(id, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .RequireRateLimiting("public")
        .WithTags("Public");
    }
}
