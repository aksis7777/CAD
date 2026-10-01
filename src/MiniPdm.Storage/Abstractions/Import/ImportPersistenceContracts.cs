using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Composition;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Storage.Abstractions.Import;

public sealed record ImportLookup(IReadOnlyCollection<string> Designations, IReadOnlyCollection<string> NormalizedStandardNames);
public sealed record ActiveGraphEdge(Guid ParentId, Guid ChildId);
public sealed record ImportSnapshot(IReadOnlyList<PdmObject> ExistingObjects, IReadOnlyList<ActiveGraphEdge> CurrentGraph);
public sealed record CurrentVersionAssignment(PdmObject Object, ObjectVersion Version);
public sealed record ImportWritePlan(IReadOnlyList<PdmObject> NewObjects, IReadOnlyList<ObjectVersion> NewVersions, IReadOnlyList<CurrentVersionAssignment> CurrentVersions, string ReportJson, IReadOnlyList<BomLink>? RemovedLinks = null);
public enum ImportCommitState { Completed, ConfirmedRollback, Unknown }
public sealed record ImportPersistenceResult(ImportCommitState State, bool Replayed, string? ReportJson, string? Error = null);

public interface IImportPersistence
{
    /// <summary>The prepare callback must perform its final cancellation check before promoting any files.</summary>
    Task<ImportPersistenceResult> ExecuteAsync(Guid importId, ImportLookup lookup, Func<ImportSnapshot, CancellationToken, Task<ImportWritePlan>> prepare, CancellationToken ct);
    Task<ImportPersistenceResult?> FindAsync(Guid id, CancellationToken ct);
    Task<ImportPersistenceResult> ResolveAsync(Guid id, CancellationToken ct);
    Task<bool> CompensateIfRolledBackAsync(Guid id, Func<CancellationToken, Task> compensate, CancellationToken ct);
}
