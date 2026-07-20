using CoachOS.API.Extensions;
using CoachOS.Application.Planning;
using CoachOS.Application.Planning.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Planning;

public class GetSlotSuggestionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/tennisclubs/{clubId:guid}/slot-suggestions", async (
            Guid clubId,
            ISlotSuggestionService service,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            Result<List<SlotSuggestionDto>> result =
                await service.SuggestSlotsAsync(ctx.GetOrganizationId(), clubId, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Planning");
    }
}
