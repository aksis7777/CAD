using Microsoft.Extensions.DependencyInjection;
using MiniPdm.Modules.Objects.Services;

namespace MiniPdm.Modules.Objects.Extensions;

public static class ModuleRegistration
{
    public static IServiceCollection AddObjectsModule(this IServiceCollection services)
    {
        services.AddScoped<ObjectReadService>();
        return services;
    }
}
