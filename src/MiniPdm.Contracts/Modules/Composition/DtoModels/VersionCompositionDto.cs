namespace MiniPdm.Contracts.Modules.Composition.DtoModels;

/// <summary>
/// Снимок непосредственного состава одной версии объекта.
/// Элементы ссылаются только на дочерние объекты и не раскрывают их состав.
/// </summary>
/// <param name="ObjectId">Идентификатор объекта-владельца состава.</param>
/// <param name="Version">Номер версии, для которой получен состав.</param>
/// <param name="ConcurrencyToken">Токен конкурентного доступа версии.</param>
/// <param name="Items">Непосредственные компоненты версии.</param>
public sealed record VersionCompositionDto(
    Guid ObjectId,
    int Version,
    Guid ConcurrencyToken,
    IReadOnlyList<VersionCompositionItemDto> Items);

/// <summary>
/// Один непосредственный компонент состава версии.
/// </summary>
/// <param name="ChildObjectId">Идентификатор дочернего объекта.</param>
/// <param name="Quantity">Количество дочернего объекта в составе.</param>
/// <param name="Type">Тип дочернего объекта.</param>
/// <param name="Designation">Обозначение дочернего объекта либо <see langword="null"/>.</param>
/// <param name="Name">Наименование дочернего объекта либо <see langword="null"/>.</param>
/// <param name="NoCurrentVersion">Указывает, что у дочернего объекта нет текущей версии.</param>
public sealed record VersionCompositionItemDto(
    Guid ChildObjectId,
    int Quantity,
    string Type,
    string? Designation,
    string? Name,
    bool NoCurrentVersion);
