using Microsoft.Extensions.DependencyInjection;
using MiniPdm.Modules.Composition.Services;

namespace MiniPdm.Modules.Composition.Extensions;

public static class ModuleRegistration
{
    public static IServiceCollection AddCompositionModule(this IServiceCollection services)
    {
        services.AddScoped<CompositionReadService>();
        services.AddScoped<VersionCompositionReadService>();
        return services;
    }
}
