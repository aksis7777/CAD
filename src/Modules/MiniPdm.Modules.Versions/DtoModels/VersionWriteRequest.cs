namespace MiniPdm.Modules.Versions.DtoModels;

/// <summary>
/// Предусловие конкурентного доступа и данные для подготовки одной мутации версии.
/// </summary>
/// <param name="ObjectId">Идентификатор изменяемого объекта.</param>
/// <param name="VersionNumber">Номер версии, к которой относится мутация.</param>
/// <param name="ExpectedConcurrencyToken">Токен конкурентности, ожидаемый от клиента.</param>
/// <param name="ReferencedChildIds">Идентификаторы дочерних объектов для предварительной проверки.</param>
public sealed record VersionWriteRequest(
    Guid ObjectId,
    int VersionNumber,
    Guid ExpectedConcurrencyToken,
    IReadOnlyCollection<Guid> ReferencedChildIds);
