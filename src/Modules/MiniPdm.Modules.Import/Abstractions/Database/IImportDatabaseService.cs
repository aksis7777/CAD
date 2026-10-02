using MiniPdm.Modules.Import.DtoModels.Database;

namespace MiniPdm.Modules.Import.Abstractions.Database;

/// <summary>Import-specific transaction boundary and idempotency journal operations.</summary>
public interface IImportDatabaseService
{
    /// <summary>The prepare callback must perform its final cancellation check before promoting any files.</summary>
    Task<ImportPersistenceResult> ExecuteAsync(Guid importId, ImportLookup lookup, Func<ImportSnapshot, CancellationToken, Task<ImportWritePlan>> prepare, CancellationToken ct);
    Task<ImportPersistenceResult?> FindAsync(Guid id, CancellationToken ct);
    Task<ImportPersistenceResult> ResolveAsync(Guid id, CancellationToken ct);
    Task<bool> CompensateIfRolledBackAsync(Guid id, Func<CancellationToken, Task> compensate, CancellationToken ct);
}
