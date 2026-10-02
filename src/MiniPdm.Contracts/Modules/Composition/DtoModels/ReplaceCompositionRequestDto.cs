namespace MiniPdm.Contracts.Modules.Composition.DtoModels;

/// <summary>
/// Задаёт дочерний объект и его количество в составе.
/// </summary>
public sealed record CompositionItemDto(
    Guid ChildObjectId,
    int Quantity)
{
    /// <summary>
    /// Идентификатор дочернего объекта.
    /// </summary>
    public Guid ChildObjectId { get; init; } = ChildObjectId;

    /// <summary>
    /// Количество дочернего объекта.
    /// </summary>
    public int Quantity { get; init; } = Quantity;

}

/// <summary>
/// Запрос на полную замену состава объекта.
/// Для выполнения обновления используется токен конкурентного доступа.
/// </summary>
public sealed record ReplaceCompositionRequestDto(
    IReadOnlyList<CompositionItemDto>? Components,
    Guid ExpectedConcurrencyToken)
{
    /// <summary>
    /// Новый список компонентов либо <see langword="null"/>.
    /// </summary>
    public IReadOnlyList<CompositionItemDto>? Components { get; init; } = Components;

    /// <summary>
    /// Токен конкурентного доступа, полученный при чтении состава.
    /// </summary>
    public Guid ExpectedConcurrencyToken { get; init; } = ExpectedConcurrencyToken;

}
