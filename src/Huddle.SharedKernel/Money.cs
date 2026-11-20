using System.Globalization;

namespace Huddle.SharedKernel;

/// <summary>
/// An exact amount of money: whole cents plus an ISO 4217 currency code.
/// Stored as a 64-bit count of cents so arithmetic is exact; exposed as a decimal with two places.
/// </summary>
public readonly struct Money : IEquatable<Money>, IComparable<Money>
{
    private Money(long cents, string currency)
    {
        Cents = cents;
        Currency = currency;
    }

    /// <summary>Signed amount in minor units (cents).</summary>
    public long Cents { get; }

    /// <summary>ISO 4217 code, for example ZAR.</summary>
    public string Currency { get; }

    /// <summary>The amount as a decimal with exactly two decimal places (scale 2), e.g. 250.00.</summary>
    public decimal Amount
    {
        get
        {
            var abs = Cents == long.MinValue ? (ulong)long.MaxValue + 1 : (ulong)Math.Abs(Cents);
            return new decimal((int)(abs & 0xFFFFFFFF), (int)(abs >> 32), 0, Cents < 0, 2);
        }
    }

    public bool IsZero => Cents == 0;

    public bool IsPositive => Cents > 0;

    public bool IsNegative => Cents < 0;

    public static Money Zero(string currency) => new(0, NormaliseCurrency(currency));

    public static Money FromCents(long cents, string currency) => new(cents, NormaliseCurrency(currency));

    /// <summary>Creates money from a decimal amount. Throws when it has more than two decimal places.</summary>
    public static Money Of(decimal amount, string currency)
    {
        if (!TryOf(amount, currency, out var money))
        {
            throw new ArgumentException($"{amount.ToString(CultureInfo.InvariantCulture)} is not a whole number of cents.", nameof(amount));
        }

        return money;
    }

    /// <summary>Creates money when the amount is a whole number of cents and within range.</summary>
    public static bool TryOf(decimal amount, string currency, out Money money)
    {
        money = default;
        var code = NormaliseCurrency(currency);
        decimal scaled;
        try
        {
            scaled = amount * 100m;
        }
        catch (OverflowException)
        {
            return false;
        }

        if (scaled != decimal.Truncate(scaled) || scaled > long.MaxValue || scaled < long.MinValue)
        {
            return false;
        }

        money = new Money(decimal.ToInt64(scaled), code);
        return true;
    }

    public static bool IsValidCurrency(string? currency) =>
        currency is { Length: 3 } && currency.All(c => c is >= 'A' and <= 'Z');

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(checked(Cents + other.Cents), Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(checked(Cents - other.Cents), Currency);
    }

    public Money Negate() => new(checked(-Cents), Currency);

    public static Money operator +(Money left, Money right) => left.Add(right);

    public static Money operator -(Money left, Money right) => left.Subtract(right);

    public static Money operator -(Money value) => value.Negate();

    public static bool operator ==(Money left, Money right) => left.Equals(right);

    public static bool operator !=(Money left, Money right) => !left.Equals(right);

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public int CompareTo(Money other)
    {
        EnsureSameCurrency(other);
        return Cents.CompareTo(other.Cents);
    }

    public bool Equals(Money other) => Cents == other.Cents && string.Equals(Currency, other.Currency, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is Money other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Cents, Currency);

    /// <summary>Invariant, unambiguous form such as "ZAR 1250.00".</summary>
    public override string ToString() => $"{Currency} {Amount.ToString("0.00", CultureInfo.InvariantCulture)}";

    /// <summary>Display form used in messages, e.g. "R2 000.00" for ZAR.</summary>
    public string ToDisplay()
    {
        var digits = Math.Abs(Amount).ToString("#,##0.00", CultureInfo.InvariantCulture).Replace(',', ' ');
        var sign = Cents < 0 ? "-" : string.Empty;
        return Currency == "ZAR" ? $"{sign}R{digits}" : $"{sign}{Currency} {digits}";
    }

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Cannot combine {Currency} with {other.Currency}.");
        }
    }

    private static string NormaliseCurrency(string currency)
    {
        if (!IsValidCurrency(currency))
        {
            throw new ArgumentException("Currency must be a three-letter upper-case ISO 4217 code.", nameof(currency));
        }

        return currency;
    }
}
