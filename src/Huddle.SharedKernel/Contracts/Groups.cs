namespace Huddle.SharedKernel.Contracts;

public enum MemberRole
{
    Member,
    Treasurer,
    Chairperson,
}

public sealed record PersonView(string MemberId, string DisplayName);

public sealed record MemberView(string MemberId, string DisplayName, MemberRole Role, Money ContributionLimit, DateOnly JoinedOn);

public sealed record GroupView(
    string GroupId,
    string Name,
    string Currency,
    string Schedule,
    Money MonthlyContribution,
    IReadOnlyList<MemberView> Members)
{
    public string PoolAccountId => LedgerAccounts.GroupPool(GroupId);
}

public enum Membership
{
    GroupNotFound,
    NotAMember,
    Member,
}

/// <summary>Read side of the Groups module.</summary>
public interface IGroupDirectory
{
    PersonView? FindPerson(string memberId);

    GroupView? FindGroup(string groupId);

    Membership CheckMembership(string groupId, string memberId);

    IReadOnlyList<GroupView> GroupsOf(string memberId);
}

/// <summary>Write side of the Groups module (used by onboarding and demo seeding).</summary>
public interface IGroupRegistry
{
    void RegisterPerson(string memberId, string displayName);

    void CreateGroup(string groupId, string name, string currency, string schedule, Money monthlyContribution);

    void AddMember(string groupId, string memberId, MemberRole role, Money contributionLimit, DateOnly joinedOn);
}
