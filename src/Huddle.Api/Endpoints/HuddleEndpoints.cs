using System.Globalization;
using System.Security.Claims;
using Huddle.Api.Infrastructure;
using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Huddle.Api.Endpoints;

/// <summary>HTTP surface for slice 1. Endpoints compose module contracts; rules live in the modules.</summary>
internal static class HuddleEndpoints
{
    /// <summary>South Africa has no daylight saving: SAST is always UTC+2. Used for "this month".</summary>
    private static readonly TimeSpan Sast = TimeSpan.FromHours(2);

    public static void MapHuddleEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api").RequireAuthorization();

        api.MapGet("/me", (ClaimsPrincipal user, IGroupDirectory directory) =>
        {
            var memberId = MemberId(user);
            var person = directory.FindPerson(memberId)!;
            return TypedResults.Ok(new MeResponse(person.MemberId, person.DisplayName, directory.GroupsOf(memberId).Select(g => new GroupLink(g.GroupId, g.Name)).ToList()));
        });

        api.MapGet("/groups/{groupId}", (string groupId, ClaimsPrincipal user, IGroupDirectory directory, ILedger ledger, IStatements statements, TimeProvider time) =>
        {
            var memberId = MemberId(user);
            var denied = Authorise(directory, groupId, memberId);
            if (denied is not null)
            {
                return denied;
            }

            var group = directory.FindGroup(groupId)!;
            var history = statements.ContributionHistory(groupId, group.Currency);
            var monthStart = MonthStart(time.GetUtcNow());
            var paidThisMonth = history.Lines.Where(l => l.OccurredAt >= monthStart).Select(l => l.MemberId).ToHashSet(StringComparer.Ordinal);
            var collected = history.Lines.Where(l => l.OccurredAt >= monthStart).Aggregate(Money.Zero(group.Currency), (sum, l) => sum + l.Amount);
            var me = group.Members.First(m => m.MemberId == memberId);

            var members = group.Members
                .OrderByDescending(m => m.Role != MemberRole.Member).ThenBy(m => m.DisplayName, StringComparer.Ordinal)
                .Select(m =>
                {
                    var total = history.TotalsByMember.FirstOrDefault(t => t.MemberId == m.MemberId);
                    return new MemberResponse(
                        m.MemberId,
                        m.DisplayName,
                        Initials(m.DisplayName),
                        m.Role,
                        total?.Total ?? Money.Zero(group.Currency),
                        total?.Count ?? 0,
                        total?.LastContributionAt,
                        paidThisMonth.Contains(m.MemberId));
                })
                .ToList();

            return TypedResults.Ok(new GroupResponse(
                group.GroupId,
                group.Name,
                group.Currency,
                group.Schedule,
                group.MonthlyContribution,
                history.Balance,
                history.Lines.Count,
                new PeriodProgress(monthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture), paidThisMonth.Count, group.Members.Count, collected),
                new MyMembership(me.MemberId, me.DisplayName, me.Role, me.ContributionLimit, ledger.Balance(LedgerAccounts.MemberWallet(me.MemberId)), paidThisMonth.Contains(me.MemberId)),
                members));
        });

        api.MapGet("/groups/{groupId}/contributions", (string groupId, int? limit, ClaimsPrincipal user, IGroupDirectory directory, IStatements statements) =>
        {
            var denied = Authorise(directory, groupId, MemberId(user));
            if (denied is not null)
            {
                return denied;
            }

            var group = directory.FindGroup(groupId)!;
            var names = group.Members.ToDictionary(m => m.MemberId, m => m.DisplayName, StringComparer.Ordinal);
            var history = statements.ContributionHistory(groupId, group.Currency);
            var take = Math.Clamp(limit ?? 100, 1, 500);
            var items = history.Lines.Take(take)
                .Select(l => new HistoryItem(l.PostingId, l.Reference, l.MemberId, names.GetValueOrDefault(l.MemberId, "Former member"), l.Amount, l.OccurredAt, l.RunningBalance))
                .ToList();
            return TypedResults.Ok(new HistoryResponse(groupId, history.Balance, history.Lines.Count, items));
        });

        api.MapPost("/groups/{groupId}/contributions", (
            string groupId,
            ContributeRequest body,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            ClaimsPrincipal user,
            HttpContext http,
            IContributions contributions) =>
        {
            var result = contributions.Contribute(new ContributeCommand(groupId, MemberId(user), body.Amount, body.Currency, idempotencyKey));
            if (!result.Succeeded)
            {
                return Problems.For(result.Failure!.Value, result.Detail!);
            }

            if (result.Replayed)
            {
                http.Response.Headers["Idempotency-Replayed"] = "true";
            }

            var r = result.Receipt!;
            return TypedResults.Created(
                $"/api/groups/{groupId}/contributions/{r.ContributionId}",
                new ContributionResponse(r.ContributionId, r.PostingId, r.GroupId, r.MemberId, r.Amount, r.PostedAt));
        });

        api.MapGet("/ledger/integrity", (ILedger ledger) => TypedResults.Ok(ledger.CheckIntegrity()));
    }

    internal static string Initials(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));

    internal static DateTimeOffset MonthStart(DateTimeOffset now)
    {
        var local = now.ToOffset(Sast);
        return new DateTimeOffset(local.Year, local.Month, 1, 0, 0, 0, Sast);
    }

    private static string MemberId(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private static IResult? Authorise(IGroupDirectory directory, string groupId, string memberId) =>
        directory.CheckMembership(groupId, memberId) switch
        {
            Membership.GroupNotFound => Problems.GroupNotFound(),
            Membership.NotAMember => Problems.NotAMember(),
            _ => null,
        };
}
