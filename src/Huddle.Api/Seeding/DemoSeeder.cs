using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;

namespace Huddle.Api.Seeding;

/// <summary>
/// Seeds invented demo data in Development: two stokvel groups, seven people, funded wallets and
/// four months of contribution history, all written as ordinary balanced ledger postings.
/// Dates are relative to the injected clock, so tests and demos are deterministic.
/// </summary>
internal static class DemoSeeder
{
    private static readonly TimeSpan Sast = TimeSpan.FromHours(2);

    private static readonly (string Id, string Name)[] People =
    [
        (DemoData.Thandi, "Thandi Mokoena"),
        (DemoData.Lerato, "Lerato Dube"),
        (DemoData.Sipho, "Sipho Ndlovu"),
        (DemoData.Ayanda, "Ayanda Khumalo"),
        (DemoData.Kagiso, "Kagiso Molefe"),
        (DemoData.Nomsa, "Nomsa Zulu"),
        (DemoData.Bongani, "Bongani Sithole"),
    ];

    /// <summary>Masakhane history: months back (4 = July when now is November) → (member, amount, day, hour).</summary>
    private static readonly (int MonthsBack, string Member, decimal Amount, int Day, int Hour)[] MasakhaneHistory =
    [
        (4, DemoData.Thandi, 500m, 3, 9), (4, DemoData.Lerato, 500m, 2, 18), (4, DemoData.Sipho, 500m, 5, 12),
        (4, DemoData.Ayanda, 500m, 6, 20), (4, DemoData.Kagiso, 250m, 4, 7), (4, DemoData.Kagiso, 250m, 21, 7), (4, DemoData.Nomsa, 500m, 3, 13),
        (3, DemoData.Thandi, 500m, 2, 8), (3, DemoData.Lerato, 500m, 4, 19), (3, DemoData.Sipho, 500m, 7, 11),
        (3, DemoData.Kagiso, 500m, 9, 17), (3, DemoData.Nomsa, 600m, 3, 10),
        (2, DemoData.Thandi, 500m, 1, 9), (2, DemoData.Lerato, 500m, 5, 18), (2, DemoData.Sipho, 500m, 6, 12),
        (2, DemoData.Ayanda, 1000m, 8, 21), (2, DemoData.Kagiso, 500m, 12, 7), (2, DemoData.Nomsa, 500m, 2, 14),
        (1, DemoData.Thandi, 500m, 4, 9), (1, DemoData.Lerato, 500m, 3, 18), (1, DemoData.Sipho, 450m, 10, 12),
        (1, DemoData.Ayanda, 500m, 14, 20), (1, DemoData.Kagiso, 500m, 20, 7), (1, DemoData.Nomsa, 500m, 2, 13),
        (0, DemoData.Lerato, 500m, 2, 18), (0, DemoData.Sipho, 500m, 6, 12), (0, DemoData.Nomsa, 500m, 3, 10),
    ];

    private static readonly (int MonthsBack, string Member, decimal Amount, int Day, int Hour)[] LetsemaHistory =
    [
        (2, DemoData.Bongani, 300m, 5, 9), (2, DemoData.Thandi, 300m, 6, 10), (2, DemoData.Nomsa, 300m, 7, 11),
        (1, DemoData.Bongani, 300m, 4, 9), (1, DemoData.Thandi, 300m, 6, 10), (1, DemoData.Nomsa, 300m, 7, 11),
        (0, DemoData.Bongani, 300m, 5, 9),
    ];

    public static void Seed(IServiceProvider services)
    {
        var ledger = services.GetRequiredService<ILedger>();
        var registry = services.GetRequiredService<IGroupRegistry>();
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();
        var thisMonth = new DateTimeOffset(now.ToOffset(Sast).Year, now.ToOffset(Sast).Month, 1, 0, 0, 0, Sast);

        ledger.OpenAccount(LedgerAccounts.Settlement, "ZAR", AccountKind.Settlement);
        foreach (var (id, name) in People)
        {
            registry.RegisterPerson(id, name);
            ledger.OpenAccount(LedgerAccounts.MemberWallet(id), "ZAR", AccountKind.MemberWallet);
            Post(ledger, PostingKinds.Funding, $"seed-fund-{id}", "seed", thisMonth.AddMonths(-5), LedgerAccounts.Settlement, LedgerAccounts.MemberWallet(id), 8000m);
        }

        registry.CreateGroup(DemoData.MasakhaneGroupId, "Masakhane Savings Circle", "ZAR", "Monthly, by the 25th", Zar(500m));
        ledger.OpenAccount(LedgerAccounts.GroupPool(DemoData.MasakhaneGroupId), "ZAR", AccountKind.GroupPool);
        var joined = DateOnly.FromDateTime(thisMonth.AddMonths(-14).Date);
        registry.AddMember(DemoData.MasakhaneGroupId, DemoData.Thandi, MemberRole.Treasurer, Zar(2000m), joined);
        registry.AddMember(DemoData.MasakhaneGroupId, DemoData.Lerato, MemberRole.Chairperson, Zar(2000m), joined);
        registry.AddMember(DemoData.MasakhaneGroupId, DemoData.Sipho, MemberRole.Member, Zar(1000m), joined.AddMonths(1));
        registry.AddMember(DemoData.MasakhaneGroupId, DemoData.Ayanda, MemberRole.Member, Zar(1000m), joined.AddMonths(2));
        registry.AddMember(DemoData.MasakhaneGroupId, DemoData.Kagiso, MemberRole.Member, Zar(750m), joined.AddMonths(6));
        registry.AddMember(DemoData.MasakhaneGroupId, DemoData.Nomsa, MemberRole.Member, Zar(1000m), joined.AddMonths(3));

        registry.CreateGroup(DemoData.LetsemaGroupId, "Letsema Year-End Fund", "ZAR", "Monthly, by the 7th", Zar(300m));
        ledger.OpenAccount(LedgerAccounts.GroupPool(DemoData.LetsemaGroupId), "ZAR", AccountKind.GroupPool);
        registry.AddMember(DemoData.LetsemaGroupId, DemoData.Bongani, MemberRole.Treasurer, Zar(1500m), joined.AddMonths(8));
        registry.AddMember(DemoData.LetsemaGroupId, DemoData.Thandi, MemberRole.Member, Zar(1000m), joined.AddMonths(8));
        registry.AddMember(DemoData.LetsemaGroupId, DemoData.Nomsa, MemberRole.Member, Zar(1000m), joined.AddMonths(9));

        SeedHistory(ledger, DemoData.MasakhaneGroupId, MasakhaneHistory, thisMonth, now);
        SeedHistory(ledger, DemoData.LetsemaGroupId, LetsemaHistory, thisMonth, now);
    }

    private static void SeedHistory(
        ILedger ledger,
        string groupId,
        (int MonthsBack, string Member, decimal Amount, int Day, int Hour)[] history,
        DateTimeOffset thisMonth,
        DateTimeOffset now)
    {
        var rows = history
            .Select((h, i) => (h.Member, h.Amount, At: thisMonth.AddMonths(-h.MonthsBack).AddDays(h.Day - 1).AddHours(h.Hour).AddMinutes(i * 7 % 60), Index: i))
            .Where(r => r.At < now)
            .OrderBy(r => r.At);
        foreach (var row in rows)
        {
            Post(ledger, PostingKinds.Contribution, $"seed-{groupId}-{row.Index:00}", row.Member, row.At, LedgerAccounts.MemberWallet(row.Member), LedgerAccounts.GroupPool(groupId), row.Amount);
        }
    }

    private static void Post(ILedger ledger, string kind, string reference, string by, DateTimeOffset at, string from, string to, decimal amount)
    {
        var result = ledger.Post(new PostingRequest(kind, reference, by, [new LedgerLine(from, Zar(-amount)), new LedgerLine(to, Zar(amount))], OccurredAt: at));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Seed posting {reference} failed: {result.Detail}");
        }
    }

    private static Money Zar(decimal amount) => Money.Of(amount, "ZAR");
}
