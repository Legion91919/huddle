using Huddle.Accounts;
using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;
using Xunit;

namespace Huddle.Tests;

public class LedgerTests
{
    private static Money Zar(decimal amount) => Money.Of(amount, "ZAR");

    private static InMemoryLedger NewLedger(params string[] accounts)
    {
        var ledger = new InMemoryLedger(new FixedClock());
        foreach (var account in accounts)
        {
            ledger.OpenAccount(account, "ZAR", AccountKind.MemberWallet);
        }

        return ledger;
    }

    private static PostingRequest Request(params (string Account, decimal Amount)[] lines) =>
        new("test", "ref", "tester", lines.Select(l => new LedgerLine(l.Account, Zar(l.Amount))).ToList());

    [Fact]
    public void A_balanced_posting_is_appended_and_balances_are_sums_of_entries()
    {
        var ledger = NewLedger("a", "b");

        var result = ledger.Post(Request(("a", -100m), ("b", 100m)));

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Posting!.Entries.Count);
        Assert.Equal(Zar(-100m), ledger.Balance("a"));
        Assert.Equal(Zar(100m), ledger.Balance("b"));
        Assert.Equal(FixedClock.Default, result.Posting.RecordedAt);
    }

    [Fact]
    public void An_unbalanced_posting_is_rejected_and_nothing_is_written()
    {
        var ledger = NewLedger("a", "b");

        var result = ledger.Post(Request(("a", -100m), ("b", 99.99m)));

        Assert.Equal(PostingError.Unbalanced, result.Error);
        Assert.Empty(ledger.PostingsFor("a"));
        Assert.Equal(0L, ledger.CheckIntegrity().Entries);
    }

    [Fact]
    public void Structural_errors_are_reported_with_a_reason()
    {
        var ledger = NewLedger("a", "b");
        ledger.OpenAccount("usd", "USD", AccountKind.MemberWallet);

        Assert.Equal(PostingError.TooFewLines, ledger.Post(Request(("a", 0m))).Error);
        Assert.Equal(PostingError.ZeroLine, ledger.Post(Request(("a", 0m), ("b", 0m))).Error);
        Assert.Equal(PostingError.UnknownAccount, ledger.Post(Request(("a", -1m), ("nope", 1m))).Error);
        Assert.Equal(
            PostingError.CurrencyMismatch,
            ledger.Post(new PostingRequest("t", "r", "x", [new LedgerLine("a", Zar(-1m)), new LedgerLine("usd", Money.Of(1m, "USD"))])).Error);
    }

    [Fact]
    public void A_no_overdraft_account_cannot_go_below_zero()
    {
        var ledger = NewLedger("wallet", "pool", "bank");
        ledger.Post(Request(("bank", -50m), ("wallet", 50m)));

        var result = ledger.Post(new PostingRequest(
            "contribution", "r", "x", [new LedgerLine("wallet", Zar(-50.01m)), new LedgerLine("pool", Zar(50.01m))], NoOverdraft: ["wallet"]));

        Assert.Equal(PostingError.InsufficientFunds, result.Error);
        Assert.Equal(Zar(50m), ledger.Balance("wallet"));
    }

    [Fact]
    public void Opening_an_account_twice_with_another_currency_is_refused()
    {
        var ledger = NewLedger("a");

        ledger.OpenAccount("a", "ZAR", AccountKind.MemberWallet);
        Assert.Throws<InvalidOperationException>(() => ledger.OpenAccount("a", "USD", AccountKind.MemberWallet));
    }

    [Fact]
    public void The_ledger_contract_has_no_way_to_change_or_remove_history()
    {
        var verbs = new[] { "Update", "Delete", "Remove", "Set", "Edit", "Reverse", "Clear" };
        var methods = typeof(ILedger).GetMethods().Select(m => m.Name).ToList();

        Assert.DoesNotContain(methods, m => verbs.Any(v => m.StartsWith(v, StringComparison.Ordinal)));
        Assert.DoesNotContain(typeof(InMemoryLedger).GetProperties().Select(p => p.Name), n => n.Contains("Balance", StringComparison.Ordinal));
    }
}
