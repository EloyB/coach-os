using CoachOS.API.Extensions;
using CoachOS.API.Filters;
using CoachOS.Application.Camps;
using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Public;

/// <summary>
/// De betaalkeuze van een deelnemer voor een kampinschrijving: online (Mollie) of cash.
/// Online geeft een checkout-URL terug; cash legt een Pending cash-betaling vast.
/// </summary>
public class ChooseCampPaymentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/public/camp-enrollments/{id:guid}/pay",
            async (Guid id, ChooseCampPaymentRequest request, ICampEnrollmentService service, CancellationToken ct) =>
            {
                Result<ChooseCampPaymentResultDto> result = await service.ChoosePaymentAsync(id, request.Method, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .AddEndpointFilter<ValidationFilter<ChooseCampPaymentRequest>>()
        .RequireRateLimiting("public")
        .WithTags("Public");
    }
}
