using CoachOS.API.Extensions;
using CoachOS.Application.Privacy;
using CoachOS.Application.Privacy.DTOs;
using CoachOS.Domain.Enums;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Privacy;

/// <summary>Alleen-lezen overzicht vóór het definitief verwijderen van een speler (Admin).</summary>
public sealed class PreviewPlayerErasureEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        Map(app, "/privacy/enrollments/{enrollmentId:guid}/erasure-preview", PrivacyErasureKind.Enrollment);
        Map(app, "/privacy/camp-enrollments/{enrollmentId:guid}/erasure-preview", PrivacyErasureKind.CampEnrollment);
    }

    private static void Map(IEndpointRouteBuilder app, string route, PrivacyErasureKind kind)
    {
        app.MapGet(route, async (Guid enrollmentId, IPlayerErasureService service, HttpContext ctx, CancellationToken ct) =>
        {
            Result<PlayerErasurePreviewDto> result = await service.PreviewAsync(
                ctx.GetOrganizationId(), ctx.GetUserId(), kind, enrollmentId, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
        })
        .RequireAuthorization(policy => policy.RequireRole("Admin"))
        .WithTags("Privacy");
    }
}
