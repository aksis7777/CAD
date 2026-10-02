using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Modules.Composition.DtoModels;

/// <summary>
/// Одно вхождение объекта на пути дерева состава.
/// Данные включают путь, локальное количество и текущую версию объекта.
/// </summary>
/// <param name="ObjectId">Идентификатор объекта-вхождения.</param>
/// <param name="ObjectPath">Путь от корня дерева до этого объекта включительно.</param>
/// <param name="ParentPath">Путь до родительского объекта; для корня равен <see langword="null"/>.</param>
/// <param name="LocalQuantity">Количество объекта относительно непосредственного родителя.</param>
/// <param name="Type">Тип объекта PDM.</param>
/// <param name="Designation">Обозначение сборки или детали.</param>
/// <param name="Name">Отображаемое имя объекта.</param>
/// <param name="Material">Материал текущей версии, если задан.</param>
/// <param name="VersionId">Идентификатор текущей версии, если она есть.</param>
/// <param name="VersionNumber">Номер текущей версии, если она есть.</param>
/// <param name="State">Состояние текущей версии, если она есть.</param>
/// <param name="UnitMassKg">Масса единицы объекта в килограммах, если известна.</param>
/// <param name="IsCycle">Показывает, что объект повторно встретился на текущем пути.</param>
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
    bool IsCycle);
