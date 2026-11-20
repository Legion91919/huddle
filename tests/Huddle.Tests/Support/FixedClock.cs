namespace Huddle.Tests;

/// <summary>A controllable clock (the testing package for TimeProvider is not used, to keep the test project light).</summary>
internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset Default = new(2026, 11, 19, 8, 30, 0, TimeSpan.Zero);

    private DateTimeOffset _now = now;

    public FixedClock()
        : this(Default)
    {
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}
