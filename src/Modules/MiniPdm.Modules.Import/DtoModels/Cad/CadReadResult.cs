namespace MiniPdm.Modules.Import.DtoModels.Cad;

/// <summary>
/// Результат чтения CAD-файла, содержащий документ или описание ошибки.
/// </summary>
public sealed record CadReadResult(CadDocument? Document, string? Error)
{
    /// <summary>
    /// Прочитанный документ либо <see langword="null"/>, если чтение не удалось.
    /// </summary>
    public CadDocument? Document { get; init; } = Document;

    /// <summary>
    /// Описание ошибки чтения либо <see langword="null"/> при успехе.
    /// </summary>
    public string? Error { get; init; } = Error;
}
