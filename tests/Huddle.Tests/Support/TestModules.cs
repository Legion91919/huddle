using Huddle.Accounts;
using Huddle.Groups;
using Huddle.Payments;
using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;
using Huddle.Statements;
using Microsoft.Extensions.DependencyInjection;

namespace Huddle.Tests;

/// <summary>Wires the four modules together without HTTP, plus a small invented group for rule and concurrency tests.</summary>
internal sealed class TestModules : IDisposable
{
    public const string GroupId = "g-test-circle";
    public const string Treasurer = "m-ada";
    public const string Member = "m-bo";
    public const string Outsider = "m-cy";

    private readonly ServiceProvider _provider;

    public TestModules(TimeProvider? clock = null)
    {
        _provider = new ServiceCollection()
            .AddSingleton(clock ?? new FixedClock())
            .AddAccountsModule()
            .AddGroupsModule()
            .AddPaymentsModule()
            .AddStatementsModule()
            .BuildServiceProvider();
    }

    public ILedger Ledger => _provider.GetRequiredService<ILedger>();

    public IGroupRegistry Registry => _provider.GetRequiredService<IGroupRegistry>();

    public IGroupDirectory Directory => _provider.GetRequiredService<IGroupDirectory>();

    public IContributions Contributions => _provider.GetRequiredService<IContributions>();

    public IStatements Statements => _provider.GetRequiredService<IStatements>();

    public static Money Zar(decimal amount) => Money.Of(amount, "ZAR");

    /// <summary>One group, two members (limits R2 000 and R500), one outsider; every wallet funded with R5 000.</summary>
    public TestModules WithCircle()
    {
        Ledger.OpenAccount(LedgerAccounts.Settlement, "ZAR", AccountKind.Settlement);
        Registry.CreateGroup(GroupId, "Test Circle", "ZAR", "Monthly", Zar(500));
        Ledger.OpenAccount(LedgerAccounts.GroupPool(GroupId), "ZAR", AccountKind.GroupPool);
        AddPerson(Treasurer, "Ada Test", 5000);
        AddPerson(Member, "Bo Test", 5000);
        AddPerson(Outsider, "Cy Test", 5000);
        Registry.AddMember(GroupId, Treasurer, MemberRole.Treasurer, Zar(2000), new DateOnly(2025, 1, 1));
        Registry.AddMember(GroupId, Member, MemberRole.Member, Zar(500), new DateOnly(2025, 3, 1));
        return this;
    }

    public void AddPerson(string memberId, string name, decimal funding)
    {
        Registry.RegisterPerson(memberId, name);
        Ledger.OpenAccount(LedgerAccounts.MemberWallet(memberId), "ZAR", AccountKind.MemberWallet);
        if (funding > 0)
        {
            var result = Ledger.Post(new PostingRequest(
                "funding",
                $"fund-{memberId}",
                "test",
                [new LedgerLine(LedgerAccounts.Settlement, Zar(-funding)), new LedgerLine(LedgerAccounts.MemberWallet(memberId), Zar(funding))]));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.Detail);
            }
        }
    }

    public ContributionResult Contribute(string memberId, decimal? amount, string? key = null, string? currency = "ZAR", string groupId = GroupId) =>
        Contributions.Contribute(new ContributeCommand(groupId, memberId, amount, currency, key ?? Guid.NewGuid().ToString()));

    public void Dispose() => _provider.Dispose();
}
