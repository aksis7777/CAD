using MiniPdm.Domain.Composition;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Domain.Versions.Mutations;

/// <summary>
///     Результат планирования изменения версии.
/// </summary>
public enum VersionMutationStatus
{
    /// <summary>
    ///     Изменение прошло проверку и может быть сохранено.
    /// </summary>
    Succeeded,

    /// <summary>
    ///     Выбранный объект или версия не существует.
    /// </summary>
    NotFound,

    /// <summary>
    ///     Изменение конфликтует с текущим состоянием объекта.
    /// </summary>
    Conflict,

    /// <summary>
    ///     Запрос содержит недопустимые значения.
    /// </summary>
    Invalid
}

/// <summary>
///     Машиночитаемое и понятное пользователю описание отклонённого изменения.
/// </summary>
/// <param name="Code">
///     Стабильный код ошибки для вызывающей стороны.
/// </param>
/// <param name="Message">
///     Описание для показа пользователю.
/// </param>
/// <param name="CyclePath">
///     Замкнутый путь объектов, если причиной ошибки стал цикл.
/// </param>
public sealed record VersionMutationError(string Code, string Message, Guid[]? CyclePath = null);

/// <summary>
///     Загруженное состояние для планирования изменения версии без обращения к базе данных.
/// </summary>
/// <param name="Object">
///     Объект, версия которого изменяется.
/// </param>
/// <param name="SelectedVersion">
///     Версия, выбранная вызывающей стороной.
/// </param>
/// <param name="CurrentGraph">
///     Связи действующего состава для проверки циклов.
/// </param>
/// <param name="ExistingChildIds">
///     Идентификаторы объектов, которые можно добавить как компоненты.
/// </param>
public sealed record VersionMutationSnapshot(PdmObject Object, ObjectVersion SelectedVersion,
    IReadOnlyList<CompositionGraphEdge> CurrentGraph, IReadOnlySet<Guid> ExistingChildIds);

/// <summary>
///     Предлагаемые изменения базы данных и диагностики изменения версии.
/// </summary>
/// <param name="Status">
///     Результат операции.
/// </param>
/// <param name="Version">
///     Версия, возвращаемая операцией.
/// </param>
/// <param name="DesiredCurrentVersionId">
///     Идентификатор текущей версии после применения плана.
/// </param>
/// <param name="NewVersion">
///     Новая версия для добавления при клонировании.
/// </param>
/// <param name="RemovedLinks">
///     Существующие строки компонентов для удаления.
/// </param>
/// <param name="Error">
///     Описание причины отклонения плана.
/// </param>
/// <param name="Warnings">
///     Предупреждения проверки, не препятствующие операции.
/// </param>
public sealed record VersionMutationPlan(VersionMutationStatus Status, ObjectVersion? Version,
    Guid? DesiredCurrentVersionId, ObjectVersion? NewVersion, IReadOnlyList<BomLink> RemovedLinks,
    VersionMutationError? Error, IReadOnlyList<string> Warnings);

/// <summary>
///     Результат применения плана изменения версии.
/// </summary>
/// <param name="Status">
///     Результат операции.
/// </param>
/// <param name="ObjectId">
///     Идентификатор изменённого объекта.
/// </param>
/// <param name="VersionId">
///     Идентификатор изменённой версии, если он доступен.
/// </param>
/// <param name="VersionNumber">
///     Номер изменённой версии, если он доступен.
/// </param>
/// <param name="State">
///     Состояние изменённой версии, если оно доступно.
/// </param>
/// <param name="CurrentVersionId">
///     Идентификатор текущей версии после изменения.
/// </param>
/// <param name="ConcurrencyToken">
///     Новый токен объекта после успешной записи.
/// </param>
/// <param name="Error">
///     Описание причины отклонения изменения.
/// </param>
/// <param name="Warnings">
///     Предупреждения проверки, не препятствующие операции.
/// </param>
public sealed record VersionMutationResult(VersionMutationStatus Status, Guid ObjectId, Guid? VersionId,
    int? VersionNumber, VersionState? State, Guid? CurrentVersionId, Guid? ConcurrencyToken,
    VersionMutationError? Error, IReadOnlyList<string> Warnings);

/// <summary>
///     Компонент и его количество в новом составе сборки.
/// </summary>
/// <param name="ChildObjectId">
///     Идентификатор объекта-компонента.
/// </param>
/// <param name="Quantity">
///     Положительное количество экземпляров компонента.
/// </param>
public sealed record CompositionItem(Guid ChildObjectId, int Quantity);
