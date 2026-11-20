using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;

namespace Huddle.Statements;

/// <summary>Builds statements purely from ledger postings, so every figure on a statement is provable from entries.</summary>
internal sealed class LedgerStatements(ILedger ledger) : IStatements
{
    public GroupContributionHistory ContributionHistory(string groupId, string currency)
    {
        var pool = LedgerAccounts.GroupPool(groupId);
        if (!ledger.AccountExists(pool))
        {
            return new GroupContributionHistory(groupId, Money.Zero(currency), [], []);
        }

        var postings = ledger.PostingsFor(pool);
        var running = Money.Zero(currency);
        var lines = new List<ContributionLine>();
        foreach (var posting in postings.OrderBy(p => p.OccurredAt).ThenBy(p => p.Sequence))
        {
            var poolAmount = posting.Entries.Where(e => e.AccountId == pool).Aggregate(Money.Zero(currency), (sum, e) => sum + e.Amount);
            running += poolAmount;
            if (posting.Kind == PostingKinds.Contribution)
            {
                lines.Add(new ContributionLine(posting.PostingId, posting.Reference, posting.InitiatedBy, poolAmount, posting.OccurredAt, running));
            }
        }

        var totals = lines.GroupBy(l => l.MemberId, StringComparer.Ordinal)
            .Select(g => new MemberContributionTotal(
                g.Key,
                g.Aggregate(Money.Zero(currency), (sum, l) => sum + l.Amount),
                g.Count(),
                g.Max(l => l.OccurredAt)))
            .OrderBy(t => t.MemberId, StringComparer.Ordinal)
            .ToList();

        lines.Reverse();
        return new GroupContributionHistory(groupId, running, lines, totals);
    }
}
