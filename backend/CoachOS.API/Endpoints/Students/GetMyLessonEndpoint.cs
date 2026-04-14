using CoachOS.API.Extensions;
using CoachOS.Application.Students;

namespace CoachOS.API.Endpoints.Students;

public class GetMyLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/student/lessons/{id:guid}", async (
            Guid id,
            IStudentLessonsService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.GetMyLessonAsync(ctx.GetEmail(), id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Student"))
        .WithTags("Students");
    }
}
