using CoachOS.Application.Camps.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Camps;

public interface ICampEnrollmentService
{
    Task<Result<PublicCampDto>> GetPublicCampAsync(Guid campId, CancellationToken ct = default);
    Task<Result<CampEnrollmentFormDto?>> GetPublicFormAsync(Guid campId, CancellationToken ct = default);
    Task<Result<SubmitCampEnrollmentResultDto>> SubmitAsync(Guid campId, SubmitCampEnrollmentRequest request, CancellationToken ct = default);

    /// <summary>
    /// Betaalopties voor de publieke betaalpagina: de prijs en of online betalen
    /// beschikbaar is (= de organisatie van het kamp is aan Mollie gekoppeld).
    /// </summary>
    Task<Result<CampPaymentOptionsDto>> GetPaymentOptionsAsync(Guid campId, CancellationToken ct = default);

    /// <summary>
    /// Verwerkt de betaalkeuze van een deelnemer. Online maakt de Mollie payment aan
    /// en geeft een checkout-URL terug; cash legt een Pending cash-betaling vast en
    /// laat de inschrijving op PendingPayment staan (de coach bevestigt later).
    /// </summary>
    Task<Result<ChooseCampPaymentResultDto>> ChoosePaymentAsync(Guid campEnrollmentId, int method, CancellationToken ct = default);
}
