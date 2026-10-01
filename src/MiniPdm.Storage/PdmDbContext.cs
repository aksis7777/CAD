using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MiniPdm.Domain.Composition;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Storage;

/// <summary>Scoped unit of work. Callers changing data must load affected objects first so their original concurrency token is the one checked by EF.</summary>
public sealed class PdmDbContext(DbContextOptions<PdmDbContext> options) : DbContext(options)
{
    public DbSet<PdmObject> Objects => Set<PdmObject>();
    public DbSet<ObjectVersion> Versions => Set<ObjectVersion>();
    public DbSet<BomLink> BomLinks => Set<BomLink>();
    public DbSet<Entities.ImportJournal> ImportJournals => Set<Entities.ImportJournal>();
    public DbSet<Entities.BackgroundTask> BackgroundTasks => Set<Entities.BackgroundTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PdmDbContext).Assembly);

    public override int SaveChanges() => SaveChanges(true);
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareForSave();
        BumpChangedParents();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => SaveChangesAsync(true, cancellationToken);
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepareForSave();
        await BumpChangedParentsAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareForSave()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<ObjectVersion>().Where(IsChanging))
        {
            if (entry.State == EntityState.Modified && entry.Property(x => x.ObjectId).IsModified)
                throw new InvalidOperationException("ObjectVersion.ObjectId is immutable.");
            var mass = entry.Entity.Mass;
            if (mass is < 0 || mass is { } value && (value >= 1_000_000_000_000m || decimal.Round(value, 6) != value))
                throw new InvalidOperationException("Mass must be nonnegative and exactly representable as numeric(18,6).");
        }
        foreach (var entry in ChangeTracker.Entries<BomLink>().Where(IsChanging))
        {
            if (entry.State == EntityState.Modified && (entry.Property(x => x.ParentVersionId).IsModified || entry.Property(x => x.ChildObjectId).IsModified))
                throw new InvalidOperationException("BomLink keys are immutable.");
        }
    }

    private void BumpChangedParents()
    {
        var objectIds = ChangedParentObjectIds();
        foreach (var id in objectIds)
        {
            var parent = Objects.Local.FirstOrDefault(x => x.Id == id) ?? Objects.Find(id);
            if (parent is not null) parent.ConcurrencyToken = Guid.NewGuid();
        }
    }

    private HashSet<Guid> ChangedParentObjectIds()
    {
        var ids = ChangeTracker.Entries<ObjectVersion>().Where(IsChanging).Select(e => e.Entity.ObjectId).ToHashSet();
        foreach (var entry in ChangeTracker.Entries<PdmObject>().Where(e => e.State == EntityState.Modified))
            ids.Add(entry.Entity.Id);

        var links = ChangeTracker.Entries<BomLink>().Where(IsChanging).ToArray();
        var versionIds = links.Select(e => e.Entity.ParentVersionId).Distinct().ToArray();
        ids.UnionWith(Versions.Local.Where(v => versionIds.Contains(v.Id)).Select(v => v.ObjectId));
        var unresolved = versionIds.Except(Versions.Local.Select(v => v.Id)).ToArray();
        if (unresolved.Length > 0)
            ids.UnionWith(Versions.Where(v => unresolved.Contains(v.Id)).Select(v => v.ObjectId).ToArray());
        return ids;
    }

    private async Task<HashSet<Guid>> ChangedParentObjectIdsAsync(CancellationToken ct)
    {
        var ids = ChangeTracker.Entries<ObjectVersion>().Where(IsChanging).Select(e => e.Entity.ObjectId).ToHashSet();
        foreach (var entry in ChangeTracker.Entries<PdmObject>().Where(e => e.State == EntityState.Modified))
            ids.Add(entry.Entity.Id);

        var links = ChangeTracker.Entries<BomLink>().Where(IsChanging).ToArray();
        var versionIds = links.Select(e => e.Entity.ParentVersionId).Distinct().ToArray();
        ids.UnionWith(Versions.Local.Where(v => versionIds.Contains(v.Id)).Select(v => v.ObjectId));
        var unresolved = versionIds.Except(Versions.Local.Select(v => v.Id)).ToArray();
        if (unresolved.Length > 0)
        {
            var owners = await Versions.AsNoTracking().Where(v => unresolved.Contains(v.Id)).Select(v => v.ObjectId).ToListAsync(ct);
            ids.UnionWith(owners);
        }
        return ids;
    }

    private async Task BumpChangedParentsAsync(CancellationToken ct)
    {
        var objectIds = await ChangedParentObjectIdsAsync(ct);
        foreach (var id in objectIds)
        {
            var parent = Objects.Local.FirstOrDefault(x => x.Id == id) ?? await Objects.FindAsync([id], ct);
            if (parent is not null) parent.ConcurrencyToken = Guid.NewGuid();
        }
    }

    private static bool IsChanging(EntityEntry entry) => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;
}
