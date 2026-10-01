namespace MiniPdm.Contracts.Modules.Composition.DtoModels;

/// <summary>A direct composition snapshot for one version; unlike the tree endpoint, items reference child objects only.</summary>
public sealed record VersionCompositionDto(Guid ObjectId, int Version, Guid ConcurrencyToken,
    IReadOnlyList<VersionCompositionItemDto> Items);

public sealed record VersionCompositionItemDto(Guid ChildObjectId, int Quantity, string Type,
    string? Designation, string? Name, bool NoCurrentVersion);
