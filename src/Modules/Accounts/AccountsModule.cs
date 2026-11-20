using Huddle.SharedKernel.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Huddle.Accounts;

/// <summary>Registers the Accounts module: the double-entry, append-only ledger.</summary>
public static class AccountsModule
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ILedger, InMemoryLedger>();
        return services;
    }
}
