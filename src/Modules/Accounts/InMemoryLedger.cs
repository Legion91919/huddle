using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;

namespace Huddle.Accounts;

/// <summary>
/// Thread-safe, append-only, double-entry ledger kept in memory (persistence arrives in slice 2 behind <see cref="ILedger"/>).
/// <list type="bullet">
/// <item>No balance is stored: <see cref="Balance"/> sums the account's entries every time.</item>
/// <item>A posting is accepted only when its lines sum to zero in one currency.</item>
/// <item>Concurrency: every account has its own lock; a posting takes the locks of all its accounts in ordinal order
/// (so two postings can never deadlock), checks overdraft guards and appends while holding them.</item>
/// </list>
/// </summary>
internal sealed class InMemoryLedger(TimeProvider time) : ILedger
{
    private readonly ConcurrentDictionary<string, AccountStream> _accounts = new(StringComparer.Ordinal);
    private readonly List<PostingView> _journal = [];
    private readonly object _journalGate = new();
    private long _sequence;

    public void OpenAccount(string accountId, string currency, AccountKind kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        var created = new AccountStream(accountId, Money.Zero(currency).Currency, kind);
        var stream = _accounts.GetOrAdd(accountId, created);
        if (!ReferenceEquals(stream, created) && (stream.Currency != currency || stream.Kind != kind))
        {
            throw new InvalidOperationException($"Account {accountId} already exists as {stream.Kind} in {stream.Currency}.");
        }
    }

    public bool AccountExists(string accountId) => _accounts.ContainsKey(accountId);

    public PostingResult Post(PostingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var invalid = Validate(request);
        if (invalid is not null)
        {
            return invalid;
        }

        var streams = request.Lines.Select(l => l.AccountId).Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal).Select(id => _accounts[id]).ToArray();
        var taken = 0;
        try
        {
            foreach (var stream in streams)
            {
                Monitor.Enter(stream.Gate);
                taken++;
            }

            foreach (var guarded in request.NoOverdraft ?? [])
            {
                var stream = streams.FirstOrDefault(s => s.Id == guarded);
                if (stream is null)
                {
                    continue;
                }

                var delta = request.Lines.Where(l => l.AccountId == guarded).Sum(l => l.Amount.Cents);
                var balance = stream.SumOfEntries();
                if (balance + delta < 0)
                {
                    return new PostingResult(
                        null,
                        PostingError.InsufficientFunds,
                        $"Available balance is {Money.FromCents(balance, stream.Currency).ToDisplay()}.");
                }
            }

            var postingId = Guid.NewGuid();
            var sequence = Interlocked.Increment(ref _sequence);
            var recordedAt = time.GetUtcNow();
            var entries = request.Lines.Select(l => new LedgerEntryView(sequence, postingId, l.AccountId, l.Amount)).ToArray();
            var posting = new PostingView(
                postingId,
                sequence,
                request.Kind,
                request.Reference,
                request.InitiatedBy,
                request.OccurredAt ?? recordedAt,
                recordedAt,
                new ReadOnlyCollection<LedgerEntryView>(entries));

            foreach (var stream in streams)
            {
                stream.Append(posting, entries.Where(e => e.AccountId == stream.Id));
            }

            lock (_journalGate)
            {
                _journal.Add(posting);
            }

            return new PostingResult(posting, null, null);
        }
        finally
        {
            for (var i = taken - 1; i >= 0; i--)
            {
                Monitor.Exit(streams[i].Gate);
            }
        }
    }

    public Money Balance(string accountId)
    {
        var stream = Find(accountId);
        lock (stream.Gate)
        {
            return Money.FromCents(stream.SumOfEntries(), stream.Currency);
        }
    }

    public IReadOnlyList<PostingView> PostingsFor(string accountId)
    {
        var stream = Find(accountId);
        lock (stream.Gate)
        {
            return stream.Postings.ToArray();
        }
    }

    public LedgerIntegrity CheckIntegrity()
    {
        // Consistent snapshot: hold every account lock (in the same ordinal order postings use), then read the journal.
        var streams = _accounts.Values.OrderBy(s => s.Id, StringComparer.Ordinal).ToArray();
        var taken = 0;
        try
        {
            foreach (var stream in streams)
            {
                Monitor.Enter(stream.Gate);
                taken++;
            }

            PostingView[] journal;
            lock (_journalGate)
            {
                journal = [.. _journal];
            }

            var everyPostingZero = journal.All(p => p.Entries.Sum(e => e.Amount.Cents) == 0);
            var fromJournal = journal.SelectMany(p => p.Entries)
                .GroupBy(e => e.AccountId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount.Cents), StringComparer.Ordinal);

            var balancesMatch = fromJournal.Keys.All(_accounts.ContainsKey);
            var totals = new Dictionary<string, decimal>(StringComparer.Ordinal);
            foreach (var stream in streams)
            {
                var balance = stream.SumOfEntries();
                balancesMatch &= balance == fromJournal.GetValueOrDefault(stream.Id);
                totals[stream.Currency] = totals.GetValueOrDefault(stream.Currency) + Money.FromCents(balance, stream.Currency).Amount;
            }

            return new LedgerIntegrity(streams.Length, journal.Length, journal.Sum(p => (long)p.Entries.Count), everyPostingZero, balancesMatch, totals);
        }
        finally
        {
            for (var i = taken - 1; i >= 0; i--)
            {
                Monitor.Exit(streams[i].Gate);
            }
        }
    }

    private PostingResult? Validate(PostingRequest request)
    {
        if (request.Lines is null || request.Lines.Count < 2)
        {
            return Reject(PostingError.TooFewLines, "A posting needs at least two lines.");
        }

        foreach (var line in request.Lines)
        {
            if (line.Amount.IsZero)
            {
                return Reject(PostingError.ZeroLine, $"Line for {line.AccountId} has a zero amount.");
            }

            if (!_accounts.TryGetValue(line.AccountId, out var stream))
            {
                return Reject(PostingError.UnknownAccount, $"Account {line.AccountId} does not exist.");
            }

            if (stream.Currency != line.Amount.Currency)
            {
                return Reject(PostingError.CurrencyMismatch, $"Account {line.AccountId} is in {stream.Currency}, not {line.Amount.Currency}.");
            }
        }

        if (request.Lines.Select(l => l.Amount.Currency).Distinct(StringComparer.Ordinal).Count() > 1)
        {
            return Reject(PostingError.CurrencyMismatch, "All lines of a posting must share one currency.");
        }

        if (request.Lines.Sum(l => l.Amount.Cents) != 0)
        {
            return Reject(PostingError.Unbalanced, "Debits and credits must sum to zero.");
        }

        return null;
    }

    private static PostingResult Reject(PostingError error, string detail) => new(null, error, detail);

    private AccountStream Find(string accountId) =>
        _accounts.TryGetValue(accountId, out var stream) ? stream : throw new KeyNotFoundException($"Account {accountId} does not exist.");

    private sealed class AccountStream(string id, string currency, AccountKind kind)
    {
        private readonly List<LedgerEntryView> _entries = [];
        private readonly List<PostingView> _postings = [];

        public string Id { get; } = id;

        public string Currency { get; } = currency;

        public AccountKind Kind { get; } = kind;

        public object Gate { get; } = new();

        public IReadOnlyList<PostingView> Postings => _postings;

        public long SumOfEntries()
        {
            long sum = 0;
            foreach (var entry in _entries)
            {
                sum = checked(sum + entry.Amount.Cents);
            }

            return sum;
        }

        public void Append(PostingView posting, IEnumerable<LedgerEntryView> entries)
        {
            _entries.AddRange(entries);
            _postings.Add(posting);
        }
    }
}
