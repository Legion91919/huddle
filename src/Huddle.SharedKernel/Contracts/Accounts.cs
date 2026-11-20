namespace Huddle.SharedKernel.Contracts;

public enum AccountKind
{
    Settlement,
    MemberWallet,
    GroupPool,
}

/// <summary>One side of a posting. Positive amounts increase the account, negative amounts decrease it.</summary>
public sealed record LedgerLine(string AccountId, Money Amount);

/// <summary>
/// A request to append one balanced posting. Lines must sum to zero in a single currency.
/// <paramref name="NoOverdraft"/> lists accounts whose balance may not drop below zero; the check and the append are atomic.
/// <paramref name="OccurredAt"/> is the value date (defaults to the recording time); used by imports and seed data.
/// </summary>
public sealed record PostingRequest(
    string Kind,
    string Reference,
    string InitiatedBy,
    IReadOnlyList<LedgerLine> Lines,
    IReadOnlyCollection<string>? NoOverdraft = null,
    DateTimeOffset? OccurredAt = null);

public sealed record LedgerEntryView(long Sequence, Guid PostingId, string AccountId, Money Amount);

public sealed record PostingView(
    Guid PostingId,
    long Sequence,
    string Kind,
    string Reference,
    string InitiatedBy,
    DateTimeOffset OccurredAt,
    DateTimeOffset RecordedAt,
    IReadOnlyList<LedgerEntryView> Entries);

public enum PostingError
{
    TooFewLines,
    ZeroLine,
    UnknownAccount,
    CurrencyMismatch,
    Unbalanced,
    InsufficientFunds,
}

public sealed record PostingResult(PostingView? Posting, PostingError? Error, string? Detail)
{
    public bool Succeeded => Posting is not null;
}

public sealed record LedgerIntegrity(
    int Accounts,
    long Postings,
    long Entries,
    bool EveryPostingSumsToZero,
    bool BalancesEqualSumOfEntries,
    IReadOnlyDictionary<string, decimal> TotalsByCurrency)
{
    public bool Balanced => EveryPostingSumsToZero && BalancesEqualSumOfEntries && TotalsByCurrency.Values.All(t => t == 0m);
}

/// <summary>
/// The double-entry, append-only ledger owned by the Accounts module.
/// There is no stored balance anywhere: a balance is always the sum of an account's entries.
/// There is deliberately no update or delete operation.
/// </summary>
public interface ILedger
{
    void OpenAccount(string accountId, string currency, AccountKind kind);

    bool AccountExists(string accountId);

    PostingResult Post(PostingRequest request);

    Money Balance(string accountId);

    IReadOnlyList<PostingView> PostingsFor(string accountId);

    LedgerIntegrity CheckIntegrity();
}
