namespace MiniPdm.Contracts.Modules.Composition.DtoModels;

/// <summary>
/// Представляет плоский список вхождений состава объекта.
/// Пути сохраняют структуру дерева без рекурсивного JSON-вложенного представления.
/// </summary>
public sealed record CompositionTreeDto(
    Guid RootObjectId,
    IReadOnlyList<CompositionNodeDto> Nodes)
{
    /// <summary>
    /// Идентификатор корневого объекта состава.
    /// </summary>
    public Guid RootObjectId { get; init; } = RootObjectId;

    /// <summary>
    /// Узлы состава в порядке обхода дерева.
    /// </summary>
    public IReadOnlyList<CompositionNodeDto> Nodes { get; init; } = Nodes;

}

/// <summary>
/// Описывает одно вхождение объекта в дереве состава и его путь от корня.
/// </summary>
public sealed record CompositionNodeDto(
    Guid ObjectId,
    IReadOnlyList<Guid> ObjectPath,
    IReadOnlyList<Guid>? ParentPath,
    int LocalQuantity,
    string Type,
    string? Designation,
    string? Name,
    string? Material,
    Guid? VersionId,
    int? VersionNumber,
    string? State,
    decimal? UnitMassKg,
    string? ErrorCode,
    string? Error)
{
    /// <summary>
    /// Идентификатор объекта в этом вхождении.
    /// </summary>
    public Guid ObjectId { get; init; } = ObjectId;

    /// <summary>
    /// Путь идентификаторов от корневого объекта до данного вхождения.
    /// </summary>
    public IReadOnlyList<Guid> ObjectPath { get; init; } = ObjectPath;

    /// <summary>
    /// Путь родителя либо <see langword="null"/> для корневого узла.
    /// </summary>
    public IReadOnlyList<Guid>? ParentPath { get; init; } = ParentPath;

    /// <summary>
    /// Количество объекта в непосредственном родительском узле.
    /// </summary>
    public int LocalQuantity { get; init; } = LocalQuantity;

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
    /// Материал выбранной версии либо <see langword="null"/>.
    /// </summary>
    public string? Material { get; init; } = Material;

    /// <summary>
    /// Идентификатор использованной версии либо <see langword="null"/>.
    /// </summary>
    public Guid? VersionId { get; init; } = VersionId;

    /// <summary>
    /// Номер использованной версии либо <see langword="null"/>.
    /// </summary>
    public int? VersionNumber { get; init; } = VersionNumber;

    /// <summary>
    /// Состояние использованной версии либо <see langword="null"/>.
    /// </summary>
    public string? State { get; init; } = State;

    /// <summary>
    /// Масса единицы в килограммах либо <see langword="null"/>.
    /// </summary>
    public decimal? UnitMassKg { get; init; } = UnitMassKg;

    /// <summary>
    /// Код ошибки получения сведений об узле либо <see langword="null"/>.
    /// </summary>
    public string? ErrorCode { get; init; } = ErrorCode;

    /// <summary>
    /// Описание ошибки получения сведений об узле либо <see langword="null"/>.
    /// </summary>
    public string? Error { get; init; } = Error;

}
