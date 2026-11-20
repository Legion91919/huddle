namespace Huddle.SharedKernel.Contracts;

/// <summary>A member's request to move money from their wallet into a group's pool.</summary>
public sealed record ContributeCommand(string GroupId, string MemberId, decimal? Amount, string? Currency, string? IdempotencyKey);

public sealed record ContributionReceipt(
    Guid ContributionId,
    Guid PostingId,
    string GroupId,
    string MemberId,
    Money Amount,
    DateTimeOffset PostedAt);

public enum ContributionFailure
{
    IdempotencyKeyMissing,
    IdempotencyKeyInvalid,
    IdempotencyKeyReused,
    GroupNotFound,
    NotAMember,
    AmountRequired,
    AmountNotPositive,
    AmountPrecision,
    CurrencyMismatch,
    AboveLimit,
    InsufficientFunds,
}

public sealed record ContributionResult(ContributionReceipt? Receipt, bool Replayed, ContributionFailure? Failure, string? Detail)
{
    public bool Succeeded => Receipt is not null;

    public static ContributionResult Fail(ContributionFailure failure, string detail) => new(null, false, failure, detail);
}

public interface IContributions
{
    ContributionResult Contribute(ContributeCommand command);
}
