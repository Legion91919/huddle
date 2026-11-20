using Huddle.Accounts;
using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;
using Xunit;

namespace Huddle.Tests;

/// <summary>
/// Property-style checks of the two ledger invariants over thousands of random postings with fixed seeds,
/// so any failure is reproducible: (1) every posting's entries sum to zero; (2) every balance equals the sum of its entries.
/// </summary>
public class LedgerInvariantPropertyTests
{
    private const int Accounts = 12;

    [Theory]
    [InlineData(20261119, 2000)]
    [InlineData(7, 1500)]
    [InlineData(424242, 1500)]
    public void Random_balanced_postings_keep_both_invariants(int seed, int postings)
    {
        var random = new Random(seed);
        var ledger = new InMemoryLedger(new FixedClock());
        var ids = Enumerable.Range(0, Accounts).Select(i => $"acct-{i:00}").ToArray();
        foreach (var id in ids)
        {
            ledger.OpenAccount(id, "ZAR", AccountKind.MemberWallet);
        }

        var model = ids.ToDictionary(id => id, _ => 0L);

        for (var p = 0; p < postings; p++)
        {
            var lines = RandomBalancedLines(random, ids);
            var result = ledger.Post(new PostingRequest("prop", $"p{p}", "property", lines));

            Assert.True(result.Succeeded, result.Detail);
            Assert.Equal(0L, result.Posting!.Entries.Sum(e => e.Amount.Cents));
            foreach (var line in lines)
            {
                model[line.AccountId] += line.Amount.Cents;
            }
        }

        AssertInvariants(ledger, ids, model, postings);
    }

    [Theory]
    [InlineData(99, 1000)]
    [InlineData(2026, 1000)]
    public void Random_unbalanced_postings_are_always_rejected_and_change_nothing(int seed, int attempts)
    {
        var random = new Random(seed);
        var ledger = new InMemoryLedger(new FixedClock());
        var ids = Enumerable.Range(0, Accounts).Select(i => $"acct-{i:00}").ToArray();
        foreach (var id in ids)
        {
            ledger.OpenAccount(id, "ZAR", AccountKind.MemberWallet);
        }

        var model = ids.ToDictionary(id => id, _ => 0L);
        var accepted = 0;
        for (var a = 0; a < attempts; a++)
        {
            var lines = RandomBalancedLines(random, ids).ToList();
            var skew = random.NextInt64(1, 500);
            var last = lines[^1];
            var broken = random.Next(2) == 0;
            if (broken)
            {
                lines[^1] = last with { Amount = Money.FromCents(last.Amount.Cents + skew, "ZAR") };
            }

            var result = ledger.Post(new PostingRequest("prop", $"u{a}", "property", lines));
            if (broken && lines[^1].Amount.Cents != 0)
            {
                Assert.Equal(PostingError.Unbalanced, result.Error);
            }
            else if (result.Succeeded)
            {
                accepted++;
                foreach (var line in lines)
                {
                    model[line.AccountId] += line.Amount.Cents;
                }
            }
        }

        AssertInvariants(ledger, ids, model, accepted);
    }

    private static List<LedgerLine> RandomBalancedLines(Random random, string[] ids)
    {
        var count = random.Next(2, 6);
        var chosen = ids.OrderBy(_ => random.Next()).Take(count).ToArray();
        var lines = new List<LedgerLine>();
        long sum = 0;
        for (var i = 0; i < count - 1; i++)
        {
            long cents;
            do
            {
                cents = random.NextInt64(-2_500_000, 2_500_000);
            }
            while (cents == 0 || (i == count - 2 && cents == -sum));

            sum += cents;
            lines.Add(new LedgerLine(chosen[i], Money.FromCents(cents, "ZAR")));
        }

        lines.Add(new LedgerLine(chosen[^1], Money.FromCents(-sum, "ZAR")));
        return lines;
    }

    private static void AssertInvariants(InMemoryLedger ledger, string[] ids, Dictionary<string, long> model, int expectedPostings)
    {
        var allPostings = ids.SelectMany(ledger.PostingsFor).DistinctBy(p => p.PostingId).ToList();
        Assert.Equal(expectedPostings, allPostings.Count);
        Assert.All(allPostings, p => Assert.Equal(0L, p.Entries.Sum(e => e.Amount.Cents)));

        foreach (var id in ids)
        {
            var sumOfEntries = ledger.PostingsFor(id).SelectMany(p => p.Entries).Where(e => e.AccountId == id).Sum(e => e.Amount.Cents);
            Assert.Equal(sumOfEntries, ledger.Balance(id).Cents);
            Assert.Equal(model[id], ledger.Balance(id).Cents);
        }

        Assert.Equal(0L, ids.Sum(id => ledger.Balance(id).Cents));
        var integrity = ledger.CheckIntegrity();
        Assert.True(integrity.Balanced);
        Assert.Equal((long)expectedPostings, integrity.Postings);
    }
}
