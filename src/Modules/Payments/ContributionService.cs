using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;

namespace Huddle.Payments;

/// <summary>Use case: a member contributes to their group. Wallet to pool, one balanced posting, exactly once per key.</summary>
internal sealed class ContributionService(IGroupDirectory groups, ILedger ledger, IdempotencyStore idempotency, PaymentsMetrics metrics) : IContributions
{
    private const int MaxKeyLength = 128;

    public ContributionResult Contribute(ContributeCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var result = Execute(command);
        if (result.Replayed)
        {
            metrics.Replayed();
        }
        else if (result.Succeeded)
        {
            metrics.Posted();
        }
        else
        {
            metrics.Rejected(result.Failure!.Value.ToString());
        }

        return result;
    }

    private ContributionResult Execute(ContributeCommand command)
    {
        if (string.IsNullOrEmpty(command.IdempotencyKey))
        {
            return ContributionResult.Fail(ContributionFailure.IdempotencyKeyMissing, "Send an Idempotency-Key header so a retry can never pay twice.");
        }

        if (command.IdempotencyKey.Length > MaxKeyLength || !command.IdempotencyKey.All(c => c is > ' ' and <= '~'))
        {
            return ContributionResult.Fail(ContributionFailure.IdempotencyKeyInvalid, $"The Idempotency-Key must be 1 to {MaxKeyLength} visible ASCII characters.");
        }

        switch (groups.CheckMembership(command.GroupId, command.MemberId))
        {
            case Membership.GroupNotFound:
                return ContributionResult.Fail(ContributionFailure.GroupNotFound, "That group does not exist.");
            case Membership.NotAMember:
                return ContributionResult.Fail(ContributionFailure.NotAMember, "Only members of this group can contribute to it.");
        }

        var fingerprint = RequestFingerprint.Of(command.GroupId, command.Amount, command.Currency);
        return idempotency.Run(command.MemberId, command.IdempotencyKey, fingerprint, () => Post(command));
    }

    private ContributionResult Post(ContributeCommand command)
    {
        var group = groups.FindGroup(command.GroupId)!;
        var member = group.Members.First(m => m.MemberId == command.MemberId);
        var rules = ContributionRules.Validate(command.Amount, command.Currency, group, member);
        if (rules.Failure is not null)
        {
            return ContributionResult.Fail(rules.Failure.Value, rules.Detail!);
        }

        var amount = rules.Amount!.Value;
        var wallet = LedgerAccounts.MemberWallet(member.MemberId);
        var contributionId = Guid.NewGuid();
        var posting = ledger.Post(new PostingRequest(
            PostingKinds.Contribution,
            contributionId.ToString(),
            member.MemberId,
            [new LedgerLine(wallet, -amount), new LedgerLine(group.PoolAccountId, amount)],
            NoOverdraft: [wallet]));

        if (posting.Error == PostingError.InsufficientFunds)
        {
            return ContributionResult.Fail(ContributionFailure.InsufficientFunds, $"Not enough money in your wallet. {posting.Detail}");
        }

        if (!posting.Succeeded)
        {
            throw new InvalidOperationException($"Ledger refused a contribution posting: {posting.Error} {posting.Detail}");
        }

        var receipt = new ContributionReceipt(contributionId, posting.Posting!.PostingId, group.GroupId, member.MemberId, amount, posting.Posting.RecordedAt);
        return new ContributionResult(receipt, false, null, null);
    }
}
