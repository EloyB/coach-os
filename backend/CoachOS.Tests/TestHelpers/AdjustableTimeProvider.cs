namespace CoachOS.Tests.TestHelpers;

/// <summary>Klok die een test kan vooruitzetten (backoff, verlopen termijnen).</summary>
public sealed class AdjustableTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}
