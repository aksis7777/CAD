namespace MiniPdm.Contracts.Modules.Objects.DtoModels;

public sealed record ObjectSearchPageDto(IReadOnlyList<ObjectSearchItemDto> Items, int Offset, int Limit, bool HasMore);
public sealed record ObjectSearchItemDto(Guid Id, string Type, string? Designation, string? Name, Guid? CurrentVersionId,
    int? VersionNumber, string? State, decimal? UnitMassKg, Guid ConcurrencyToken, bool NoCurrentVersion);
public sealed record ObjectCardDto(Guid Id, string Type, string? Designation, string? Name, Guid? CurrentVersionId,
    Guid ConcurrencyToken, ObjectVersionDto? SelectedVersion, IReadOnlyList<ObjectVersionSummaryDto> Versions, string? ErrorCode, string? Error);
public sealed record ObjectVersionDto(Guid Id, int Version, string State, string? Name, string? Material, decimal? UnitMassKg,
    string? SourceReference, bool IsCurrent);
public sealed record ObjectVersionSummaryDto(Guid Id, int Version, string State, bool IsCurrent);
