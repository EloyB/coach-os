using CoachOS.API.Extensions;
using CoachOS.Application.Payments;
using CoachOS.Application.Payments.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Payments;

/// <summary>
/// Publieke status-poll voor de thank-you-page (PR #5). De student kent zijn
/// <c>enrollmentId</c> uit de Mollie redirect URL en pollt elke paar seconden
/// tot de status terminal is. <c>sync=true</c> dwingt een Mollie-roundtrip
/// af — bedoeld voor lokaal dev waar webhooks niet aankomen.
/// </summary>
public class GetPaymentStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/public/payments/by-enrollment/{enrollmentId:guid}",
            async (Guid enrollmentId, bool? sync, IPaymentService service, CancellationToken ct) =>
            {
                Result<PaymentStatusDto> result = await service.GetPaymentStatusForEnrollmentAsync(
                    enrollmentId, sync ?? false, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .RequireRateLimiting("public")
        .WithTags("Payments");
    }
}
