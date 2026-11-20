using System.Diagnostics.Metrics;

namespace Huddle.Payments;

/// <summary>Counters behind the payments SLIs (see .agentic/quality.md). Exported by OpenTelemetry once it is added.</summary>
internal sealed class PaymentsMetrics : IDisposable
{
    public const string MeterName = "Huddle.Payments";

    private readonly Meter _meter = new(MeterName);
    private readonly Counter<long> _posted;
    private readonly Counter<long> _replayed;
    private readonly Counter<long> _rejected;

    public PaymentsMetrics()
    {
        _posted = _meter.CreateCounter<long>("huddle.contributions.posted");
        _replayed = _meter.CreateCounter<long>("huddle.contributions.replayed");
        _rejected = _meter.CreateCounter<long>("huddle.contributions.rejected");
    }

    public void Posted() => _posted.Add(1);

    public void Replayed() => _replayed.Add(1);

    public void Rejected(string reason) => _rejected.Add(1, new KeyValuePair<string, object?>("reason", reason));

    public void Dispose() => _meter.Dispose();
}
