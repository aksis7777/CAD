namespace MiniPdm.Contracts.Modules.Import.DtoModels;

/// <summary>
/// Определяет результат обработки файла импорта.
/// </summary>
public enum ImportFileStatus
{
    /// <summary>
    /// Файл принят и обработан.
    /// </summary>
    Accepted,

    /// <summary>
    /// Файл отклонён и не обработан.
    /// </summary>
    Rejected
}

/// <summary>
/// Определяет действие, выполненное при обработке файла.
/// </summary>
public enum ImportFileAction
{
    /// <summary>
    /// Создан новый объект.
    /// </summary>
    Created,

    /// <summary>
    /// Обновлены данные существующего объекта.
    /// </summary>
    Updated,

    /// <summary>
    /// Создана новая версия существующего объекта.
    /// </summary>
    NewVersion,

    /// <summary>
    /// Файл совпадает с уже сохранёнными данными, изменений не потребовалось.
    /// </summary>
    Unchanged
}

/// <summary>
/// Результат обработки одного файла в пакете импорта.
/// </summary>
public sealed record ImportFileResultDto(
    string FileName,
    ImportFileStatus Status,
    string? Reason,
    ImportFileAction? Action,
    IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// Имя обработанного файла.
    /// </summary>
    public string FileName { get; init; } = FileName;

    /// <summary>
    /// Статус обработки файла.
    /// </summary>
    public ImportFileStatus Status { get; init; } = Status;

    /// <summary>
    /// Причина отклонения либо <see langword="null"/>.
    /// </summary>
    public string? Reason { get; init; } = Reason;

    /// <summary>
    /// Выполненное действие либо <see langword="null"/>, если файл отклонён.
    /// </summary>
    public ImportFileAction? Action { get; init; } = Action;

    /// <summary>
    /// Предупреждения, сформированные при обработке файла.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Warnings;

}

/// <summary>
/// Сводный отчёт об обработке пакета импорта.
/// </summary>
public sealed record ImportReportDto(
    Guid ImportId,
    IReadOnlyList<ImportFileResultDto> Files)
{
    /// <summary>
    /// Идентификатор операции импорта.
    /// </summary>
    public Guid ImportId { get; init; } = ImportId;

    /// <summary>
    /// Результаты обработки отдельных файлов.
    /// </summary>
    public IReadOnlyList<ImportFileResultDto> Files { get; init; } = Files;


    /// <summary>
    /// Возвращает число файлов, принятых при импорте.
    /// </summary>
    public int AcceptedCount => Files.Count(x => x.Status == ImportFileStatus.Accepted);

    /// <summary>
    /// Возвращает число файлов, отклонённых при импорте.
    /// </summary>
    public int RejectedCount => Files.Count(x => x.Status == ImportFileStatus.Rejected);

    /// <summary>
    /// Возвращает число принятых файлов, для которых сформированы предупреждения.
    /// </summary>
    public int WarningCount => Files.Count(x => x.Status == ImportFileStatus.Accepted && x.Warnings.Count > 0);
}
