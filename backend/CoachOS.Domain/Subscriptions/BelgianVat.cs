namespace CoachOS.Domain.Subscriptions;

/// <summary>Belgische btw voor CoachOS-abonnementen (voorlopig enkel Belgische klanten).</summary>
public static class BelgianVat
{
    public const decimal Rate = 0.21m;

    /// <summary>Bedrag incl. btw, afgerond op cent.</summary>
    public static decimal Gross(decimal net)
        => Math.Round(net * (1 + Rate), 2, MidpointRounding.AwayFromZero);

    /// <summary>Belgisch ondernemingsnummer: 10 cijfers, beginnend met 0 of 1, met of zonder "BE".</summary>
    public static bool IsValidFormat(string vatNumber)
    {
        string digits = Digits(vatNumber);
        return digits.Length == 10 && (digits[0] == '0' || digits[0] == '1');
    }

    /// <summary>Canonieke vorm <c>BE0123456789</c>. Veronderstelt <see cref="IsValidFormat"/>.</summary>
    public static string Normalize(string vatNumber) => "BE" + Digits(vatNumber);

    private static string Digits(string value)
    {
        string trimmed = value.Trim();
        if (trimmed.StartsWith("BE", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[2..];
        if (trimmed.Any(c => !char.IsDigit(c) && c != ' ' && c != '.'))
            return string.Empty;
        return new string(trimmed.Where(char.IsDigit).ToArray());
    }
}
