using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Privacy;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Privacy;

/// <summary>Definitief verwijderen van een speler na naamsbevestiging (Admin).</summary>
public sealed class ErasePlayerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        Map(app, "/privacy/enrollments/{enrollmentId:guid}/erasure", PrivacyErasureKind.Enrollment);
        Map(app, "/privacy/camp-enrollments/{enrollmentId:guid}/erasure", PrivacyErasureKind.CampEnrollment);
    }

    private static void Map(IEndpointRouteBuilder app, string route, PrivacyErasureKind kind)
    {
        app.MapPost(route, async (Guid enrollmentId, ErasePlayerRequest request, IPlayerErasureService service,
            HttpContext ctx, CancellationToken ct) =>
        {
            Result<PlayerErasureResultDto> result = await service.EraseAsync(
                ctx.GetOrganizationId(), ctx.GetUserId(), kind, enrollmentId, request, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .AddEndpointFilter<ValidationFilter<ErasePlayerRequest>>()
        .WithTags("Privacy");
    }
}
