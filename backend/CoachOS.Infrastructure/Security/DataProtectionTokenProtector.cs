using CoachOS.Domain.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace CoachOS.Infrastructure.Security;

/// <summary>
/// <see cref="ITokenProtector"/>-implementatie bovenop ASP.NET Core DataProtection.
/// Sleutels persisteren naar de filesystem zodat ze container-restarts overleven
/// (zie <c>DependencyInjection</c> voor de path-configuratie).
/// </summary>
public class DataProtectionTokenProtector(IDataProtectionProvider provider) : ITokenProtector
{
    /// <summary>
    /// Vaste purpose-string. Een latere wijziging maakt bestaande tokens onleesbaar,
    /// dus enkel bumpen samen met een token-rotatie of migratiestrategie.
    /// </summary>
    private readonly IDataProtector _protector = provider.CreateProtector("CoachOS.Mollie.OAuth.v1");

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedData) => _protector.Unprotect(protectedData);
}
