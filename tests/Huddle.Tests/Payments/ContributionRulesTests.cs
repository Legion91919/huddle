using Huddle.Payments;
using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;
using Xunit;

namespace Huddle.Tests;

public class ContributionRulesTests
{
    private static readonly GroupView Group = new("g", "G", "ZAR", "Monthly", Money.Of(500m, "ZAR"), []);
    private static readonly MemberView Member = new("m", "M", MemberRole.Member, Money.Of(1000m, "ZAR"), new DateOnly(2025, 1, 1));

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-250")]
    public void Amount_must_be_greater_than_zero(string amount)
    {
        var result = ContributionRules.Validate(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), "ZAR", Group, Member);

        Assert.Equal(ContributionFailure.AmountNotPositive, result.Failure);
        Assert.Contains("greater than R0.00", result.Detail);
    }

    [Fact]
    public void Amount_is_required()
    {
        Assert.Equal(ContributionFailure.AmountRequired, ContributionRules.Validate(null, "ZAR", Group, Member).Failure);
    }

    [Fact]
    public void Amount_above_the_members_limit_is_rejected_with_the_limit_in_the_reason()
    {
        var result = ContributionRules.Validate(1000.01m, "ZAR", Group, Member);

        Assert.Equal(ContributionFailure.AboveLimit, result.Failure);
        Assert.Contains("R1 000.00", result.Detail);
    }

    [Fact]
    public void Amount_exactly_at_the_limit_is_allowed()
    {
        var result = ContributionRules.Validate(1000m, "ZAR", Group, Member);

        Assert.Null(result.Failure);
        Assert.Equal(Money.Of(1000m, "ZAR"), result.Amount);
    }

    [Fact]
    public void Fractions_of_a_cent_and_other_currencies_are_rejected()
    {
        Assert.Equal(ContributionFailure.AmountPrecision, ContributionRules.Validate(10.005m, "ZAR", Group, Member).Failure);
        Assert.Equal(ContributionFailure.CurrencyMismatch, ContributionRules.Validate(10m, "USD", Group, Member).Failure);
        Assert.Equal(ContributionFailure.CurrencyMismatch, ContributionRules.Validate(10m, "rand", Group, Member).Failure);
    }

    [Fact]
    public void Missing_currency_defaults_to_the_groups_currency()
    {
        Assert.Equal(Money.Of(10m, "ZAR"), ContributionRules.Validate(10m, null, Group, Member).Amount);
    }

    [Fact]
    public void Fingerprint_ignores_trailing_zeros_but_not_the_amount_or_group()
    {
        Assert.Equal(RequestFingerprint.Of("g", 250m, "ZAR"), RequestFingerprint.Of("g", 250.00m, "ZAR"));
        Assert.False(RequestFingerprint.Of("g", 250m, "ZAR") == RequestFingerprint.Of("g", 250.01m, "ZAR"));
        Assert.False(RequestFingerprint.Of("g", 250m, "ZAR") == RequestFingerprint.Of("h", 250m, "ZAR"));
    }
}
