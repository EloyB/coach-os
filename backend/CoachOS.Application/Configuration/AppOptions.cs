namespace CoachOS.Application.Configuration;

public class AppOptions
{
    public const string Section = "App";

    /// <summary>Base URL of the frontend app, e.g. "https://app.coachos.be".</summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5317";

    /// <summary>Base URL for student confirmation pages, e.g. "https://app.coachos.be/confirmation".</summary>
    public string ConfirmationBaseUrl { get; set; } = "http://localhost:5317/confirmation";

    /// <summary>Base URL for student magic-link redemption, e.g. "https://app.coachos.be/auth/magic".</summary>
    public string StudentMagicLinkBaseUrl { get; set; } = "http://localhost:5317/auth/magic";

    /// <summary>Base URL for the public standalone-lesson invitation page, e.g. "https://app.coachos.be/invitation".</summary>
    public string StandaloneLessonInvitationBaseUrl { get; set; } = "http://localhost:5317/invitation";
}
