using MiniPdm.Domain.Objects;

namespace MiniPdm.Modules.Composition.DtoModels;

/// <summary>
/// Внутренняя проекция состава конкретной версии.
/// Содержит токен конкурентности и дочерние элементы состава.
/// </summary>
/// <param name="ObjectId">Идентификатор родительского объекта.</param>
/// <param name="Version">Номер версии, состав которой прочитан.</param>
/// <param name="ConcurrencyToken">Токен объекта для последующего изменения.</param>
/// <param name="Items">Дочерние объекты и сведения об их текущих версиях.</param>
public sealed record VersionCompositionReadRow(Guid ObjectId, int Version, Guid ConcurrencyToken,
    IReadOnlyList<VersionCompositionItemReadRow> Items);

/// <summary>
/// Одна строка внутренней проекции состава версии.
/// Имя и обозначение описывают дочерний объект, а не конкретную версию.
/// </summary>
/// <param name="ChildObjectId">Идентификатор дочернего объекта.</param>
/// <param name="Quantity">Количество дочернего объекта в составе.</param>
/// <param name="Type">Тип дочернего объекта PDM.</param>
/// <param name="Designation">Обозначение дочерней сборки или детали.</param>
/// <param name="Name">Имя дочернего объекта или его текущей версии.</param>
/// <param name="NoCurrentVersion">Показывает, что у дочернего объекта нет текущей версии.</param>
public sealed record VersionCompositionItemReadRow(Guid ChildObjectId, int Quantity, PdmObjectType Type,
    string? Designation, string? Name, bool NoCurrentVersion);
