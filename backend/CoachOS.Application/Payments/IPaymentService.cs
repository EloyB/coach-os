using CoachOS.Application.Payments.DTOs;
using CoachOS.Domain.Models;

namespace CoachOS.Application.Payments;

public interface IPaymentService
{
    /// <summary>
    /// Creëert een Mollie payment voor een bestaande enrollment van een lesreeks
    /// met <c>PaymentMode.Immediate</c>. Verwacht dat de enrollment al
    /// gecommit is met status <c>PendingPayment</c>. Bij succes returnt een
    /// <see cref="CreatePaymentResultDto.CheckoutUrl"/> waar de FE de student
    /// naartoe redirect.
    /// </summary>
    Task<Result<CreatePaymentResultDto>> CreatePaymentForEnrollmentAsync(
        Guid enrollmentId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Idempotent webhook handler. Roep aan met de Mollie payment ID; de service
    /// haalt de authoritative status op bij Mollie en updatet de lokale Payment +
    /// Enrollment rijen. Wordt zowel door de webhook endpoint als door
    /// <see cref="GetPaymentStatusForEnrollmentAsync"/> (sync-from-mollie) gebruikt.
    /// </summary>
    Task<Result> SyncPaymentFromMollieAsync(
        string molliePaymentId, CancellationToken ct = default);

    /// <summary>
    /// Publieke status-poll voor de thank-you-page. Wanneer <paramref name="syncFromMollie"/>
    /// true is én de payment nog niet in terminale staat zit, doet de service een
    /// extra <c>GET /v2/payments/{id}</c> bij Mollie (vangt het scenario op waar
    /// webhook delivery faalt of vertraagd is — relevant lokaal zonder ngrok).
    /// </summary>
    Task<Result<PaymentStatusDto>> GetPaymentStatusForEnrollmentAsync(
        Guid enrollmentId, bool syncFromMollie, CancellationToken ct = default);

    /// <summary>
    /// Creëert een Mollie payment voor een bestaande kamp-inschrijving. Het bedrag
    /// is <c>camp.Price × aantal deelnemers</c> (groep = leider + leden, solo = 1).
    /// Bij succes returnt een <see cref="CreatePaymentResultDto.CheckoutUrl"/> waar
    /// de FE de deelnemer naartoe redirect.
    /// </summary>
    Task<Result<CreatePaymentResultDto>> CreatePaymentForCampEnrollmentAsync(
        Guid campEnrollmentId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Publieke status-poll voor de kamp-thank-you-page. Mirror van
    /// <see cref="GetPaymentStatusForEnrollmentAsync"/> maar op basis van de
    /// kamp-inschrijving.
    /// </summary>
    Task<Result<PaymentStatusDto>> GetPaymentStatusForCampEnrollmentAsync(
        Guid campEnrollmentId, bool syncFromMollie, CancellationToken ct = default);

    /// <summary>
    /// Legt een Pending cash-betaling vast voor een kamp-inschrijving. Het bedrag is
    /// <c>camp.Price × aantal deelnemers</c> (zelfde regel als de online flow). De
    /// inschrijving blijft op PendingPayment staan; de coach bevestigt de cash later
    /// via <see cref="MarkCampCashPaidAsync"/>.
    /// </summary>
    Task<Result> RecordCampCashPaymentAsync(
        Guid campEnrollmentId, Guid organizationId, CancellationToken ct = default);

    /// <summary>
    /// Markeert de openstaande cash-betaling van een kamp-inschrijving als betaald en
    /// bevestigt de inschrijving(en). Verstuurt de bevestigingsmail. Gebruikt door de
    /// admin/trainer wanneer de deelnemer cash heeft betaald.
    /// </summary>
    Task<Result> MarkCampCashPaidAsync(
        Guid campId, Guid campEnrollmentId, Guid organizationId, CancellationToken ct = default);
}
