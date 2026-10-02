using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        return services;
    }
}
