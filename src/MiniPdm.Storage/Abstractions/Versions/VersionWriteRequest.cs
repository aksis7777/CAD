namespace MiniPdm.Storage.Abstractions.Versions;

/// <summary>Optimistic concurrency precondition and inputs needed to prepare one version mutation.</summary>
public sealed record VersionWriteRequest(
    Guid ObjectId,
    int VersionNumber,
    Guid ExpectedConcurrencyToken,
    IReadOnlyCollection<Guid> ReferencedChildIds);
