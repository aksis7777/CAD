using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Modules.Composition.DtoModels;

/// <summary>
/// Одно вхождение объекта на пути дерева состава.
/// Данные включают путь, локальное количество и текущую версию объекта.
/// </summary>
public sealed record CompositionOccurrence(
    Guid ObjectId,
    Guid[] ObjectPath,
    Guid[]? ParentPath,
    int LocalQuantity,
    PdmObjectType Type,
    string? Designation,
    string? Name,
    string? Material,
    Guid? VersionId,
    int? VersionNumber,
    VersionState? State,
    decimal? UnitMassKg,
    bool IsCycle)
{
    /// <summary>
    /// Идентификатор объекта, представленного этим вхождением дерева.
    /// </summary>
    public Guid ObjectId { get; init; } = ObjectId;

    /// <summary>
    /// Последовательность идентификаторов от корня дерева до данного объекта включительно.
    /// </summary>
    public Guid[] ObjectPath { get; init; } = ObjectPath;

    /// <summary>
    /// Последовательность идентификаторов от корня до родителя объекта.
    /// Для корневого вхождения путь родителя отсутствует.
    /// </summary>
    public Guid[]? ParentPath { get; init; } = ParentPath;

    /// <summary>
    /// Количество объекта относительно его непосредственного родителя.
    /// Для корневого вхождения значение равно единице.
    /// </summary>
    public int LocalQuantity { get; init; } = LocalQuantity;

    /// <summary>
    /// Тип объекта PDM, определяющий его свойства и правила расчёта.
    /// </summary>
    public PdmObjectType Type { get; init; } = Type;

    /// <summary>
    /// Обозначение сборки или детали; для стандартного изделия не задаётся.
    /// </summary>
    public string? Designation { get; init; } = Designation;

    /// <summary>
    /// Отображаемое имя объекта или имя его текущей версии.
    /// </summary>
    public string? Name { get; init; } = Name;

    /// <summary>
    /// Материал текущей версии, если он задан для объекта.
    /// </summary>
    public string? Material { get; init; } = Material;

    /// <summary>
    /// Идентификатор текущей версии объекта; равен null при её отсутствии.
    /// </summary>
    public Guid? VersionId { get; init; } = VersionId;

    /// <summary>
    /// Номер текущей версии объекта; равен null при её отсутствии.
    /// </summary>
    public int? VersionNumber { get; init; } = VersionNumber;

    /// <summary>
    /// Состояние текущей версии объекта, если она существует.
    /// </summary>
    public VersionState? State { get; init; } = State;

    /// <summary>
    /// Масса одной единицы объекта в килограммах, если она известна.
    /// Масса сборки в этой проекции не вычисляется.
    /// </summary>
    public decimal? UnitMassKg { get; init; } = UnitMassKg;

    /// <summary>
    /// Показывает, что объект уже присутствует в пути и образует цикл.
    /// </summary>
    public bool IsCycle { get; init; } = IsCycle;
}
