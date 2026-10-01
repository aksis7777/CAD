using Microsoft.Extensions.DependencyInjection;
using MiniPdm.Modules.Import.Abstractions;
using MiniPdm.Modules.Import.Abstractions.Cad;
using MiniPdm.Modules.Import.Infrastructure.Cad;
using MiniPdm.Modules.Import.Infrastructure.SourceFiles;
using MiniPdm.Modules.Import.Services;

namespace MiniPdm.Modules.Import.Extensions;

public static class ModuleRegistration
{
    public static IServiceCollection AddImportModule(this IServiceCollection services)
    {
        services.AddOptions<ImportStorageOptions>().BindConfiguration("ImportStorage")
            .Validate(FileImportStorage.HasValidLimits, "Import upload limits are outside the allowed range.")
            .ValidateOnStart();
        services.AddSingleton<ICadSourceAdapter, FileJsonCadSourceAdapter>();
        services.AddSingleton<ICadSourceFactory, CadSourceFactory>();
        services.AddSingleton<FileImportStorage>();
        services.AddSingleton<IImportSourceStorage>(sp => sp.GetRequiredService<FileImportStorage>());
        services.AddSingleton<IImportUploadStorage>(sp => sp.GetRequiredService<FileImportStorage>());
        services.AddScoped<ImportSourceRecovery>();
        services.AddScoped<ImportService>();
        return services;
    }
}
