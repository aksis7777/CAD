using MiniPdm.Domain.Objects;

namespace MiniPdm.Modules.Composition.DtoModels;

public sealed record VersionCompositionReadRow(Guid ObjectId, int Version, Guid ConcurrencyToken,
    IReadOnlyList<VersionCompositionItemReadRow> Items);

public sealed record VersionCompositionItemReadRow(Guid ChildObjectId, int Quantity, PdmObjectType Type,
    string? Designation, string? Name, bool NoCurrentVersion);
