using CoachOS.API.Extensions;
using CoachOS.Application.Enrollments;

namespace CoachOS.API.Endpoints.Public;

public class GetPublicLessonSeriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/public/lessonseries/{id:guid}",
            async (Guid id, IEnrollmentService service, CancellationToken ct) =>
            {
                var result = await service.GetPublicLessonSeriesAsync(id, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .WithTags("Public");
    }
}
