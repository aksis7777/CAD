namespace MiniPdm.Contracts.Modules.Versions.DtoModels;

/// <summary>
/// Результат изменения версии объекта.
/// История существующих версий сохраняется, а новая версия может стать текущей.
/// </summary>
public sealed record VersionMutationDto(
    Guid ObjectId,
    Guid VersionId,
    int VersionNumber,
    string State,
    Guid? CurrentVersionId,
    Guid ConcurrencyToken,
    IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// Идентификатор изменённого объекта.
    /// </summary>
    public Guid ObjectId { get; init; } = ObjectId;

    /// <summary>
    /// Идентификатор созданной или изменённой версии.
    /// </summary>
    public Guid VersionId { get; init; } = VersionId;

    /// <summary>
    /// Номер версии.
    /// </summary>
    public int VersionNumber { get; init; } = VersionNumber;

    /// <summary>
    /// Состояние версии после изменения.
    /// </summary>
    public string State { get; init; } = State;

    /// <summary>
    /// Идентификатор текущей версии после изменения либо <see langword="null"/>.
    /// </summary>
    public Guid? CurrentVersionId { get; init; } = CurrentVersionId;

    /// <summary>
    /// Новый токен конкурентного доступа объекта.
    /// </summary>
    public Guid ConcurrencyToken { get; init; } = ConcurrencyToken;

    /// <summary>
    /// Предупреждения, выявленные при выполнении операции.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Warnings;

}

/// <summary>
/// Описывает ошибку изменения версии, включая путь цикла, если он обнаружен.
/// </summary>
public sealed record VersionMutationErrorDto(
    string Code,
    string Message,
    Guid[]? CyclePath)
{
    /// <summary>
    /// Машиночитаемый код ошибки.
    /// </summary>
    public string Code { get; init; } = Code;

    /// <summary>
    /// Понятное пользователю описание ошибки.
    /// </summary>
    public string Message { get; init; } = Message;

    /// <summary>
    /// Идентификаторы объектов, образующие цикл, либо <see langword="null"/>.
    /// </summary>
    public Guid[]? CyclePath { get; init; } = CyclePath;

}
