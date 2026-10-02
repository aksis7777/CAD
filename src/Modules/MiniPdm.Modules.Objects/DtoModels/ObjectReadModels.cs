using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Modules.Objects.DtoModels;

/// <summary>
/// Строка внутренней проекции поиска объектов.
/// Содержит поля карточки, необходимые для выдачи страницы результатов.
/// </summary>
public sealed record ObjectSearchRow(
    Guid Id,
    PdmObjectType Type,
    string? Designation,
    string? Name,
    Guid? CurrentVersionId,
    int? VersionNumber,
    VersionState? State,
    decimal? UnitMassKg,
    Guid ConcurrencyToken,
    bool NoCurrentVersion)
{
    /// <summary>
    /// Идентификатор объекта.
    /// </summary>
    public Guid Id { get; init; } = Id;

    /// <summary>
    /// Тип объекта PDM.
    /// </summary>
    public PdmObjectType Type { get; init; } = Type;

    /// <summary>
    /// Обозначение сборки или детали.
    /// </summary>
    public string? Designation { get; init; } = Designation;

    /// <summary>
    /// Отображаемое наименование объекта.
    /// </summary>
    public string? Name { get; init; } = Name;

    /// <summary>
    /// Идентификатор текущей версии, если она есть.
    /// </summary>
    public Guid? CurrentVersionId { get; init; } = CurrentVersionId;

    /// <summary>
    /// Номер текущей версии, если она есть.
    /// </summary>
    public int? VersionNumber { get; init; } = VersionNumber;

    /// <summary>
    /// Состояние текущей версии, если она есть.
    /// </summary>
    public VersionState? State { get; init; } = State;

    /// <summary>
    /// Масса единицы объекта в килограммах, если известна.
    /// </summary>
    public decimal? UnitMassKg { get; init; } = UnitMassKg;

    /// <summary>
    /// Токен для обнаружения устаревших изменений.
    /// </summary>
    public Guid ConcurrencyToken { get; init; } = ConcurrencyToken;

    /// <summary>
    /// Показывает, что у объекта нет текущей версии.
    /// </summary>
    public bool NoCurrentVersion { get; init; } = NoCurrentVersion;
}

/// <summary>
/// Страница внутренней проекции поиска объектов.
/// Метаданные позволяют продолжить постраничную загрузку результатов.
/// </summary>
public sealed record ObjectSearchPage(IReadOnlyList<ObjectSearchRow> Items, int Offset, int Limit, bool HasMore)
{
    /// <summary>
    /// Строки объектов на текущей странице.
    /// </summary>
    public IReadOnlyList<ObjectSearchRow> Items { get; init; } = Items;

    /// <summary>
    /// Смещение первой строки в полном результате.
    /// </summary>
    public int Offset { get; init; } = Offset;

    /// <summary>
    /// Максимальное число строк страницы.
    /// </summary>
    public int Limit { get; init; } = Limit;

    /// <summary>
    /// Указывает, что после страницы есть дополнительные строки.
    /// </summary>
    public bool HasMore { get; init; } = HasMore;
}

/// <summary>
/// Внутренняя проекция одной версии для карточки объекта.
/// Включает атрибуты версии и ссылку на исходный файл.
/// </summary>
public sealed record ObjectVersionReadRow(
    Guid Id,
    int Version,
    VersionState State,
    string? Name,
    string? Material,
    decimal? Mass,
    string? SourceReference)
{
    /// <summary>
    /// Идентификатор записи версии.
    /// </summary>
    public Guid Id { get; init; } = Id;

    /// <summary>
    /// Порядковый номер версии внутри объекта.
    /// </summary>
    public int Version { get; init; } = Version;

    /// <summary>
    /// Состояние версии.
    /// </summary>
    public VersionState State { get; init; } = State;

    /// <summary>
    /// Наименование, сохранённое в версии.
    /// </summary>
    public string? Name { get; init; } = Name;

    /// <summary>
    /// Материал версии, если задан.
    /// </summary>
    public string? Material { get; init; } = Material;

    /// <summary>
    /// Масса версии, если задана.
    /// </summary>
    public decimal? Mass { get; init; } = Mass;

    /// <summary>
    /// Ссылка на импортированный исходный файл.
    /// </summary>
    public string? SourceReference { get; init; } = SourceReference;
}

/// <summary>
/// Внутренняя проекция карточки объекта и списка его версий.
/// Выбранная версия отсутствует, если её нет или она не была запрошена.
/// </summary>
public sealed record ObjectCardReadRow(
    Guid Id,
    PdmObjectType Type,
    string? Designation,
    string? StandardName,
    Guid? CurrentVersionId,
    Guid ConcurrencyToken,
    IReadOnlyList<ObjectVersionSummaryReadRow> Versions,
    ObjectVersionReadRow? SelectedVersion)
{
    /// <summary>
    /// Идентификатор объекта.
    /// </summary>
    public Guid Id { get; init; } = Id;

    /// <summary>
    /// Тип объекта PDM.
    /// </summary>
    public PdmObjectType Type { get; init; } = Type;

    /// <summary>
    /// Обозначение сборки или детали.
    /// </summary>
    public string? Designation { get; init; } = Designation;

    /// <summary>
    /// Исходное имя стандартного изделия.
    /// </summary>
    public string? StandardName { get; init; } = StandardName;

    /// <summary>
    /// Идентификатор текущей версии, если она есть.
    /// </summary>
    public Guid? CurrentVersionId { get; init; } = CurrentVersionId;

    /// <summary>
    /// Токен конкурентности объекта.
    /// </summary>
    public Guid ConcurrencyToken { get; init; } = ConcurrencyToken;

    /// <summary>
    /// Краткие сведения обо всех версиях объекта.
    /// </summary>
    public IReadOnlyList<ObjectVersionSummaryReadRow> Versions { get; init; } = Versions;

    /// <summary>
    /// Полные данные запрошенной или текущей версии.
    /// </summary>
    public ObjectVersionReadRow? SelectedVersion { get; init; } = SelectedVersion;
}

/// <summary>
/// Краткая внутренняя проекция версии для списка в карточке объекта.
/// </summary>
public sealed record ObjectVersionSummaryReadRow(Guid Id, int Version, VersionState State)
{
    /// <summary>
    /// Идентификатор записи версии.
    /// </summary>
    public Guid Id { get; init; } = Id;

    /// <summary>
    /// Порядковый номер версии.
    /// </summary>
    public int Version { get; init; } = Version;

    /// <summary>
    /// Состояние версии.
    /// </summary>
    public VersionState State { get; init; } = State;
}
