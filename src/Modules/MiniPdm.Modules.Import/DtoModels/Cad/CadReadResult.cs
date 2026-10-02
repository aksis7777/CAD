namespace MiniPdm.Modules.Import.DtoModels.Cad;

/// <summary>
/// Результат чтения CAD-файла, содержащий документ или описание ошибки.
/// </summary>
/// <param name="Document">Прочитанный документ либо <see langword="null"/> при ошибке.</param>
/// <param name="Error">Описание ошибки чтения либо <see langword="null"/> при успехе.</param>
public sealed record CadReadResult(CadDocument? Document, string? Error);
