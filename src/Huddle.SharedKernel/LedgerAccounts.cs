namespace Huddle.SharedKernel;

/// <summary>Naming convention for ledger account ids, shared so modules agree without referencing each other.</summary>
public static class LedgerAccounts
{
    /// <summary>The cooperative's settlement account at its sponsor bank (money in and out of the system).</summary>
    public const string Settlement = "bank:settlement";

    public static string MemberWallet(string memberId) => $"member:{memberId}:wallet";

    public static string GroupPool(string groupId) => $"group:{groupId}:pool";
}

/// <summary>Kinds of posting the modules agree on.</summary>
public static class PostingKinds
{
    public const string Funding = "funding";

    public const string Contribution = "contribution";
}
