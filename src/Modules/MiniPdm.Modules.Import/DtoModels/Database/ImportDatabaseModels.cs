using MiniPdm.Domain.Composition;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Modules.Import.DtoModels.Database;

/// <summary>
/// Ключи, по которым хранилище выбирает существующие объекты для импорта.
/// </summary>
public sealed record ImportLookup(IReadOnlyCollection<string> Designations, IReadOnlyCollection<string> NormalizedStandardNames)
{
    /// <summary>
    /// Обозначения объектов из пакета, используемые для поиска совпадений.
    /// </summary>
    public IReadOnlyCollection<string> Designations { get; init; } = Designations;

    /// <summary>
    /// Нормализованные наименования стандартных деталей для поиска совпадений.
    /// </summary>
    public IReadOnlyCollection<string> NormalizedStandardNames { get; init; } = NormalizedStandardNames;
}
/// <summary>
/// Направленное ребро активного графа сборки между объектами.
/// </summary>
public sealed record ActiveGraphEdge(Guid ParentId, Guid ChildId)
{
    /// <summary>
    /// Идентификатор родительского объекта в ребре графа.
    /// </summary>
    public Guid ParentId { get; init; } = ParentId;

    /// <summary>
    /// Идентификатор дочернего объекта в ребре графа.
    /// </summary>
    public Guid ChildId { get; init; } = ChildId;
}
/// <summary>
/// Снимок найденных объектов и активной структуры для подготовки импорта.
/// </summary>
public sealed record ImportSnapshot(IReadOnlyList<PdmObject> ExistingObjects, IReadOnlyList<ActiveGraphEdge> CurrentGraph)
{
    /// <summary>
    /// Существующие объекты, совпавшие с ключами поиска импорта.
    /// </summary>
    public IReadOnlyList<PdmObject> ExistingObjects { get; init; } = ExistingObjects;

    /// <summary>
    /// Рёбра текущего активного графа состава.
    /// </summary>
    public IReadOnlyList<ActiveGraphEdge> CurrentGraph { get; init; } = CurrentGraph;
}
/// <summary>
/// Назначение версии объекта текущей при записи импорта.
/// </summary>
public sealed record CurrentVersionAssignment(PdmObject Object, ObjectVersion Version)
{
    /// <summary>
    /// Объект, чей указатель текущей версии назначается.
    /// </summary>
    public PdmObject Object { get; init; } = Object;

    /// <summary>
    /// Версия, назначаемая текущей для объекта.
    /// </summary>
    public ObjectVersion Version { get; init; } = Version;
}
/// <summary>
/// Подготовленный набор изменений базы данных и отчёт для фиксации импорта.
/// </summary>
public sealed record ImportWritePlan(IReadOnlyList<PdmObject> NewObjects, IReadOnlyList<ObjectVersion> NewVersions, IReadOnlyList<CurrentVersionAssignment> CurrentVersions, string ReportJson, IReadOnlyList<BomLink>? RemovedLinks = null)
{
    /// <summary>
    /// Новые PDM-объекты, создаваемые транзакцией импорта.
    /// </summary>
    public IReadOnlyList<PdmObject> NewObjects { get; init; } = NewObjects;

    /// <summary>
    /// Новые версии, добавляемые транзакцией импорта.
    /// </summary>
    public IReadOnlyList<ObjectVersion> NewVersions { get; init; } = NewVersions;

    /// <summary>
    /// Назначения новых текущих версий существующим объектам.
    /// </summary>
    public IReadOnlyList<CurrentVersionAssignment> CurrentVersions { get; init; } = CurrentVersions;

    /// <summary>
    /// Сериализованный отчёт, сохраняемый вместе с результатом импорта.
    /// </summary>
    public string ReportJson { get; init; } = ReportJson;

    /// <summary>
    /// Связи состава, удаляемые транзакцией при замене содержимого.
    /// </summary>
    public IReadOnlyList<BomLink>? RemovedLinks { get; init; } = RemovedLinks;
}
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
public sealed record ImportPersistenceResult(ImportCommitState State, bool Replayed, string? ReportJson, string? Error = null)
{
    /// <summary>
    /// Известное состояние фиксации операции импорта.
    /// </summary>
    public ImportCommitState State { get; init; } = State;

    /// <summary>
    /// Указывает, что возвращён ранее сохранённый результат для того же идентификатора.
    /// </summary>
    public bool Replayed { get; init; } = Replayed;

    /// <summary>
    /// Сериализованный отчёт завершённого импорта, если он доступен.
    /// </summary>
    public string? ReportJson { get; init; } = ReportJson;

    /// <summary>
    /// Описание ошибки или неопределённого исхода, если оно есть.
    /// </summary>
    public string? Error { get; init; } = Error;
}
