namespace MiniPdm.Contracts.Modules.Composition.DtoModels;

/// <summary>
/// Представляет плоский список вхождений состава объекта.
/// Пути сохраняют структуру дерева без рекурсивного JSON-вложенного представления.
/// </summary>
/// <param name="RootObjectId">Идентификатор корневого объекта состава.</param>
/// <param name="Nodes">Узлы состава в порядке обхода дерева.</param>
public sealed record CompositionTreeDto(
    Guid RootObjectId,
    IReadOnlyList<CompositionNodeDto> Nodes);

/// <summary>
/// Описывает одно вхождение объекта в дереве состава и его путь от корня.
/// </summary>
/// <param name="ObjectId">Идентификатор объекта в этом вхождении.</param>
/// <param name="ObjectPath">Путь идентификаторов от корневого объекта до данного вхождения.</param>
/// <param name="ParentPath">Путь родителя либо <see langword="null"/> для корневого узла.</param>
/// <param name="LocalQuantity">Количество объекта в непосредственном родительском узле.</param>
/// <param name="Type">Тип объекта.</param>
/// <param name="Designation">Обозначение объекта либо <see langword="null"/>.</param>
/// <param name="Name">Наименование объекта либо <see langword="null"/>.</param>
/// <param name="Material">Материал выбранной версии либо <see langword="null"/>.</param>
/// <param name="VersionId">Идентификатор использованной версии либо <see langword="null"/>.</param>
/// <param name="VersionNumber">Номер использованной версии либо <see langword="null"/>.</param>
/// <param name="State">Состояние использованной версии либо <see langword="null"/>.</param>
/// <param name="UnitMassKg">Масса единицы в килограммах либо <see langword="null"/>.</param>
/// <param name="ErrorCode">Код ошибки получения сведений об узле либо <see langword="null"/>.</param>
/// <param name="Error">Описание ошибки получения сведений об узле либо <see langword="null"/>.</param>
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
    string? Error);
