namespace MiniPdm.Contracts.Modules.Versions.DtoModels;

/// <summary>
/// Результат изменения версии объекта.
/// История существующих версий сохраняется, а новая версия может стать текущей.
/// </summary>
/// <param name="ObjectId">Идентификатор изменённого объекта.</param>
/// <param name="VersionId">Идентификатор созданной или изменённой версии.</param>
/// <param name="VersionNumber">Номер версии.</param>
/// <param name="State">Состояние версии после изменения.</param>
/// <param name="CurrentVersionId">Идентификатор текущей версии после изменения либо <see langword="null"/>.</param>
/// <param name="ConcurrencyToken">Новый токен конкурентного доступа объекта.</param>
/// <param name="Warnings">Предупреждения, выявленные при выполнении операции.</param>
public sealed record VersionMutationDto(
    Guid ObjectId,
    Guid VersionId,
    int VersionNumber,
    string State,
    Guid? CurrentVersionId,
    Guid ConcurrencyToken,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Описывает ошибку изменения версии, включая путь цикла, если он обнаружен.
/// </summary>
/// <param name="Code">Машиночитаемый код ошибки.</param>
/// <param name="Message">Понятное пользователю описание ошибки.</param>
/// <param name="CyclePath">Идентификаторы объектов, образующие цикл, либо <see langword="null"/>.</param>
public sealed record VersionMutationErrorDto(
    string Code,
    string Message,
    Guid[]? CyclePath);
