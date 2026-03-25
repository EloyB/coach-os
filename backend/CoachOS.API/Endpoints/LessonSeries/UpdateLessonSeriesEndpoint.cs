using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.LessonSeries;
using CoachOS.Application.LessonSeries.DTOs;

namespace CoachOS.API.Endpoints.LessonSeries;

public class UpdateLessonSeriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/lessonseries/{id:guid}", async (Guid id, UpdateLessonSeriesRequest request, ILessonSeriesService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<UpdateLessonSeriesRequest>>()
        .WithTags("LessonSeries");
    }
}
