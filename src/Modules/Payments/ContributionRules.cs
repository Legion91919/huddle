using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;

namespace Huddle.Payments;

internal sealed record RuleResult(Money? Amount, ContributionFailure? Failure, string? Detail)
{
    public static RuleResult Ok(Money amount) => new(amount, null, null);

    public static RuleResult Fail(ContributionFailure failure, string detail) => new(null, failure, detail);
}

/// <summary>Domain rules for a single contribution. Pure: no I/O, no clock.</summary>
internal static class ContributionRules
{
    public static RuleResult Validate(decimal? amount, string? currency, GroupView group, MemberView member)
    {
        if (amount is null)
        {
            return RuleResult.Fail(ContributionFailure.AmountRequired, "Enter an amount to contribute.");
        }

        var code = string.IsNullOrWhiteSpace(currency) ? group.Currency : currency;
        if (!string.Equals(code, group.Currency, StringComparison.Ordinal))
        {
            return RuleResult.Fail(ContributionFailure.CurrencyMismatch, $"{group.Name} saves in {group.Currency}.");
        }

        if (amount.Value <= 0m)
        {
            return RuleResult.Fail(ContributionFailure.AmountNotPositive, $"A contribution must be greater than {Money.Zero(group.Currency).ToDisplay()}.");
        }

        if (!Money.TryOf(amount.Value, group.Currency, out var money))
        {
            return RuleResult.Fail(ContributionFailure.AmountPrecision, "Use rand and cents only: at most two decimal places.");
        }

        if (money > member.ContributionLimit)
        {
            return RuleResult.Fail(
                ContributionFailure.AboveLimit,
                $"{money.ToDisplay()} is above your per-contribution limit of {member.ContributionLimit.ToDisplay()}.");
        }

        return RuleResult.Ok(money);
    }
}
