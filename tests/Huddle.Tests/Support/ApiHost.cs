using System.Net.Http.Json;
using System.Text.Json;
using Huddle.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Huddle.Tests;

/// <summary>Runs the real API (Kestrel on a free port, Development seed data) for HTTP-level tests.</summary>
internal sealed class ApiHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly Uri _baseAddress;

    private ApiHost(WebApplication app, Uri baseAddress)
    {
        _app = app;
        _baseAddress = baseAddress;
    }

    public IServiceProvider Services => _app.Services;

    public static async Task<ApiHost> StartAsync(TimeProvider? clock = null)
    {
        var app = HuddleApi.Build(
            ["--environment=Development", "--urls=http://127.0.0.1:0", "--Logging:LogLevel:Default=Warning"],
            builder => builder.Services.AddSingleton(clock ?? new FixedClock()));
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new ApiHost(app, new Uri(address));
    }

    public HttpClient Client(string? memberId = null)
    {
        var client = new HttpClient { BaseAddress = _baseAddress };
        if (memberId is not null)
        {
            client.DefaultRequestHeaders.Add("X-Member-Id", memberId);
        }

        return client;
    }

    public static HttpRequestMessage Contribution(string groupId, object body, string? key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/groups/{groupId}/contributions") { Content = JsonContent.Create(body) };
        if (key is not null)
        {
            request.Headers.Add("Idempotency-Key", key);
        }

        return request;
    }

    public static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
