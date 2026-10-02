using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Modules.Objects.DtoModels;

/// <summary>
/// Строка внутренней проекции поиска объектов.
/// Содержит поля карточки, необходимые для выдачи страницы результатов.
/// </summary>
/// <param name="Id">Идентификатор объекта.</param>
/// <param name="Type">Тип объекта PDM.</param>
/// <param name="Designation">Обозначение сборки или детали.</param>
/// <param name="Name">Отображаемое наименование объекта.</param>
/// <param name="CurrentVersionId">Идентификатор текущей версии, если она есть.</param>
/// <param name="VersionNumber">Номер текущей версии, если она есть.</param>
/// <param name="State">Состояние текущей версии, если она есть.</param>
/// <param name="UnitMassKg">Масса единицы объекта в килограммах, если известна.</param>
/// <param name="ConcurrencyToken">Токен для обнаружения устаревших изменений.</param>
/// <param name="NoCurrentVersion">Показывает, что у объекта нет текущей версии.</param>
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
    bool NoCurrentVersion);

/// <summary>
/// Страница внутренней проекции поиска объектов.
/// Метаданные позволяют продолжить постраничную загрузку результатов.
/// </summary>
/// <param name="Items">Строки объектов на текущей странице.</param>
/// <param name="Offset">Смещение первой строки в полном результате.</param>
/// <param name="Limit">Максимальное число строк страницы.</param>
/// <param name="HasMore">Указывает, что после страницы есть дополнительные строки.</param>
public sealed record ObjectSearchPage(IReadOnlyList<ObjectSearchRow> Items, int Offset, int Limit, bool HasMore);

/// <summary>
/// Внутренняя проекция одной версии для карточки объекта.
/// Включает атрибуты версии и ссылку на исходный файл.
/// </summary>
/// <param name="Id">Идентификатор записи версии.</param>
/// <param name="Version">Порядковый номер версии внутри объекта.</param>
/// <param name="State">Состояние версии.</param>
/// <param name="Name">Наименование, сохранённое в версии.</param>
/// <param name="Material">Материал версии, если задан.</param>
/// <param name="Mass">Масса версии, если задана.</param>
/// <param name="SourceReference">Ссылка на импортированный исходный файл.</param>
public sealed record ObjectVersionReadRow(
    Guid Id,
    int Version,
    VersionState State,
    string? Name,
    string? Material,
    decimal? Mass,
    string? SourceReference);

/// <summary>
/// Внутренняя проекция карточки объекта и списка его версий.
/// Выбранная версия отсутствует, если её нет или она не была запрошена.
/// </summary>
/// <param name="Id">Идентификатор объекта.</param>
/// <param name="Type">Тип объекта PDM.</param>
/// <param name="Designation">Обозначение сборки или детали.</param>
/// <param name="StandardName">Исходное имя стандартного изделия.</param>
/// <param name="CurrentVersionId">Идентификатор текущей версии, если она есть.</param>
/// <param name="ConcurrencyToken">Токен конкурентности объекта.</param>
/// <param name="Versions">Краткие сведения обо всех версиях объекта.</param>
/// <param name="SelectedVersion">Полные данные запрошенной или текущей версии.</param>
public sealed record ObjectCardReadRow(
    Guid Id,
    PdmObjectType Type,
    string? Designation,
    string? StandardName,
    Guid? CurrentVersionId,
    Guid ConcurrencyToken,
    IReadOnlyList<ObjectVersionSummaryReadRow> Versions,
    ObjectVersionReadRow? SelectedVersion);

/// <summary>
/// Краткая внутренняя проекция версии для списка в карточке объекта.
/// </summary>
/// <param name="Id">Идентификатор записи версии.</param>
/// <param name="Version">Порядковый номер версии.</param>
/// <param name="State">Состояние версии.</param>
public sealed record ObjectVersionSummaryReadRow(Guid Id, int Version, VersionState State);
