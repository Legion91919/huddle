using System.Net;
using Huddle.Api;
using Huddle.SharedKernel;
using Huddle.SharedKernel.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Huddle.Tests;

/// <summary>Slice-1 acceptance criteria end to end over HTTP against the seeded demo data.</summary>
public class ApiTests
{
    private const string Group = DemoData.MasakhaneGroupId;
    private const string Thandi = DemoData.Thandi;
    private const string Kagiso = DemoData.Kagiso;
    private const string Outsider = DemoData.Bongani;

    [Fact]
    public async Task Health_endpoints_report_live_and_ready()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.Client();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task Requests_without_an_identity_get_401_problem_details()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.Client();

        var response = await client.GetAsync($"/api/groups/{Group}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Criterion3_a_member_sees_the_group_balance_members_and_history()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.Client(Thandi);

        var group = await ApiHost.Json(await client.GetAsync($"/api/groups/{Group}"));
        var history = await ApiHost.Json(await client.GetAsync($"/api/groups/{Group}/contributions"));

        var ledger = host.Services.GetRequiredService<ILedger>();
        var expected = ledger.Balance(LedgerAccounts.GroupPool(Group));
        Assert.Equal(expected.Amount, group.GetProperty("balance").GetProperty("amount").GetDecimal());
        Assert.Equal("ZAR", group.GetProperty("balance").GetProperty("currency").GetString());
        Assert.Equal(6, group.GetProperty("members").GetArrayLength());
        Assert.Equal(Thandi, group.GetProperty("me").GetProperty("memberId").GetString());
        Assert.NotEmpty(history.GetProperty("items").EnumerateArray());
        var sum = history.GetProperty("items").EnumerateArray().Sum(i => i.GetProperty("amount").GetProperty("amount").GetDecimal());
        Assert.Equal(expected.Amount, sum);
    }

    [Fact]
    public async Task Criterion3_non_members_get_403_on_balance_history_and_contribute()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.Client(Outsider);

        var group = await client.GetAsync($"/api/groups/{Group}");
        var history = await client.GetAsync($"/api/groups/{Group}/contributions");
        var post = await client.SendAsync(ApiHost.Contribution(Group, new { amount = 10m, currency = "ZAR" }, "outsider-1"));

        Assert.Equal(HttpStatusCode.Forbidden, group.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, history.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
        Assert.Equal("not-a-member", (await ApiHost.Json(group)).GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Unknown_groups_are_404()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.Client(Thandi);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/groups/g-nowhere")).StatusCode);
    }

    [Fact]
    public async Task Criterion1_posting_twice_with_one_key_returns_the_original_result_and_one_pair_of_entries()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.Client(Thandi);
        var ledger = host.Services.GetRequiredService<ILedger>();
        var before = ledger.PostingsFor(LedgerAccounts.GroupPool(Group)).Count;

        var first = await client.SendAsync(ApiHost.Contribution(Group, new { amount = 500m, currency = "ZAR" }, "api-retry-1"));
        var second = await client.SendAsync(ApiHost.Contribution(Group, new { amount = 500.00m, currency = "ZAR" }, "api-retry-1"));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
        Assert.True(second.Headers.Contains("Idempotency-Replayed"));
        Assert.Equal(before + 1, ledger.PostingsFor(LedgerAccounts.GroupPool(Group)).Count);
        Assert.Equal(2, ledger.PostingsFor(LedgerAccounts.GroupPool(Group))[^1].Entries.Count);
    }

    [Fact]
    public async Task Same_key_with_a_different_body_is_409()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.Client(Thandi);

        await client.SendAsync(ApiHost.Contribution(Group, new { amount = 500m, currency = "ZAR" }, "api-reuse"));
        var conflict = await client.SendAsync(ApiHost.Contribution(Group, new { amount = 600m, currency = "ZAR" }, "api-reuse"));

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("idempotency-key-reused", (await ApiHost.Json(conflict)).GetProperty("reason").GetString());
    }

    [Fact]
    public async Task A_missing_idempotency_key_is_400()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.Client(Thandi);

        var response = await client.SendAsync(ApiHost.Contribution(Group, new { amount = 10m, currency = "ZAR" }, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("idempotency-key-missing", (await ApiHost.Json(response)).GetProperty("reason").GetString());
    }

    [Theory]
    [InlineData("0", "amount-not-positive")]
    [InlineData("-20", "amount-not-positive")]
    [InlineData("750.01", "amount-above-limit")]
    [InlineData("10.001", "amount-precision")]
    public async Task Criterion4_bad_amounts_are_422_problem_details_with_a_reason(string amount, string reason)
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.Client(Kagiso);

        var response = await client.SendAsync(ApiHost.Contribution(
            Group, new { amount = decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), currency = "ZAR" }, $"bad-{amount}"));
        var problem = await ApiHost.Json(response);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(reason, problem.GetProperty("reason").GetString());
        Assert.Equal($"urn:huddle:problem:{reason}", problem.GetProperty("type").GetString());
        Assert.NotEmpty(problem.GetProperty("errors").GetProperty("amount").EnumerateArray());
    }

    [Fact]
    public async Task Criterion2_after_api_traffic_every_balance_is_the_sum_of_entries_and_all_entries_sum_to_zero()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.Client(Thandi);
        await Task.WhenAll(Enumerable.Range(0, 20).Select(i =>
            client.SendAsync(ApiHost.Contribution(Group, new { amount = 10m + i, currency = "ZAR" }, $"par-http-{i}"))));

        var integrity = await ApiHost.Json(await client.GetAsync("/api/ledger/integrity"));
        var history = await ApiHost.Json(await client.GetAsync($"/api/groups/{Group}/contributions"));

        Assert.True(integrity.GetProperty("balanced").GetBoolean());
        Assert.True(integrity.GetProperty("everyPostingSumsToZero").GetBoolean());
        Assert.True(integrity.GetProperty("balancesEqualSumOfEntries").GetBoolean());
        var ledger = host.Services.GetRequiredService<ILedger>();
        Assert.Equal(ledger.Balance(LedgerAccounts.GroupPool(Group)).Amount, history.GetProperty("balance").GetProperty("amount").GetDecimal());
        Assert.Equal(20, history.GetProperty("items").EnumerateArray().Count(i => i.GetProperty("memberId").GetString() == Thandi
            && i.GetProperty("occurredAt").GetDateTimeOffset() == FixedClock.Default));
    }

    [Fact]
    public async Task Me_lists_the_members_groups_oldest_membership_first()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.Client(Thandi);

        var me = await ApiHost.Json(await client.GetAsync("/api/me"));

        Assert.Equal("Thandi Mokoena", me.GetProperty("displayName").GetString());
        var groups = me.GetProperty("groups").EnumerateArray().Select(g => g.GetProperty("groupId").GetString()).ToList();
        Assert.Equal(new[] { Group, DemoData.LetsemaGroupId }, groups.ToArray());
    }
}
