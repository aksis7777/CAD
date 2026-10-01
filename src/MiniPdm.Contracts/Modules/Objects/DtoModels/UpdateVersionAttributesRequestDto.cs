namespace MiniPdm.Contracts.Modules.Objects.DtoModels;

public sealed record UpdateVersionAttributesRequestDto(string? Name, string? Material, decimal? Mass,
    Guid ExpectedConcurrencyToken);
