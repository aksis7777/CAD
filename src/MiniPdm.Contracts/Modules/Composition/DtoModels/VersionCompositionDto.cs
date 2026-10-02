namespace MiniPdm.Contracts.Modules.Composition.DtoModels;

/// <summary>
/// Снимок непосредственного состава одной версии объекта.
/// Элементы ссылаются только на дочерние объекты и не раскрывают их состав.
/// </summary>
public sealed record VersionCompositionDto(
    Guid ObjectId,
    int Version,
    Guid ConcurrencyToken,
    IReadOnlyList<VersionCompositionItemDto> Items)
{
    /// <summary>
    /// Идентификатор объекта-владельца состава.
    /// </summary>
    public Guid ObjectId { get; init; } = ObjectId;

    /// <summary>
    /// Номер версии, для которой получен состав.
    /// </summary>
    public int Version { get; init; } = Version;

    /// <summary>
    /// Токен конкурентного доступа версии.
    /// </summary>
    public Guid ConcurrencyToken { get; init; } = ConcurrencyToken;

    /// <summary>
    /// Непосредственные компоненты версии.
    /// </summary>
    public IReadOnlyList<VersionCompositionItemDto> Items { get; init; } = Items;

}

/// <summary>
/// Один непосредственный компонент состава версии.
/// </summary>
public sealed record VersionCompositionItemDto(
    Guid ChildObjectId,
    int Quantity,
    string Type,
    string? Designation,
    string? Name,
    bool NoCurrentVersion)
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
    /// Тип дочернего объекта.
    /// </summary>
    public string Type { get; init; } = Type;

    /// <summary>
    /// Обозначение дочернего объекта либо <see langword="null"/>.
    /// </summary>
    public string? Designation { get; init; } = Designation;

    /// <summary>
    /// Наименование дочернего объекта либо <see langword="null"/>.
    /// </summary>
    public string? Name { get; init; } = Name;

    /// <summary>
    /// Указывает, что у дочернего объекта нет текущей версии.
    /// </summary>
    public bool NoCurrentVersion { get; init; } = NoCurrentVersion;

}
