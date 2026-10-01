using Microsoft.Extensions.DependencyInjection;

namespace MiniPdm.Modules.Calculations.Extensions;

public static class ModuleRegistration
{
    public static IServiceCollection AddCalculationsModule(this IServiceCollection services) => services;
}
