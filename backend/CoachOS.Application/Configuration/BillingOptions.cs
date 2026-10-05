namespace CoachOS.Application.Configuration;

/// <summary>Configuratie voor abonnementen. Bind aan de <c>Billing:</c> sectie.</summary>
public class BillingOptions
{
    public const string Section = "Billing";

    /// <summary>
    /// Zet een organisatie na afloop van de proef op enkel-lezen. Staat uit zolang er nog
    /// geen manier is om te betalen: een verlopen proef houdt dan volledige toegang.
    /// </summary>
    public bool EnforceTrialExpiry { get; set; }
}
