using System.Text.Json;
using System.Text.Json.Serialization;
using Huddle.SharedKernel;

namespace Huddle.Api.Infrastructure;

/// <summary>Money on the wire: { "amount": 250.00, "currency": "ZAR" } with exactly two decimals.</summary>
internal sealed class MoneyJsonConverter : JsonConverter<Money>
{
    public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        decimal? amount = null;
        string? currency = null;
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Money must be an object with amount and currency.");
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var name = reader.GetString();
            reader.Read();
            if (string.Equals(name, "amount", StringComparison.OrdinalIgnoreCase))
            {
                amount = reader.GetDecimal();
            }
            else if (string.Equals(name, "currency", StringComparison.OrdinalIgnoreCase))
            {
                currency = reader.GetString();
            }
        }

        if (amount is null || currency is null || !Money.IsValidCurrency(currency) || !Money.TryOf(amount.Value, currency, out var money))
        {
            throw new JsonException("Money needs an amount in whole cents and an ISO currency.");
        }

        return money;
    }

    public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", value.Currency);
        writer.WriteEndObject();
    }
}
