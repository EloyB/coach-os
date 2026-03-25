using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.LessonSeries;
using CoachOS.Application.LessonSeries.DTOs;

namespace CoachOS.API.Endpoints.LessonSeries;

public class CreateLessonSeriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries", async (CreateLessonSeriesRequest request, ILessonSeriesService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.CreateAsync(ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/lessonseries/{result.Value}", result.Value)
                : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<CreateLessonSeriesRequest>>()
        .WithTags("LessonSeries");
    }
}
