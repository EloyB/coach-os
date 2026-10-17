using System.Security.Cryptography;
using System.Text;

namespace CoachOS.Application.Auth;

/// <summary>
/// Cijfercodes waarmee een gebruiker zijn e-mailadres bevestigt. De code zelf wordt nooit
/// opgeslagen — enkel een hash die aan de gebruiker gebonden is.
/// </summary>
public static class EmailVerificationCodes
{
    public const int Length = 6;
    public const int MaxAttempts = 5;

    /// <summary>
    /// Maximaal aantal codes dat één account ooit kan aanvragen. Begrenst zowel het totale
    /// aantal gokken (MaxSends × MaxAttempts) als het aantal mails naar een adres dat
    /// mogelijk niet van de aanvrager is.
    /// </summary>
    public const int MaxSends = 10;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    public static string Generate()
    {
        int value = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, Length));
        return value.ToString().PadLeft(Length, '0');
    }

    public static string Hash(Guid userId, string code)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId:N}:{code}"));
        return Convert.ToHexStringLower(bytes);
    }

    public static bool Matches(string storedHash, Guid userId, string code)
    {
        byte[] expected = Encoding.UTF8.GetBytes(storedHash);
        byte[] actual = Encoding.UTF8.GetBytes(Hash(userId, code));
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
