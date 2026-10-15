using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.Application.Enrollments;
using CoachOS.Application.LessonSerie;

namespace CoachOS.API.Endpoints.LessonSerie;

public class GetEnrollmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/lessonseries/{id:guid}/enrollments",
            async (Guid id, IEnrollmentService service, ILessonSerieService series,
                HttpContext ctx, CancellationToken ct) =>
            {
                // Deze lijst bevat volledige PII (e-mail, telefoon, geboortedatum,
                // formulierantwoorden). Beperk een hoofdtrainer tot z'n eigen club-reeksen,
                // net als het planning-endpoint — Admin behoudt volledige toegang.
                var access = await HeadTrainerAccess.EnsureSerieAccessAsync(ctx, series, id, ct);
                if (!access.IsSuccess) return access.ToErrorResult();

                var result = await service.GetSeriesEnrollmentsAsync(id, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .WithTags("Enrollments");
    }
}
