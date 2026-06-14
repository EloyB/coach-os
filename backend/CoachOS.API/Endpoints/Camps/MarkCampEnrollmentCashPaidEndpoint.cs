using CoachOS.API.Extensions;
using CoachOS.Application.Camps;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

/// <summary>
/// Admin/trainer markeert de cash-betaling van een kamp-inschrijving als betaald,
/// wat de inschrijving bevestigt en de bevestigingsmail verstuurt.
/// </summary>
public class MarkCampEnrollmentCashPaidEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/camps/{campId:guid}/enrollments/{enrollmentId:guid}/mark-cash-paid",
            async (Guid campId, Guid enrollmentId, ICampService service, HttpContext ctx, CancellationToken ct) =>
            {
                Result result = await service.MarkEnrollmentCashPaidAsync(
                    campId, enrollmentId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Camps");
    }
}
