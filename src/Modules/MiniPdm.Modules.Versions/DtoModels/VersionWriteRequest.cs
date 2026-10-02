namespace MiniPdm.Modules.Versions.DtoModels;

/// <summary>Concurrency precondition and inputs required to prepare one version mutation.</summary>
public sealed record VersionWriteRequest(
    Guid ObjectId,
    int VersionNumber,
    Guid ExpectedConcurrencyToken,
    IReadOnlyCollection<Guid> ReferencedChildIds);
