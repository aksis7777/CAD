using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Modules.Objects.DtoModels;

public sealed record ObjectSearchRow(
    Guid Id,
    PdmObjectType Type,
    string? Designation,
    string? Name,
    Guid? CurrentVersionId,
    int? VersionNumber,
    VersionState? State,
    decimal? UnitMassKg,
    Guid ConcurrencyToken,
    bool NoCurrentVersion);

public sealed record ObjectSearchPage(IReadOnlyList<ObjectSearchRow> Items, int Offset, int Limit, bool HasMore);

public sealed record ObjectVersionReadRow(
    Guid Id,
    int Version,
    VersionState State,
    string? Name,
    string? Material,
    decimal? Mass,
    string? SourceReference);

public sealed record ObjectCardReadRow(
    Guid Id,
    PdmObjectType Type,
    string? Designation,
    string? StandardName,
    Guid? CurrentVersionId,
    Guid ConcurrencyToken,
    IReadOnlyList<ObjectVersionSummaryReadRow> Versions,
    ObjectVersionReadRow? SelectedVersion);

public sealed record ObjectVersionSummaryReadRow(Guid Id, int Version, VersionState State);
