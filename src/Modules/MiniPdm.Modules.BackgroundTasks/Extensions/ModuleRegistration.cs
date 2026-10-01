using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniPdm.Modules.BackgroundTasks.Abstractions;
using MiniPdm.Modules.BackgroundTasks.Services;

namespace MiniPdm.Modules.BackgroundTasks.Extensions;

public static class ModuleRegistration
{
    public static IServiceCollection AddBackgroundTasksModule(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(BackgroundTaskCoordinatorOptions.Default);
        services.AddSingleton<BackgroundTaskCoordinator>();
        services.AddSingleton<IBackgroundTaskCoordinator>(sp => sp.GetRequiredService<BackgroundTaskCoordinator>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<BackgroundTaskCoordinator>());
        return services;
    }
}
