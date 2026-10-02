using MiniPdm.Domain.Composition;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Modules.Import.DtoModels.Database;

public sealed record ImportLookup(IReadOnlyCollection<string> Designations, IReadOnlyCollection<string> NormalizedStandardNames);
public sealed record ActiveGraphEdge(Guid ParentId, Guid ChildId);
public sealed record ImportSnapshot(IReadOnlyList<PdmObject> ExistingObjects, IReadOnlyList<ActiveGraphEdge> CurrentGraph);
public sealed record CurrentVersionAssignment(PdmObject Object, ObjectVersion Version);
public sealed record ImportWritePlan(IReadOnlyList<PdmObject> NewObjects, IReadOnlyList<ObjectVersion> NewVersions, IReadOnlyList<CurrentVersionAssignment> CurrentVersions, string ReportJson, IReadOnlyList<BomLink>? RemovedLinks = null);
public enum ImportCommitState { Completed, ConfirmedRollback, Unknown }
public sealed record ImportPersistenceResult(ImportCommitState State, bool Replayed, string? ReportJson, string? Error = null);
