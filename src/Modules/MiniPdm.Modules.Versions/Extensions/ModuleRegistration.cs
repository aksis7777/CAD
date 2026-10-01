using Microsoft.Extensions.DependencyInjection;

namespace MiniPdm.Modules.Versions.Extensions;

public static class ModuleRegistration
{
    public static IServiceCollection AddVersionsModule(this IServiceCollection services) => services;
}
