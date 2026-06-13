using CoachOS.API.Extensions;
using CoachOS.Application.Payments;
using CoachOS.Application.Payments.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.API.Endpoints.Public;

/// <summary>
/// Publieke status-poll voor de kamp-thank-you-page. De deelnemer kent zijn
/// <c>campEnrollmentId</c> uit de Mollie redirect URL en pollt tot de status
/// terminal is. <c>sync=true</c> dwingt een Mollie-roundtrip af, bedoeld voor
/// lokaal dev waar webhooks niet aankomen.
/// </summary>
public class GetCampPaymentStatusEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/public/camp-enrollments/{id:guid}/payment-status",
            async (Guid id, bool? sync, IPaymentService service, CancellationToken ct) =>
            {
                Result<PaymentStatusDto> result = await service.GetPaymentStatusForCampEnrollmentAsync(
                    id, sync ?? false, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToErrorResult();
            })
        .AllowAnonymous()
        .RequireRateLimiting("public")
        .WithTags("Public");
    }
}
