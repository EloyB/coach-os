using CoachOS.Domain.Models;

namespace CoachOS.Application.Billing;

/// <summary>
/// HTTP-status voor de Mollie-billingwebhook. Kon Mollie zelf niet bereikt worden, dan 503 zodat
/// Mollie het later opnieuw probeert (anders gaat de melding verloren). Alles anders 200: een
/// onbekende of al verwerkte betaling hoeft niet opnieuw.
/// </summary>
public static class BillingWebhookResponses
{
    public static int StatusCodeFor(Result result)
        => !result.IsSuccess && result.Errors.Any(e => e.Code == ErrorCodes.ExternalService) ? 503 : 200;
}
