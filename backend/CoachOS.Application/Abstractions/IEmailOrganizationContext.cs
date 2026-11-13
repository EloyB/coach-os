namespace CoachOS.Application.Abstractions;

/// <summary>
/// Organisatie waartoe de mails in deze scope horen, voor flows zonder ingelogde tenant:
/// publieke links (bevestigen, kampinschrijving) en webhooks. Zonder deze context zou zo'n
/// mail in de outbox zonder organisatie belanden; dan ruimt offboarding hem niet op en
/// vindt "speler wissen" hem enkel nog via het adres, over alle clubs heen.
/// </summary>
/// <remarks>
/// Geldt voor de rest van de scope (het verzoek). Een expliciet gezette organisatie gaat voor
/// op de tenant uit het token. Bij een ingelogde tenant is aanroepen niet nodig.
/// </remarks>
public interface IEmailOrganizationContext
{
    void Use(Guid organizationId);
}
