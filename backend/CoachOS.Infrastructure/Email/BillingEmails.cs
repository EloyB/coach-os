using System.Globalization;

namespace CoachOS.Infrastructure.Email;

/// <summary>Onderwerp, titel, tekst en knop van een abonnementsmail (template <c>billing-notice</c>).</summary>
public sealed record BillingEmailContent(string Subject, string Heading, string Body, string ButtonLabel);

/// <summary>
/// Teksten van de abonnementsmails. Puur en apart van <see cref="EmailService"/> zodat bedragen
/// (<c>€ 1.076,90</c>) en datums (<c>dd/MM/yyyy</c>) testbaar zijn zonder SMTP.
/// </summary>
public static class BillingEmails
{
    private static readonly CultureInfo Belgian = CultureInfo.GetCultureInfo("nl-BE");

    public static string Amount(decimal amount) => $"€ {amount.ToString("N2", Belgian)}";

    private static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static BillingEmailContent SubscriptionStarted(
        string planName, string intervalLabel, decimal amountInclVat, DateOnly periodStart,
        bool bankTransfer, DateOnly? dueDate)
    {
        string payment = bankTransfer
            ? $"Je ontvangt een factuur van {Amount(amountInclVat)} (incl. btw), te betalen per overschrijving voor {Date(dueDate ?? periodStart)}."
            : $"We ontvingen je betaling van {Amount(amountInclVat)} (incl. btw). Je abonnement wordt elke {intervalLabel} automatisch verlengd; opzeggen kan altijd in je instellingen.";

        return new BillingEmailContent(
            $"Je CoachOS {planName}-abonnement is actief",
            $"Welkom bij CoachOS {planName}",
            $"Je {planName}-abonnement (per {intervalLabel}) gaat in op {Date(periodStart)}. {payment}",
            "Naar je abonnement");
    }

    public static BillingEmailContent PaymentFailed(decimal amountInclVat) => new(
        "Je betaling voor CoachOS is niet gelukt",
        "Je betaling is niet gelukt",
        $"De automatische betaling van {Amount(amountInclVat)} (incl. btw) voor je CoachOS-abonnement is niet gelukt. Je kan gewoon verder werken; betaal via de knop hieronder om je abonnement in orde te brengen.",
        "Nu betalen");

    public static BillingEmailContent Invitation() => new(
        "Kies je CoachOS-abonnement",
        "Kies je abonnement",
        "Bedankt dat je CoachOS al gebruikt. Je kan vanaf nu zelf een abonnement kiezen: per maand of per jaar, automatisch via Bancontact of kaart, of met een jaarfactuur per overschrijving.",
        "Abonnement kiezen");
}
