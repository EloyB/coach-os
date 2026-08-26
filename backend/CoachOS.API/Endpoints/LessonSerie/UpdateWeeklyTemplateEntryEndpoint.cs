using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.LessonSerie;
using CoachOS.Application.LessonSerie.DTOs;

namespace CoachOS.API.Endpoints.LessonSerie;

public class UpdateWeeklyTemplateEntryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/lessonseries/{seriesId:guid}/weekly-template/{entryId:guid}", async (
            Guid seriesId, Guid entryId, UpdateWeekSlotRequest request,
            ILessonSerieService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.UpdateWeekSlotAsync(seriesId, entryId, ctx.GetOrganizationId(), request, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<UpdateWeekSlotRequest>>()
        .WithTags("LessonSerie");
    }
}
