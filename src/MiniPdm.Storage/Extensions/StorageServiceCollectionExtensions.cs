using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniPdm.Storage.Abstractions.Composition;
using MiniPdm.Storage.Abstractions.BackgroundTasks;
using MiniPdm.Storage.Abstractions.Import;
using MiniPdm.Storage.Abstractions.Objects;
using MiniPdm.Storage.Abstractions.Versions;
using MiniPdm.Storage.Queries;
using MiniPdm.Storage.Repositories;

namespace MiniPdm.Storage.Extensions;

public static class StorageServiceCollectionExtensions
{
    public static IServiceCollection AddPdmStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PdmDatabase") ?? Environment.GetEnvironmentVariable("PDM_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Configure ConnectionStrings:PdmDatabase or PDM_CONNECTION_STRING.");
        services.AddDbContextFactory<PdmDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<PdmDbContext>>().CreateDbContext());
        services.AddScoped<ICompositionReadQuery, CompositionReadQuery>();
        services.AddScoped<IVersionCompositionReadQuery, VersionCompositionReadQuery>();
        services.AddScoped<IObjectReadQuery, ObjectReadQuery>();
        services.AddScoped<IImportPersistence, ImportPersistence>();
        services.AddScoped<IVersionWritePersistence, VersionWritePersistence>();
        services.AddScoped<IBackgroundTaskPersistence, BackgroundTaskPersistence>();
        return services;
    }
}
