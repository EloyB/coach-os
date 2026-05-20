namespace CoachOS.Domain.Models;

public static class ErrorCodes
{
    public const string Validation = "validation";
    public const string NotFound = "not_found";
    public const string Unauthorized = "unauthorized";
    public const string Forbidden = "forbidden";
    public const string Conflict = "conflict";
    public const string Unexpected = "unexpected";

    /// <summary>
    /// Externe afhankelijkheid (third-party API) faalde of antwoordde onverwacht.
    /// Mappet op HTTP 502 Bad Gateway in <c>ResultExtensions</c>.
    /// </summary>
    public const string ExternalService = "external_service";
}
