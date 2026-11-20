using System.Text.Json;
using System.Text.Json.Serialization;
using Huddle.Accounts;
using Huddle.Api.Endpoints;
using Huddle.Api.Identity;
using Huddle.Api.Infrastructure;
using Huddle.Api.Seeding;
using Huddle.Groups;
using Huddle.Payments;
using Huddle.Statements;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Huddle.Api;

/// <summary>Composition root. Kept as a builder method so tests can host the real app on a free port.</summary>
public static class HuddleApi
{
    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services
            .AddAccountsModule()
            .AddGroupsModule()
            .AddPaymentsModule()
            .AddStatementsModule();

        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier));
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new MoneyJsonConverter());
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

        builder.Services
            .AddAuthentication(DemoMemberAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DemoMemberAuthenticationHandler>(DemoMemberAuthenticationHandler.SchemeName, null);
        builder.Services.AddAuthorization();
        builder.Services.AddHealthChecks().AddCheck<LedgerIntegrityHealthCheck>("ledger", tags: ["ready"]);

        configure?.Invoke(builder);

        var app = builder.Build();

        if (!app.Environment.IsDevelopment() && !app.Configuration.GetValue<bool>("Auth:AllowDemoHeader"))
        {
            throw new InvalidOperationException(
                "Huddle slice 1 only has the development identity header. Configure the real identity provider (backlog) or set Auth:AllowDemoHeader=true for a demo.");
        }

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
        app.MapHuddleEndpoints();

        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Seed:Demo"))
        {
            DemoSeeder.Seed(app.Services);
        }

        return app;
    }
}
