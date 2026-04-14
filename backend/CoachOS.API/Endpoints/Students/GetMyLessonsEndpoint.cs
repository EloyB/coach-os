using CoachOS.API.Extensions;
using CoachOS.Application.Students;

namespace CoachOS.API.Endpoints.Students;

public class GetMyLessonsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/student/lessons", async (
            IStudentLessonsService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var result = await service.GetMyLessonsAsync(ctx.GetEmail(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Student"))
        .WithTags("Students");
    }
}
