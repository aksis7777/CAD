namespace MiniPdm.Contracts.Modules.Objects.DtoModels;

/// <summary>
/// Страница результатов поиска объектов.
/// Значения смещения и размера страницы позволяют продолжить чтение списка.
/// </summary>
public sealed record ObjectSearchPageDto(
    IReadOnlyList<ObjectSearchItemDto> Items,
    int Offset,
    int Limit,
    bool HasMore)
{
    /// <summary>
    /// Найденные объекты на текущей странице.
    /// </summary>
    public IReadOnlyList<ObjectSearchItemDto> Items { get; init; } = Items;

    /// <summary>
    /// Число записей, пропущенных перед этой страницей.
    /// </summary>
    public int Offset { get; init; } = Offset;

    /// <summary>
    /// Максимальное число записей, запрошенное для страницы.
    /// </summary>
    public int Limit { get; init; } = Limit;

    /// <summary>
    /// Указывает, есть ли записи после текущей страницы.
    /// </summary>
    public bool HasMore { get; init; } = HasMore;

}

/// <summary>
/// Краткие сведения об объекте, найденном при поиске.
/// </summary>
public sealed record ObjectSearchItemDto(
    Guid Id,
    string Type,
    string? Designation,
    string? Name,
    Guid? CurrentVersionId,
    int? VersionNumber,
    string? State,
    decimal? UnitMassKg,
    Guid ConcurrencyToken,
    bool NoCurrentVersion)
{
    /// <summary>
    /// Идентификатор объекта.
    /// </summary>
    public Guid Id { get; init; } = Id;

    /// <summary>
    /// Тип объекта, например сборка, деталь или стандартное изделие.
    /// </summary>
    public string Type { get; init; } = Type;

    /// <summary>
    /// Обозначение объекта либо <see langword="null"/>.
    /// </summary>
    public string? Designation { get; init; } = Designation;

    /// <summary>
    /// Наименование объекта либо <see langword="null"/>.
    /// </summary>
    public string? Name { get; init; } = Name;

    /// <summary>
    /// Идентификатор текущей версии либо <see langword="null"/>, если у объекта нет текущей версии.
    /// </summary>
    public Guid? CurrentVersionId { get; init; } = CurrentVersionId;

    /// <summary>
    /// Номер текущей версии либо <see langword="null"/>.
    /// </summary>
    public int? VersionNumber { get; init; } = VersionNumber;

    /// <summary>
    /// Состояние текущей версии либо <see langword="null"/>.
    /// </summary>
    public string? State { get; init; } = State;

    /// <summary>
    /// Масса единицы текущей версии в килограммах либо <see langword="null"/>.
    /// </summary>
    public decimal? UnitMassKg { get; init; } = UnitMassKg;

    /// <summary>
    /// Токен конкурентного доступа объекта.
    /// </summary>
    public Guid ConcurrencyToken { get; init; } = ConcurrencyToken;

    /// <summary>
    /// Указывает, что у объекта отсутствует текущая версия.
    /// </summary>
    public bool NoCurrentVersion { get; init; } = NoCurrentVersion;

}

/// <summary>
/// Карточка объекта с выбранной версией и перечнем его версий.
/// </summary>
public sealed record ObjectCardDto(
    Guid Id,
    string Type,
    string? Designation,
    string? Name,
    Guid? CurrentVersionId,
    Guid ConcurrencyToken,
    ObjectVersionDto? SelectedVersion,
    IReadOnlyList<ObjectVersionSummaryDto> Versions,
    string? ErrorCode,
    string? Error)
{
    /// <summary>
    /// Идентификатор объекта.
    /// </summary>
    public Guid Id { get; init; } = Id;

    /// <summary>
    /// Тип объекта.
    /// </summary>
    public string Type { get; init; } = Type;

    /// <summary>
    /// Обозначение объекта либо <see langword="null"/>.
    /// </summary>
    public string? Designation { get; init; } = Designation;

    /// <summary>
    /// Наименование объекта либо <see langword="null"/>.
    /// </summary>
    public string? Name { get; init; } = Name;

    /// <summary>
    /// Идентификатор текущей версии либо <see langword="null"/>.
    /// </summary>
    public Guid? CurrentVersionId { get; init; } = CurrentVersionId;

    /// <summary>
    /// Токен конкурентного доступа объекта.
    /// </summary>
    public Guid ConcurrencyToken { get; init; } = ConcurrencyToken;

    /// <summary>
    /// Сведения о выбранной версии либо <see langword="null"/>.
    /// </summary>
    public ObjectVersionDto? SelectedVersion { get; init; } = SelectedVersion;

    /// <summary>
    /// Список доступных версий объекта.
    /// </summary>
    public IReadOnlyList<ObjectVersionSummaryDto> Versions { get; init; } = Versions;

    /// <summary>
    /// Код ошибки получения выбранной версии либо <see langword="null"/>.
    /// </summary>
    public string? ErrorCode { get; init; } = ErrorCode;

    /// <summary>
    /// Описание ошибки получения выбранной версии либо <see langword="null"/>.
    /// </summary>
    public string? Error { get; init; } = Error;

}

/// <summary>
/// Подробные сведения о версии объекта.
/// </summary>
public sealed record ObjectVersionDto(
    Guid Id,
    int Version,
    string State,
    string? Name,
    string? Material,
    decimal? UnitMassKg,
    string? SourceReference,
    bool IsCurrent)
{
    /// <summary>
    /// Идентификатор версии.
    /// </summary>
    public Guid Id { get; init; } = Id;

    /// <summary>
    /// Порядковый номер версии.
    /// </summary>
    public int Version { get; init; } = Version;

    /// <summary>
    /// Состояние версии.
    /// </summary>
    public string State { get; init; } = State;

    /// <summary>
    /// Наименование версии либо <see langword="null"/>.
    /// </summary>
    public string? Name { get; init; } = Name;

    /// <summary>
    /// Материал версии либо <see langword="null"/>.
    /// </summary>
    public string? Material { get; init; } = Material;

    /// <summary>
    /// Масса единицы в килограммах либо <see langword="null"/>.
    /// </summary>
    public decimal? UnitMassKg { get; init; } = UnitMassKg;

    /// <summary>
    /// Ссылка на исходный файл либо <see langword="null"/>.
    /// </summary>
    public string? SourceReference { get; init; } = SourceReference;

    /// <summary>
    /// Указывает, является ли версия текущей.
    /// </summary>
    public bool IsCurrent { get; init; } = IsCurrent;

}

/// <summary>
/// Краткие сведения о версии объекта для отображения в списке.
/// </summary>
public sealed record ObjectVersionSummaryDto(
    Guid Id,
    int Version,
    string State,
    bool IsCurrent)
{
    /// <summary>
    /// Идентификатор версии.
    /// </summary>
    public Guid Id { get; init; } = Id;

    /// <summary>
    /// Порядковый номер версии.
    /// </summary>
    public int Version { get; init; } = Version;

    /// <summary>
    /// Состояние версии.
    /// </summary>
    public string State { get; init; } = State;

    /// <summary>
    /// Указывает, является ли версия текущей.
    /// </summary>
    public bool IsCurrent { get; init; } = IsCurrent;

}
