namespace MiniPdm.Modules.Versions.DtoModels;

/// <summary>
/// Предусловие конкурентного доступа и данные для подготовки одной мутации версии.
/// </summary>
public sealed record VersionWriteRequest(
    Guid ObjectId,
    int VersionNumber,
    Guid ExpectedConcurrencyToken,
    IReadOnlyCollection<Guid> ReferencedChildIds)
{
    /// <summary>
    /// Идентификатор изменяемого объекта.
    /// </summary>
    public Guid ObjectId { get; init; } = ObjectId;

    /// <summary>
    /// Номер версии, к которой относится мутация.
    /// </summary>
    public int VersionNumber { get; init; } = VersionNumber;

    /// <summary>
    /// Токен конкурентности, ожидаемый от клиента.
    /// </summary>
    public Guid ExpectedConcurrencyToken { get; init; } = ExpectedConcurrencyToken;

    /// <summary>
    /// Идентификаторы дочерних объектов для предварительной проверки.
    /// </summary>
    public IReadOnlyCollection<Guid> ReferencedChildIds { get; init; } = ReferencedChildIds;
}
