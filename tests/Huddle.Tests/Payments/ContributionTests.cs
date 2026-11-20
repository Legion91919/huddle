using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;
using Xunit;

namespace Huddle.Tests;

/// <summary>Slice-1 acceptance criteria exercised through the module contracts (no HTTP).</summary>
public class ContributionTests
{
    [Fact]
    public void A_contribution_moves_money_from_the_wallet_to_the_group_pool_as_one_balanced_posting()
    {
        using var m = new TestModules().WithCircle();

        var result = m.Contribute(TestModules.Member, 250m, "key-1");

        Assert.True(result.Succeeded, result.Detail);
        var pool = m.Ledger.PostingsFor(LedgerAccounts.GroupPool(TestModules.GroupId));
        var posting = Assert.Single(pool);
        Assert.Equal(2, posting.Entries.Count);
        Assert.Equal(0L, posting.Entries.Sum(e => e.Amount.Cents));
        Assert.Equal(TestModules.Zar(250m), m.Ledger.Balance(LedgerAccounts.GroupPool(TestModules.GroupId)));
        Assert.Equal(TestModules.Zar(4750m), m.Ledger.Balance(LedgerAccounts.MemberWallet(TestModules.Member)));
    }

    [Fact]
    public void Criterion1_same_idempotency_key_twice_gives_one_pair_of_entries_and_the_original_result()
    {
        using var m = new TestModules().WithCircle();

        var first = m.Contribute(TestModules.Member, 300m, "retry-key");
        var second = m.Contribute(TestModules.Member, 300.00m, "retry-key");

        Assert.False(first.Replayed);
        Assert.True(second.Replayed);
        Assert.Equal(first.Receipt, second.Receipt);
        var posting = Assert.Single(m.Ledger.PostingsFor(LedgerAccounts.GroupPool(TestModules.GroupId)));
        Assert.Equal(2, posting.Entries.Count);
    }

    [Fact]
    public void Same_key_with_a_different_body_is_a_conflict_and_posts_nothing()
    {
        using var m = new TestModules().WithCircle();
        m.Contribute(TestModules.Member, 300m, "reused");

        var conflict = m.Contribute(TestModules.Member, 301m, "reused");

        Assert.Equal(ContributionFailure.IdempotencyKeyReused, conflict.Failure);
        Assert.Single(m.Ledger.PostingsFor(LedgerAccounts.GroupPool(TestModules.GroupId)));
    }

    [Fact]
    public void Keys_are_scoped_per_member()
    {
        using var m = new TestModules().WithCircle();

        Assert.True(m.Contribute(TestModules.Member, 100m, "shared").Succeeded);
        Assert.False(m.Contribute(TestModules.Treasurer, 100m, "shared").Replayed);
        Assert.Equal(2, m.Ledger.PostingsFor(LedgerAccounts.GroupPool(TestModules.GroupId)).Count);
    }

    [Fact]
    public void A_rejected_attempt_does_not_burn_the_key()
    {
        using var m = new TestModules().WithCircle();

        Assert.Equal(ContributionFailure.AboveLimit, m.Contribute(TestModules.Member, 900m, "k").Failure);
        Assert.True(m.Contribute(TestModules.Member, 400m, "k").Succeeded);
    }

    [Theory]
    [InlineData(null, ContributionFailure.IdempotencyKeyMissing)]
    [InlineData("", ContributionFailure.IdempotencyKeyMissing)]
    [InlineData("has space", ContributionFailure.IdempotencyKeyInvalid)]
    public void An_idempotency_key_is_required_and_well_formed(string? key, ContributionFailure expected)
    {
        using var m = new TestModules().WithCircle();

        var result = m.Contributions.Contribute(new ContributeCommand(TestModules.GroupId, TestModules.Member, 10m, "ZAR", key));

        Assert.Equal(expected, result.Failure);
    }

    [Fact]
    public void Criterion3_non_members_cannot_contribute_and_unknown_groups_are_reported()
    {
        using var m = new TestModules().WithCircle();

        Assert.Equal(ContributionFailure.NotAMember, m.Contribute(TestModules.Outsider, 10m).Failure);
        Assert.Equal(ContributionFailure.GroupNotFound, m.Contribute(TestModules.Member, 10m, groupId: "g-nope").Failure);
        Assert.Equal(Membership.NotAMember, m.Directory.CheckMembership(TestModules.GroupId, TestModules.Outsider));
        Assert.Empty(m.Ledger.PostingsFor(LedgerAccounts.GroupPool(TestModules.GroupId)));
    }

    [Theory]
    [InlineData(0.0, ContributionFailure.AmountNotPositive)]
    [InlineData(-5.0, ContributionFailure.AmountNotPositive)]
    [InlineData(500.01, ContributionFailure.AboveLimit)]
    public void Criterion4_invalid_amounts_are_rejected_with_a_reason(double amount, ContributionFailure expected)
    {
        using var m = new TestModules().WithCircle();

        var result = m.Contribute(TestModules.Member, (decimal)amount);

        Assert.Equal(expected, result.Failure);
        Assert.False(string.IsNullOrWhiteSpace(result.Detail));
        Assert.Empty(m.Ledger.PostingsFor(LedgerAccounts.GroupPool(TestModules.GroupId)));
    }

    [Fact]
    public void A_contribution_larger_than_the_wallet_is_refused()
    {
        using var m = new TestModules().WithCircle();
        m.AddPerson("m-dee", "Dee Test", 20m);
        m.Registry.AddMember(TestModules.GroupId, "m-dee", MemberRole.Member, TestModules.Zar(1000m), new DateOnly(2026, 1, 1));

        var result = m.Contribute("m-dee", 20.01m);

        Assert.Equal(ContributionFailure.InsufficientFunds, result.Failure);
        Assert.Contains("R20.00", result.Detail);
    }

    [Fact]
    public void Criterion3_history_lists_contributions_newest_first_with_running_balance_and_member_totals()
    {
        var clock = new FixedClock();
        using var m = new TestModules(clock).WithCircle();
        m.Contribute(TestModules.Member, 100m);
        clock.Advance(TimeSpan.FromHours(1));
        m.Contribute(TestModules.Treasurer, 250m);
        clock.Advance(TimeSpan.FromHours(1));
        m.Contribute(TestModules.Member, 50m);

        var history = m.Statements.ContributionHistory(TestModules.GroupId, "ZAR");

        Assert.Equal(TestModules.Zar(400m), history.Balance);
        Assert.Equal(new[] { 50m, 250m, 100m }, history.Lines.Select(l => l.Amount.Amount).ToArray());
        Assert.Equal(new[] { 400m, 350m, 100m }, history.Lines.Select(l => l.RunningBalance.Amount).ToArray());
        var bo = Assert.Single(history.TotalsByMember, t => t.MemberId == TestModules.Member);
        Assert.Equal(TestModules.Zar(150m), bo.Total);
        Assert.Equal(2, bo.Count);
    }

    [Fact]
    public void An_empty_group_has_a_zero_balance_and_no_history()
    {
        using var m = new TestModules().WithCircle();

        var history = m.Statements.ContributionHistory(TestModules.GroupId, "ZAR");

        Assert.Equal(Money.Zero("ZAR"), history.Balance);
        Assert.Empty(history.Lines);
    }
}
