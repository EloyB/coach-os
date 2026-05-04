using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.StandaloneLessons;
using CoachOS.Application.StandaloneLessons.DTOs;

namespace CoachOS.API.Endpoints.StandaloneLessons;

public class CreateStandaloneLessonEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/standalone-lessons",
            async (CreateStandaloneLessonRequest request,
                   IStandaloneLessonService service, HttpContext ctx, CancellationToken ct) =>
            {
                Domain.Models.Result<Guid> result =
                    await service.CreateAsync(ctx.GetOrganizationId(), request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/standalone-lessons/{result.Value}", result.Value)
                    : result.ToErrorResult();
            })
        .RequireAuthorization()
        .AddEndpointFilter<ValidationFilter<CreateStandaloneLessonRequest>>()
        .WithTags("StandaloneLessons");
    }
}
