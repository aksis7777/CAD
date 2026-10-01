using Microsoft.Extensions.DependencyInjection;

namespace MiniPdm.Modules.Composition.Extensions;

public static class ModuleRegistration
{
    public static IServiceCollection AddCompositionModule(this IServiceCollection services) => services;
}
