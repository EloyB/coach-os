namespace CoachOS.Domain.Interfaces;

/// <summary>
/// At-rest encryption van gevoelige strings (Mollie OAuth tokens). Implementatie
/// gebruikt ASP.NET Core <c>IDataProtectionProvider</c>. De plaintext verlaat
/// nooit de Application-laag.
/// </summary>
public interface ITokenProtector
{
    string Protect(string plaintext);

    /// <summary>
    /// Ontsleutelt de payload. Werpt <see cref="System.Security.Cryptography.CryptographicException"/>
    /// wanneer de payload corrupt is of versleuteld werd met een sleutel die niet
    /// meer beschikbaar is (bv. keystore-verlies).
    /// </summary>
    string Unprotect(string protectedData);
}
