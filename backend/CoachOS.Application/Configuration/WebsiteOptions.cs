namespace CoachOS.Application.Configuration;

/// <summary>Bind aan de <c>Website:</c> sectie: instellingen voor de publieke marketingsite.</summary>
public class WebsiteOptions
{
    public const string Section = "Website";

    /// <summary>Adres dat een melding krijgt bij elk nieuw contactbericht. Leeg = geen melding.</summary>
    public string ContactNotificationEmail { get; set; } = "info@coach-os.be";
}
