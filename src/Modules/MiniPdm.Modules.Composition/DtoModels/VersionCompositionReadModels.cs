using MiniPdm.Domain.Objects;

namespace MiniPdm.Modules.Composition.DtoModels;

/// <summary>
/// Внутренняя проекция состава конкретной версии.
/// Содержит токен конкурентности и дочерние элементы состава.
/// </summary>
public sealed record VersionCompositionReadRow(Guid ObjectId, int Version, Guid ConcurrencyToken,
    IReadOnlyList<VersionCompositionItemReadRow> Items)
{
    /// <summary>
    /// Идентификатор родительского объекта.
    /// </summary>
    public Guid ObjectId { get; init; } = ObjectId;

    /// <summary>
    /// Номер версии, состав которой прочитан.
    /// </summary>
    public int Version { get; init; } = Version;

    /// <summary>
    /// Токен объекта для последующего изменения состава.
    /// </summary>
    public Guid ConcurrencyToken { get; init; } = ConcurrencyToken;

    /// <summary>
    /// Дочерние объекты и сведения об их текущих версиях.
    /// </summary>
    public IReadOnlyList<VersionCompositionItemReadRow> Items { get; init; } = Items;
}

/// <summary>
/// Одна строка внутренней проекции состава версии.
/// Имя и обозначение описывают дочерний объект, а не конкретную версию.
/// </summary>
public sealed record VersionCompositionItemReadRow(Guid ChildObjectId, int Quantity, PdmObjectType Type,
    string? Designation, string? Name, bool NoCurrentVersion)
{
    /// <summary>
    /// Идентификатор дочернего объекта.
    /// </summary>
    public Guid ChildObjectId { get; init; } = ChildObjectId;

    /// <summary>
    /// Количество дочернего объекта в составе.
    /// </summary>
    public int Quantity { get; init; } = Quantity;

    /// <summary>
    /// Тип дочернего объекта PDM.
    /// </summary>
    public PdmObjectType Type { get; init; } = Type;

    /// <summary>
    /// Обозначение дочерней сборки или детали.
    /// </summary>
    public string? Designation { get; init; } = Designation;

    /// <summary>
    /// Имя дочернего объекта или его текущей версии.
    /// </summary>
    public string? Name { get; init; } = Name;

    /// <summary>
    /// Показывает, что у дочернего объекта нет текущей версии.
    /// </summary>
    public bool NoCurrentVersion { get; init; } = NoCurrentVersion;
}
