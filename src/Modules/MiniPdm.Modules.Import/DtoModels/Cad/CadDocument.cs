using MiniPdm.Domain.Objects;

namespace MiniPdm.Modules.Import.DtoModels.Cad;

public sealed record CadDocument(
    string FileName,
    PdmObjectType Type,
    string? Designation,
    string Name,
    string? Material,
    decimal? Mass,
    IReadOnlyList<CadComponent> Components);

public sealed record CadComponent(string File, int Count);
