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
/// <param name="FileName">Имя обработанного файла.</param>
/// <param name="Status">Статус обработки файла.</param>
/// <param name="Reason">Причина отклонения либо <see langword="null"/>.</param>
/// <param name="Action">Выполненное действие либо <see langword="null"/>, если файл отклонён.</param>
/// <param name="Warnings">Предупреждения, сформированные при обработке файла.</param>
public sealed record ImportFileResultDto(
    string FileName,
    ImportFileStatus Status,
    string? Reason,
    ImportFileAction? Action,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Сводный отчёт об обработке пакета импорта.
/// </summary>
/// <param name="ImportId">Идентификатор операции импорта.</param>
/// <param name="Files">Результаты обработки отдельных файлов.</param>
public sealed record ImportReportDto(
    Guid ImportId,
    IReadOnlyList<ImportFileResultDto> Files)
{
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
