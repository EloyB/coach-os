namespace CoachOS.Application.Configuration;

public class AppOptions
{
    public const string Section = "App";

    /// <summary>Base URL for student confirmation pages, e.g. "https://app.coachos.be/confirmation".</summary>
    public string ConfirmationBaseUrl { get; set; } = "http://localhost:5317/confirmation";
}
