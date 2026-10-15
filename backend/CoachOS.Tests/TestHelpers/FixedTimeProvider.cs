namespace CoachOS.Tests.TestHelpers;

/// <summary>Vaste klok voor tests die het gedrag rond een specifiek UTC-moment moeten controleren.</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
