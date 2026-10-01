using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Storage.Abstractions.Composition;

/// <summary>A single occurrence of an object along a composition tree path.</summary>
public sealed record CompositionOccurrence(
    Guid ObjectId,
    Guid[] ObjectPath,
    Guid[]? ParentPath,
    int LocalQuantity,
    PdmObjectType Type,
    string? Designation,
    string? Name,
    string? Material,
    Guid? VersionId,
    int? VersionNumber,
    VersionState? State,
    decimal? UnitMassKg,
    bool IsCycle);
