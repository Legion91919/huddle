using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Huddle.Payments;

/// <summary>A stable hash of what a contribution request asks for, so a retried key can be matched to its original body.</summary>
internal static class RequestFingerprint
{
    public static string Of(string groupId, decimal? amount, string? currency)
    {
        // "G29" drops trailing zeros, so 250 and 250.00 are the same request.
        var canonical = string.Join(
            '\n',
            groupId,
            amount?.ToString("G29", CultureInfo.InvariantCulture) ?? "-",
            currency?.Trim() ?? "-");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
