using MiniPdm.Domain.Composition;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Domain.Versions.Mutations;

public enum VersionMutationStatus { Succeeded, NotFound, Conflict, Invalid }
public sealed record VersionMutationError(string Code, string Message, Guid[]? CyclePath = null);
public sealed record VersionMutationSnapshot(PdmObject Object, ObjectVersion SelectedVersion,
    IReadOnlyList<CompositionGraphEdge> CurrentGraph, IReadOnlySet<Guid> ExistingChildIds);
public sealed record VersionMutationPlan(VersionMutationStatus Status, ObjectVersion? Version,
    Guid? DesiredCurrentVersionId, ObjectVersion? NewVersion, IReadOnlyList<BomLink> RemovedLinks,
    VersionMutationError? Error, IReadOnlyList<string> Warnings);
public sealed record VersionMutationResult(VersionMutationStatus Status, Guid ObjectId, Guid? VersionId,
    int? VersionNumber, VersionState? State, Guid? CurrentVersionId, Guid? ConcurrencyToken,
    VersionMutationError? Error, IReadOnlyList<string> Warnings);
public sealed record CompositionItem(Guid ChildObjectId, int Quantity);
