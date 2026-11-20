using Huddle.SharedKernel.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Huddle.Payments;

/// <summary>Registers the Payments module: idempotent, validated contributions from a member wallet to a group pool.</summary>
public static class PaymentsModule
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IdempotencyStore>();
        services.AddSingleton<PaymentsMetrics>();
        services.AddSingleton<IContributions, ContributionService>();
        return services;
    }
}
