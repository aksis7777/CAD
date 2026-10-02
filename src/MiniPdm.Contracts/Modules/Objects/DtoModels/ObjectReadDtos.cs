namespace MiniPdm.Contracts.Modules.Objects.DtoModels;

/// <summary>
/// Страница результатов поиска объектов.
/// Значения смещения и размера страницы позволяют продолжить чтение списка.
/// </summary>
/// <param name="Items">Найденные объекты на текущей странице.</param>
/// <param name="Offset">Число записей, пропущенных перед этой страницей.</param>
/// <param name="Limit">Максимальное число записей, запрошенное для страницы.</param>
/// <param name="HasMore">Указывает, есть ли записи после текущей страницы.</param>
public sealed record ObjectSearchPageDto(
    IReadOnlyList<ObjectSearchItemDto> Items,
    int Offset,
    int Limit,
    bool HasMore);

/// <summary>
/// Краткие сведения об объекте, найденном при поиске.
/// </summary>
/// <param name="Id">Идентификатор объекта.</param>
/// <param name="Type">Тип объекта, например сборка, деталь или стандартное изделие.</param>
/// <param name="Designation">Обозначение объекта либо <see langword="null"/>.</param>
/// <param name="Name">Наименование объекта либо <see langword="null"/>.</param>
/// <param name="CurrentVersionId">Идентификатор текущей версии либо <see langword="null"/>, если у объекта нет текущей версии.</param>
/// <param name="VersionNumber">Номер текущей версии либо <see langword="null"/>.</param>
/// <param name="State">Состояние текущей версии либо <see langword="null"/>.</param>
/// <param name="UnitMassKg">Масса единицы текущей версии в килограммах либо <see langword="null"/>.</param>
/// <param name="ConcurrencyToken">Токен конкурентного доступа объекта.</param>
/// <param name="NoCurrentVersion">Указывает, что у объекта отсутствует текущая версия.</param>
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
    bool NoCurrentVersion);

/// <summary>
/// Карточка объекта с выбранной версией и перечнем его версий.
/// </summary>
/// <param name="Id">Идентификатор объекта.</param>
/// <param name="Type">Тип объекта.</param>
/// <param name="Designation">Обозначение объекта либо <see langword="null"/>.</param>
/// <param name="Name">Наименование объекта либо <see langword="null"/>.</param>
/// <param name="CurrentVersionId">Идентификатор текущей версии либо <see langword="null"/>.</param>
/// <param name="ConcurrencyToken">Токен конкурентного доступа объекта.</param>
/// <param name="SelectedVersion">Сведения о выбранной версии либо <see langword="null"/>.</param>
/// <param name="Versions">Список доступных версий объекта.</param>
/// <param name="ErrorCode">Код ошибки получения выбранной версии либо <see langword="null"/>.</param>
/// <param name="Error">Описание ошибки получения выбранной версии либо <see langword="null"/>.</param>
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
    string? Error);

/// <summary>
/// Подробные сведения о версии объекта.
/// </summary>
/// <param name="Id">Идентификатор версии.</param>
/// <param name="Version">Порядковый номер версии.</param>
/// <param name="State">Состояние версии.</param>
/// <param name="Name">Наименование версии либо <see langword="null"/>.</param>
/// <param name="Material">Материал версии либо <see langword="null"/>.</param>
/// <param name="UnitMassKg">Масса единицы в килограммах либо <see langword="null"/>.</param>
/// <param name="SourceReference">Ссылка на исходный файл либо <see langword="null"/>.</param>
/// <param name="IsCurrent">Указывает, является ли версия текущей.</param>
public sealed record ObjectVersionDto(
    Guid Id,
    int Version,
    string State,
    string? Name,
    string? Material,
    decimal? UnitMassKg,
    string? SourceReference,
    bool IsCurrent);

/// <summary>
/// Краткие сведения о версии объекта для отображения в списке.
/// </summary>
/// <param name="Id">Идентификатор версии.</param>
/// <param name="Version">Порядковый номер версии.</param>
/// <param name="State">Состояние версии.</param>
/// <param name="IsCurrent">Указывает, является ли версия текущей.</param>
public sealed record ObjectVersionSummaryDto(
    Guid Id,
    int Version,
    string State,
    bool IsCurrent);
