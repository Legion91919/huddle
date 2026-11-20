using System.Collections.Concurrent;
using Huddle.SharedKernel.Contracts;

namespace Huddle.Payments;

/// <summary>
/// Remembers successful contributions by (member, Idempotency-Key) together with a fingerprint of the request body.
/// Same key + same body returns the original receipt; same key + different body is a conflict.
/// Concurrent requests with one key are serialised on that key, so only one of them can post.
/// Rejected attempts are not remembered, so a key can be reused after fixing the problem.
/// Slice 2 persists keys in the same transaction as the ledger posting and expires them after 24 hours.
/// </summary>
internal sealed class IdempotencyStore
{
    private readonly ConcurrentDictionary<string, Slot> _slots = new(StringComparer.Ordinal);

    public ContributionResult Run(string scope, string key, string fingerprint, Func<ContributionResult> work)
    {
        var id = scope + "\n" + key;
        while (true)
        {
            var slot = _slots.GetOrAdd(id, _ => new Slot(fingerprint));
            lock (slot.Gate)
            {
                if (slot.Abandoned)
                {
                    continue;
                }

                if (!string.Equals(slot.Fingerprint, fingerprint, StringComparison.Ordinal))
                {
                    return ContributionResult.Fail(
                        ContributionFailure.IdempotencyKeyReused,
                        "This Idempotency-Key was already used for a different contribution. Use a new key for a new contribution.");
                }

                if (slot.Receipt is not null)
                {
                    return new ContributionResult(slot.Receipt, true, null, null);
                }

                var result = work();
                if (result.Receipt is not null)
                {
                    slot.Receipt = result.Receipt;
                    return result;
                }

                slot.Abandoned = true;
                _slots.TryRemove(new KeyValuePair<string, Slot>(id, slot));
                return result;
            }
        }
    }

    private sealed class Slot(string fingerprint)
    {
        public string Fingerprint { get; } = fingerprint;

        public object Gate { get; } = new();

        public ContributionReceipt? Receipt { get; set; }

        public bool Abandoned { get; set; }
    }
}
