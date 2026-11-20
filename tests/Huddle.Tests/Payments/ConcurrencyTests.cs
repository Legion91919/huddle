using System.Collections.Concurrent;
using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;
using Xunit;

namespace Huddle.Tests;

/// <summary>Parallel contributions must never lose an entry, double-post a key or overdraw a wallet.</summary>
public class ConcurrencyTests
{
    [Fact]
    public async Task Parallel_contributions_from_many_members_add_up_exactly()
    {
        using var m = new TestModules().WithCircle();
        var members = new[] { TestModules.Treasurer, TestModules.Member };
        var amounts = new ConcurrentBag<(string Member, long Cents)>();

        var tasks = Enumerable.Range(0, 200).Select(i => Task.Run(() =>
        {
            var member = members[i % 2];
            var cents = 100L + (i * 37 % 900);
            var result = m.Contribute(member, cents / 100m, $"par-{i}");
            Assert.True(result.Succeeded, result.Detail);
            amounts.Add((member, cents));
        }));
        await Task.WhenAll(tasks);

        var pool = LedgerAccounts.GroupPool(TestModules.GroupId);
        Assert.Equal(200, m.Ledger.PostingsFor(pool).Count);
        Assert.Equal(amounts.Sum(a => a.Cents), m.Ledger.Balance(pool).Cents);
        foreach (var member in members)
        {
            var spent = amounts.Where(a => a.Member == member).Sum(a => a.Cents);
            Assert.Equal(500_000L - spent, m.Ledger.Balance(LedgerAccounts.MemberWallet(member)).Cents);
        }

        Assert.True(m.Ledger.CheckIntegrity().Balanced);
    }

    [Fact]
    public async Task Parallel_contributions_cannot_overdraw_a_wallet()
    {
        using var m = new TestModules().WithCircle();
        m.AddPerson("m-eve", "Eve Test", 1000m);
        m.Registry.AddMember(TestModules.GroupId, "m-eve", MemberRole.Member, TestModules.Zar(500m), new DateOnly(2026, 1, 1));

        var results = await Task.WhenAll(Enumerable.Range(0, 50).Select(i => Task.Run(() => m.Contribute("m-eve", 30m, $"eve-{i}"))));

        Assert.Equal(33, results.Count(r => r.Succeeded));
        Assert.Equal(17, results.Count(r => r.Failure == ContributionFailure.InsufficientFunds));
        Assert.Equal(TestModules.Zar(10m), m.Ledger.Balance(LedgerAccounts.MemberWallet("m-eve")));
        Assert.Equal(TestModules.Zar(990m), m.Ledger.Balance(LedgerAccounts.GroupPool(TestModules.GroupId)));
        Assert.True(m.Ledger.CheckIntegrity().Balanced);
    }

    [Fact]
    public async Task Parallel_retries_with_one_key_post_exactly_once()
    {
        using var m = new TestModules().WithCircle();

        var results = await Task.WhenAll(Enumerable.Range(0, 40).Select(_ => Task.Run(() => m.Contribute(TestModules.Member, 75m, "same-key"))));

        Assert.All(results, r => Assert.True(r.Succeeded));
        Assert.Single(results.Select(r => r.Receipt!.ContributionId).Distinct());
        Assert.Single(results, r => !r.Replayed);
        Assert.Single(m.Ledger.PostingsFor(LedgerAccounts.GroupPool(TestModules.GroupId)));
        Assert.Equal(TestModules.Zar(75m), m.Ledger.Balance(LedgerAccounts.GroupPool(TestModules.GroupId)));
    }
}
