using Microsoft.Extensions.DependencyInjection;
using MiniPdm.Modules.Calculations.Services;

namespace MiniPdm.Modules.Calculations.Extensions;

public static class ModuleRegistration
{
    public static IServiceCollection AddCalculationsModule(this IServiceCollection services)
    {
        services.AddScoped<CompositionCalculationService>();
        return services;
    }
}
