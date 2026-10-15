using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Camps;

/// <summary>Past basisgegevens en formulierantwoorden van een kamp-inschrijving aan. Enkel Admin.</summary>
public class UpdateCampEnrollmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/camps/{campId:guid}/enrollments/{enrollmentId:guid}",
            async (Guid campId, Guid enrollmentId, UpdateCampEnrollmentRequest request, ICampService service,
                HttpContext ctx, CancellationToken ct) =>
            {
                Result result = await service.UpdateEnrollmentAsync(
                    campId, enrollmentId, ctx.GetOrganizationId(), request, ct);
                return result.IsSuccess ? Results.NoContent() : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<UpdateCampEnrollmentRequest>>()
        .WithTags("Camps");
    }
}
