using CoachOS.API.Auth;
using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Enrollments;
using CoachOS.Application.Enrollments.DTOs;
using CoachOS.Application.LessonSerie;
using CoachOS.Domain.Models;
namespace CoachOS.API.Endpoints.LessonSerie;

public class CreateManualEnrollmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/lessonseries/{id:guid}/enrollments/manual",
            async (Guid id, CreateManualEnrollmentRequest request, IEnrollmentService service,
                ILessonSerieService series, HttpContext ctx, CancellationToken ct) =>
            {
                Result access = await HeadTrainerAccess.EnsureSerieAccessAsync(ctx, series, id, ct);
                if (!access.IsSuccess) return access.ToErrorResult();
                Result write = HeadTrainerAccess.EnsureManualEnrollmentAllowed(ctx);
                if (!write.IsSuccess) return write.ToErrorResult();

                Result<Guid> result = await service.CreateManualEnrollmentAsync(
                    id, request, ctx.GetOrganizationId(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .RequireAuthorization(policy => policy.RequireRole("Admin", "Trainer"))
        .AddEndpointFilter<ValidationFilter<CreateManualEnrollmentRequest>>()
        .WithTags("Enrollments");
    }
}
