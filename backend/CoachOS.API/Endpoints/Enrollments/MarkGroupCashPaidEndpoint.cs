using CoachOS.API.Extensions;
using CoachOS.Application.StudentConfirmation;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Enrollments;

/// <summary>
/// Admin/trainer markeert de hele groep (of solo) van een reeksinschrijving als betaald:
/// alle openstaande betalingen van de leden worden betaald en iedereen bevestigd.
/// </summary>
public class MarkGroupCashPaidEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/enrollments/{enrollmentId:guid}/mark-group-cash-paid",
            async (Guid enrollmentId, IStudentConfirmationService service, HttpContext ctx, CancellationToken ct) =>
            {
                Result result = await service.MarkGroupCashPaidAsync(
                    enrollmentId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Enrollments");
    }
}
