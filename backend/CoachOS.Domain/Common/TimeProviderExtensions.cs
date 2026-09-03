namespace CoachOS.Domain.Common;

/// <summary>
/// Levert "vandaag" consistent in Europe/Brussels-tijd, ongeacht de OS-tijdzone van de host
/// (containers draaien standaard in UTC). Gebruik dit in plaats van
/// <c>DateOnly.FromDateTime(DateTime.UtcNow)</c> overal waar "vandaag" bepaald wordt, anders
/// loopt de dag tussen 22:00/23:00-23:59 UTC (net na middernacht CET/CEST) één dag achter.
/// </summary>
public static class TimeProviderExtensions
{
    public static readonly TimeZoneInfo BrusselsTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");

    public static DateOnly GetBrusselsToday(this TimeProvider timeProvider)
    {
        DateTimeOffset nowBrussels = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), BrusselsTimeZone);
        return DateOnly.FromDateTime(nowBrussels.DateTime);
    }
}
