namespace MiniPdm.Contracts.Modules.Calculations.DtoModels;

/// <summary>
/// Содержит рассчитанную массу сборки, спецификацию и диагностику.
/// Массы выражаются в килограммах, количества — в штуках.
/// </summary>
/// <param name="RootObjectId">Идентификатор корневого объекта расчёта.</param>
/// <param name="TotalMassKg">Суммарная масса сборки в килограммах либо <see langword="null"/>, если её нельзя определить.</param>
/// <param name="IsComplete">Показывает, известна ли полная суммарная масса с учётом диагностики.</param>
/// <param name="Items">Строки плоской спецификации.</param>
/// <param name="Diagnostics">Диагностические сообщения, возникшие при расчёте.</param>
public sealed record CompositionCalculationDto(
    Guid RootObjectId,
    decimal? TotalMassKg,
    bool IsComplete,
    IReadOnlyList<SpecificationItemDto> Items,
    IReadOnlyList<CalculationDiagnosticDto> Diagnostics);

/// <summary>
/// Строка плоской спецификации с количеством и массами.
/// Количества указаны в целых штуках; неизвестные значения представлены как <see langword="null"/>.
/// </summary>
/// <param name="ObjectId">Идентификатор объекта спецификации.</param>
/// <param name="Type">Тип объекта.</param>
/// <param name="Designation">Обозначение объекта либо <see langword="null"/>.</param>
/// <param name="Name">Наименование объекта либо <see langword="null"/>.</param>
/// <param name="Material">Материал объекта либо <see langword="null"/>.</param>
/// <param name="VersionId">Идентификатор учтённой версии либо <see langword="null"/>.</param>
/// <param name="VersionNumber">Номер учтённой версии либо <see langword="null"/>.</param>
/// <param name="Quantity">Количество объекта в спецификации либо <see langword="null"/>.</param>
/// <param name="UnitMassKg">Масса одной единицы в килограммах либо <see langword="null"/>.</param>
/// <param name="TotalMassKg">Суммарная масса данного объекта в килограммах либо <see langword="null"/>.</param>
public sealed record SpecificationItemDto(
    Guid ObjectId,
    string Type,
    string? Designation,
    string? Name,
    string? Material,
    Guid? VersionId,
    int? VersionNumber,
    decimal? Quantity,
    decimal? UnitMassKg,
    decimal? TotalMassKg);

/// <summary>
/// Диагностика, обнаруженная во время расчёта состава.
/// </summary>
/// <param name="Code">Машиночитаемый код диагностики.</param>
/// <param name="ObjectId">Идентификатор объекта, к которому относится диагностика.</param>
/// <param name="ObjectPath">Путь идентификаторов от корневого объекта до проблемного объекта.</param>
/// <param name="Message">Описание обнаруженной проблемы.</param>
public sealed record CalculationDiagnosticDto(
    string Code,
    Guid ObjectId,
    Guid[] ObjectPath,
    string Message);
