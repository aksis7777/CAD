using MiniPdm.Domain.Composition;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Modules.Import.DtoModels.Database;

/// <summary>
/// Ключи, по которым хранилище выбирает существующие объекты для импорта.
/// </summary>
/// <param name="Designations">Обозначения импортируемых объектов.</param>
/// <param name="NormalizedStandardNames">Нормализованные наименования стандартных деталей.</param>
public sealed record ImportLookup(IReadOnlyCollection<string> Designations, IReadOnlyCollection<string> NormalizedStandardNames);
/// <summary>
/// Направленное ребро активного графа сборки между объектами.
/// </summary>
/// <param name="ParentId">Идентификатор родительского объекта.</param>
/// <param name="ChildId">Идентификатор дочернего объекта.</param>
public sealed record ActiveGraphEdge(Guid ParentId, Guid ChildId);
/// <summary>
/// Снимок найденных объектов и активной структуры для подготовки импорта.
/// </summary>
/// <param name="ExistingObjects">Существующие объекты, совпавшие с ключами поиска.</param>
/// <param name="CurrentGraph">Рёбра текущего активного графа.</param>
public sealed record ImportSnapshot(IReadOnlyList<PdmObject> ExistingObjects, IReadOnlyList<ActiveGraphEdge> CurrentGraph);
/// <summary>
/// Назначение версии объекта текущей при записи импорта.
/// </summary>
/// <param name="Object">Объект, чей указатель текущей версии меняется.</param>
/// <param name="Version">Версия, назначаемая текущей.</param>
public sealed record CurrentVersionAssignment(PdmObject Object, ObjectVersion Version);
/// <summary>
/// Подготовленный набор изменений базы данных и отчёт для фиксации импорта.
/// </summary>
/// <param name="NewObjects">Новые объекты.</param>
/// <param name="NewVersions">Новые версии.</param>
/// <param name="CurrentVersions">Назначения текущих версий.</param>
/// <param name="ReportJson">Сериализованный итоговый отчёт.</param>
/// <param name="RemovedLinks">Связи состава, удаляемые при замене; по умолчанию отсутствуют.</param>
public sealed record ImportWritePlan(IReadOnlyList<PdmObject> NewObjects, IReadOnlyList<ObjectVersion> NewVersions, IReadOnlyList<CurrentVersionAssignment> CurrentVersions, string ReportJson, IReadOnlyList<BomLink>? RemovedLinks = null);
/// <summary>
/// Известный исход транзакции импорта.
/// </summary>
public enum ImportCommitState
{
    /// <summary>
    /// Транзакция завершена успешно.
    /// </summary>
    Completed,
    /// <summary>
    /// Откат транзакции подтверждён.
    /// </summary>
    ConfirmedRollback,
    /// <summary>
    /// Исход фиксации нельзя достоверно установить.
    /// </summary>
    Unknown
}
/// <summary>
/// Сохранённый или восстановленный результат обработки операции импорта.
/// </summary>
/// <param name="State">Известное состояние транзакции.</param>
/// <param name="Replayed">Признак возврата уже сохранённого результата.</param>
/// <param name="ReportJson">Сериализованный отчёт, если операция завершилась.</param>
/// <param name="Error">Описание ошибки при неуспешном исходе.</param>
public sealed record ImportPersistenceResult(ImportCommitState State, bool Replayed, string? ReportJson, string? Error = null);
