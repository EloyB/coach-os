using CoachOS.Domain.Enums;

namespace CoachOS.Application.Export;

/// <summary>Gedeelde Nederlandse labels + bestandsnaam-helper voor de Excel-exports.</summary>
internal static class ExportLabels
{
    public static string DutchDay(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => "Maandag",
        DayOfWeek.Tuesday => "Dinsdag",
        DayOfWeek.Wednesday => "Woensdag",
        DayOfWeek.Thursday => "Donderdag",
        DayOfWeek.Friday => "Vrijdag",
        DayOfWeek.Saturday => "Zaterdag",
        DayOfWeek.Sunday => "Zondag",
        _ => d.ToString(),
    };

    public static string EnrollmentStatus(EnrollmentStatus s) => s switch
    {
        Domain.Enums.EnrollmentStatus.Pending => "In afwachting",
        Domain.Enums.EnrollmentStatus.Confirmed => "Bevestigd",
        Domain.Enums.EnrollmentStatus.Cancelled => "Geannuleerd",
        Domain.Enums.EnrollmentStatus.Waitlisted => "Wachtlijst",
        Domain.Enums.EnrollmentStatus.PendingPayment => "Wacht op betaling",
        _ => s.ToString(),
    };

    public static string PaymentStatus(PaymentStatus s) => s switch
    {
        Domain.Enums.PaymentStatus.Pending => "In afwachting",
        Domain.Enums.PaymentStatus.Paid => "Betaald",
        Domain.Enums.PaymentStatus.Failed => "Mislukt",
        Domain.Enums.PaymentStatus.Refunded => "Terugbetaald",
        _ => s.ToString(),
    };

    public static string PaymentMethod(PaymentMethod m) => m switch
    {
        Domain.Enums.PaymentMethod.Online => "Online",
        Domain.Enums.PaymentMethod.Cash => "Cash",
        Domain.Enums.PaymentMethod.Transfer => "Overschrijving",
        _ => m.ToString(),
    };

    public static string FileName(string name, string suffix, string fallback, DateOnly today)
    {
        string safe = new(name
            .Select(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' ? c : '-')
            .ToArray());
        safe = safe.Trim();
        if (string.IsNullOrWhiteSpace(safe))
            safe = fallback;

        return $"{safe}-{suffix}-{today:yyyyMMdd}.xlsx";
    }
}
