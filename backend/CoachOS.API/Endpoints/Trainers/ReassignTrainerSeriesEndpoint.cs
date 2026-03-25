using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Trainers;
using CoachOS.Application.Trainers.DTOs;

namespace CoachOS.API.Endpoints.Trainers;

public class ReassignTrainerSeriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trainers/{id:guid}/reassign-series", async (Guid id, ReassignSeriesRequest request, ITrainerService service, HttpContext ctx, CancellationToken ct) =>
        {
            var result = await service.ReassignSeriesAsync(id, request.ToTrainerId, ctx.GetOrganizationId(), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<ReassignSeriesRequest>>()
        .WithTags("Trainers");
    }
}
