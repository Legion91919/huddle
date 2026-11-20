using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;

namespace Huddle.Api.Endpoints;

internal sealed record ContributeRequest(decimal? Amount, string? Currency);

internal sealed record ContributionResponse(Guid ContributionId, Guid PostingId, string GroupId, string MemberId, Money Amount, DateTimeOffset PostedAt);

internal sealed record MeResponse(string MemberId, string DisplayName, IReadOnlyList<GroupLink> Groups);

internal sealed record GroupLink(string GroupId, string Name);

internal sealed record GroupResponse(
    string GroupId,
    string Name,
    string Currency,
    string Schedule,
    Money MonthlyContribution,
    Money Balance,
    int ContributionCount,
    PeriodProgress ThisMonth,
    MyMembership Me,
    IReadOnlyList<MemberResponse> Members);

internal sealed record PeriodProgress(string Label, int MembersPaid, int MembersTotal, Money Collected);

internal sealed record MyMembership(string MemberId, string DisplayName, MemberRole Role, Money ContributionLimit, Money WalletBalance, bool PaidThisMonth);

internal sealed record MemberResponse(
    string MemberId,
    string DisplayName,
    string Initials,
    MemberRole Role,
    Money TotalContributed,
    int ContributionCount,
    DateTimeOffset? LastContributionAt,
    bool PaidThisMonth);

internal sealed record HistoryResponse(string GroupId, Money Balance, int Total, IReadOnlyList<HistoryItem> Items);

internal sealed record HistoryItem(Guid PostingId, string Reference, string MemberId, string MemberName, Money Amount, DateTimeOffset OccurredAt, Money RunningBalance);
