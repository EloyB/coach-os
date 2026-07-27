using CoachOS.API.Extensions;
using CoachOS.Application.StudentConfirmation;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Enrollments;

/// <summary>
/// Admin/trainer markeert de overschrijving van een reeksinschrijving als betaald,
/// wat de inschrijving bevestigt en de bevestigingsmail verstuurt.
/// </summary>
public class MarkEnrollmentCashPaidEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/enrollments/{enrollmentId:guid}/mark-cash-paid",
            async (Guid enrollmentId, IStudentConfirmationService service, HttpContext ctx, CancellationToken ct) =>
            {
                Result result = await service.MarkEnrollmentCashPaidAsync(
                    enrollmentId, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Enrollments");
    }
}
