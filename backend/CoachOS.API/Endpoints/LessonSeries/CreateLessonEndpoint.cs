using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.LessonSeries;
using CoachOS.Application.LessonSeries.DTOs;

namespace CoachOS.API.Endpoints.LessonSeries;

public class CreateLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries/{id:guid}/lessons", async (Guid id, CreateLessonRequest request, ILessonSeriesService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.AddLessonAsync(id, ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/lessonseries/{id}", result.Value)
                : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<CreateLessonRequest>>()
        .WithTags("LessonSeries");
    }
}
