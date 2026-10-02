namespace MiniPdm.Contracts.Modules.Versions.DtoModels;

/// <summary>
/// Запрашивает создание рабочей версии на основе выбранной версии объекта.
/// Существующая история версий при этом сохраняется.
/// </summary>
public sealed record CloneVersionRequestDto(
    int SourceVersion,
    Guid ExpectedConcurrencyToken)
{
    /// <summary>
    /// Номер версии, используемой как источник копирования.
    /// </summary>
    public int SourceVersion { get; init; } = SourceVersion;

    /// <summary>
    /// Токен конкурентного доступа объекта, полученный при чтении.
    /// </summary>
    public Guid ExpectedConcurrencyToken { get; init; } = ExpectedConcurrencyToken;

}

/// <summary>
/// Запрашивает изменение состояния версии.
/// </summary>
public sealed record ChangeVersionStateRequestDto(
    string State,
    Guid ExpectedConcurrencyToken)
{
    /// <summary>
    /// Новое состояние версии.
    /// </summary>
    public string State { get; init; } = State;

    /// <summary>
    /// Токен конкурентного доступа объекта, полученный при чтении.
    /// </summary>
    public Guid ExpectedConcurrencyToken { get; init; } = ExpectedConcurrencyToken;

}
