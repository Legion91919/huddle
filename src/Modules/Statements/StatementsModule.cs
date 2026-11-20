using Huddle.SharedKernel.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Huddle.Statements;

/// <summary>Registers the Statements module. Slice 1: a group's contribution history, read from the ledger.</summary>
public static class StatementsModule
{
    public static IServiceCollection AddStatementsModule(this IServiceCollection services)
    {
        services.AddSingleton<IStatements, LedgerStatements>();
        return services;
    }
}
