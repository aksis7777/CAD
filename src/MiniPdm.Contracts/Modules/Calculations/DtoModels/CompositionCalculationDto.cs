namespace MiniPdm.Contracts.Modules.Calculations.DtoModels;

/// <summary>
/// Содержит рассчитанную массу сборки, спецификацию и диагностику.
/// Массы выражаются в килограммах, количества — в штуках.
/// </summary>
public sealed record CompositionCalculationDto(
    Guid RootObjectId,
    decimal? TotalMassKg,
    bool IsComplete,
    IReadOnlyList<SpecificationItemDto> Items,
    IReadOnlyList<CalculationDiagnosticDto> Diagnostics)
{
    /// <summary>
    /// Идентификатор корневого объекта расчёта.
    /// </summary>
    public Guid RootObjectId { get; init; } = RootObjectId;

    /// <summary>
    /// Суммарная масса сборки в килограммах либо <see langword="null"/>, если её нельзя определить.
    /// </summary>
    public decimal? TotalMassKg { get; init; } = TotalMassKg;

    /// <summary>
    /// Показывает, известна ли полная суммарная масса с учётом диагностики.
    /// </summary>
    public bool IsComplete { get; init; } = IsComplete;

    /// <summary>
    /// Строки плоской спецификации.
    /// </summary>
    public IReadOnlyList<SpecificationItemDto> Items { get; init; } = Items;

    /// <summary>
    /// Диагностические сообщения, возникшие при расчёте.
    /// </summary>
    public IReadOnlyList<CalculationDiagnosticDto> Diagnostics { get; init; } = Diagnostics;

}

/// <summary>
/// Строка плоской спецификации с количеством и массами.
/// Количества указаны в целых штуках; неизвестные значения представлены как <see langword="null"/>.
/// </summary>
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
    decimal? TotalMassKg)
{
    /// <summary>
    /// Идентификатор объекта спецификации.
    /// </summary>
    public Guid ObjectId { get; init; } = ObjectId;

    /// <summary>
    /// Тип объекта.
    /// </summary>
    public string Type { get; init; } = Type;

    /// <summary>
    /// Обозначение объекта либо <see langword="null"/>.
    /// </summary>
    public string? Designation { get; init; } = Designation;

    /// <summary>
    /// Наименование объекта либо <see langword="null"/>.
    /// </summary>
    public string? Name { get; init; } = Name;

    /// <summary>
    /// Материал объекта либо <see langword="null"/>.
    /// </summary>
    public string? Material { get; init; } = Material;

    /// <summary>
    /// Идентификатор учтённой версии либо <see langword="null"/>.
    /// </summary>
    public Guid? VersionId { get; init; } = VersionId;

    /// <summary>
    /// Номер учтённой версии либо <see langword="null"/>.
    /// </summary>
    public int? VersionNumber { get; init; } = VersionNumber;

    /// <summary>
    /// Количество объекта в спецификации либо <see langword="null"/>.
    /// </summary>
    public decimal? Quantity { get; init; } = Quantity;

    /// <summary>
    /// Масса одной единицы в килограммах либо <see langword="null"/>.
    /// </summary>
    public decimal? UnitMassKg { get; init; } = UnitMassKg;

    /// <summary>
    /// Суммарная масса данного объекта в килограммах либо <see langword="null"/>.
    /// </summary>
    public decimal? TotalMassKg { get; init; } = TotalMassKg;

}

/// <summary>
/// Диагностика, обнаруженная во время расчёта состава.
/// </summary>
public sealed record CalculationDiagnosticDto(
    string Code,
    Guid ObjectId,
    Guid[] ObjectPath,
    string Message)
{
    /// <summary>
    /// Машиночитаемый код диагностики.
    /// </summary>
    public string Code { get; init; } = Code;

    /// <summary>
    /// Идентификатор объекта, к которому относится диагностика.
    /// </summary>
    public Guid ObjectId { get; init; } = ObjectId;

    /// <summary>
    /// Путь идентификаторов от корневого объекта до проблемного объекта.
    /// </summary>
    public Guid[] ObjectPath { get; init; } = ObjectPath;

    /// <summary>
    /// Описание обнаруженной проблемы.
    /// </summary>
    public string Message { get; init; } = Message;

}
