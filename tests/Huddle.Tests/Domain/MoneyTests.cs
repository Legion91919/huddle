using Huddle.SharedKernel;
using Xunit;

namespace Huddle.Tests;

public class MoneyTests
{
    [Fact]
    public void Of_keeps_exact_cents_and_two_decimal_places()
    {
        var money = Money.Of(1250.5m, "ZAR");

        Assert.Equal(125050L, money.Cents);
        Assert.Equal("1250.50", money.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal("ZAR 1250.50", money.ToString());
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("10.555")]
    public void Of_rejects_fractions_of_a_cent(string amount)
    {
        var value = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture);

        Assert.False(Money.TryOf(value, "ZAR", out _));
        Assert.Throws<ArgumentException>(() => Money.Of(value, "ZAR"));
    }

    [Theory]
    [InlineData("zar")]
    [InlineData("RAND")]
    [InlineData("")]
    public void Currency_must_be_an_iso_code(string currency)
    {
        Assert.Throws<ArgumentException>(() => Money.Of(1m, currency));
    }

    [Fact]
    public void Arithmetic_is_exact_and_refuses_mixed_currencies()
    {
        var a = Money.Of(0.10m, "ZAR");
        var b = Money.Of(0.20m, "ZAR");

        Assert.Equal(Money.Of(0.30m, "ZAR"), a + b);
        Assert.Equal(Money.Of(-0.10m, "ZAR"), a - b);
        Assert.Equal(Money.Of(-0.10m, "ZAR"), -a);
        Assert.True(b > a);
        Assert.Throws<InvalidOperationException>(() => a.Add(Money.Of(1m, "USD")));
    }

    [Fact]
    public void Display_uses_rand_symbol_and_space_grouping()
    {
        Assert.Equal("R2 000.00", Money.Of(2000m, "ZAR").ToDisplay());
        Assert.Equal("-R12.50", Money.Of(-12.5m, "ZAR").ToDisplay());
    }

    [Fact]
    public void Property_adding_random_cents_matches_integer_arithmetic()
    {
        var random = new Random(20261119);
        var total = Money.Zero("ZAR");
        long expected = 0;
        for (var i = 0; i < 5000; i++)
        {
            var cents = random.NextInt64(-1_000_000, 1_000_000);
            total += Money.FromCents(cents, "ZAR");
            expected += cents;
        }

        Assert.Equal(expected, total.Cents);
        Assert.Equal(expected / 100m, total.Amount);
    }
}
