namespace MiniPdm.Contracts.Modules.Composition.DtoModels;

/// <summary>
/// Задаёт дочерний объект и его количество в составе.
/// </summary>
/// <param name="ChildObjectId">Идентификатор дочернего объекта.</param>
/// <param name="Quantity">Количество дочернего объекта.</param>
public sealed record CompositionItemDto(
    Guid ChildObjectId,
    int Quantity);

/// <summary>
/// Запрос на полную замену состава объекта.
/// Для выполнения обновления используется токен конкурентного доступа.
/// </summary>
/// <param name="Components">Новый список компонентов либо <see langword="null"/>.</param>
/// <param name="ExpectedConcurrencyToken">Токен конкурентного доступа, полученный при чтении состава.</param>
public sealed record ReplaceCompositionRequestDto(
    IReadOnlyList<CompositionItemDto>? Components,
    Guid ExpectedConcurrencyToken);
