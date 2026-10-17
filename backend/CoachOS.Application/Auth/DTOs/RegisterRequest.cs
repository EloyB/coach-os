namespace CoachOS.Application.Auth.DTOs;

public record RegisterRequest
{
    public string OrganizationName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public bool AcceptTerms { get; init; }
    public bool AcceptDataProcessingAgreement { get; init; }

    /// <summary>
    /// Honeypot: onzichtbaar veld in het formulier. Mensen laten het leeg, bots vullen het in.
    /// </summary>
    public string? Website { get; init; }
}
