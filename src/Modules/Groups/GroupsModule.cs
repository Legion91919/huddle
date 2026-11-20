using Huddle.SharedKernel.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Huddle.Groups;

/// <summary>Registers the Groups module: people, stokvel groups, roles and per-contribution limits.</summary>
public static class GroupsModule
{
    public static IServiceCollection AddGroupsModule(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryGroupStore>();
        services.AddSingleton<IGroupDirectory>(sp => sp.GetRequiredService<InMemoryGroupStore>());
        services.AddSingleton<IGroupRegistry>(sp => sp.GetRequiredService<InMemoryGroupStore>());
        return services;
    }
}
