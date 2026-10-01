namespace MiniPdm.Contracts.Modules.Composition.DtoModels;

/// <summary>A flat list of composition occurrences. Paths preserve the tree without recursive JSON nesting.</summary>
public sealed record CompositionTreeDto(Guid RootObjectId, IReadOnlyList<CompositionNodeDto> Nodes);

public sealed record CompositionNodeDto(
    Guid ObjectId,
    IReadOnlyList<Guid> ObjectPath,
    IReadOnlyList<Guid>? ParentPath,
    int LocalQuantity,
    string Type,
    string? Designation,
    string? Name,
    string? Material,
    Guid? VersionId,
    int? VersionNumber,
    string? State,
    decimal? UnitMassKg,
    string? ErrorCode,
    string? Error);
