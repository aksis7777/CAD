namespace MiniPdm.Modules.Import.DtoModels.Cad;

public sealed record CadReadResult(CadDocument? Document, string? Error);
