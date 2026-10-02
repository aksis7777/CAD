using Microsoft.Extensions.DependencyInjection;
using MiniPdm.Modules.Versions.Abstractions;
using MiniPdm.Modules.Versions.Services;

namespace MiniPdm.Modules.Versions.Extensions;

public static class ModuleRegistration
{
    public static IServiceCollection AddVersionsModule(this IServiceCollection services)
    {
        services.AddScoped<IVersionMutationService, VersionMutationService>();
        return services;
    }
}
