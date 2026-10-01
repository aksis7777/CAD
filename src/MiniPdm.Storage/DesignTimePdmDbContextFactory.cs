using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MiniPdm.Storage;

public sealed class DesignTimePdmDbContextFactory : IDesignTimeDbContextFactory<PdmDbContext>
{
    public PdmDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PDM_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Set PDM_CONNECTION_STRING to create or apply a database migration.");
        var options = new DbContextOptionsBuilder<PdmDbContext>().UseNpgsql(connectionString).Options;
        return new PdmDbContext(options);
    }
}
