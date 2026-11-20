namespace Huddle.SharedKernel.Contracts;

public sealed record ContributionLine(
    Guid PostingId,
    string Reference,
    string MemberId,
    Money Amount,
    DateTimeOffset OccurredAt,
    Money RunningBalance);

public sealed record MemberContributionTotal(string MemberId, Money Total, int Count, DateTimeOffset? LastContributionAt);

/// <summary>Contribution history of one group, newest first, derived from ledger postings on its pool account.</summary>
public sealed record GroupContributionHistory(
    string GroupId,
    Money Balance,
    IReadOnlyList<ContributionLine> Lines,
    IReadOnlyList<MemberContributionTotal> TotalsByMember);

public interface IStatements
{
    GroupContributionHistory ContributionHistory(string groupId, string currency);
}
