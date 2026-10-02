using Microsoft.EntityFrameworkCore;
using MiniPdm.Contracts.Modules.Objects.DtoModels;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using MiniPdm.Modules.Objects.DtoModels;
using MiniPdm.Storage;

namespace MiniPdm.Modules.Objects.Services;

/// <summary>
/// Читает страницы поиска и карточки объектов через проекции хранилища.
/// Сервис преобразует внутренние строки данных в публичные контракты API.
/// </summary>
/// <param name="context">Контекст базы данных объектов и версий.</param>
public sealed class ObjectReadService(PdmDbContext context)
{
    /// <summary>
    /// Выполняет поиск объектов и формирует страницу публичного ответа.
    /// </summary>
    /// <param name="search">Подстрока для поиска по обозначению и имени.</param>
    /// <param name="offset">Число строк, пропускаемых перед страницей.</param>
    /// <param name="limit">Максимальное число объектов в странице.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>Страница результатов поиска с метаданными постраничной выдачи.</returns>
    public async Task<ObjectSearchPageDto> SearchObjectsAsync(string search, int offset, int limit, CancellationToken cancellationToken)
    {
        var page = await SearchAsync(search, offset, limit, cancellationToken);
        return new ObjectSearchPageDto(page.Items.Select(row => new ObjectSearchItemDto(
            row.Id, row.Type.ToString(), row.Designation, row.Name, row.CurrentVersionId, row.VersionNumber,
            row.State?.ToString(), row.UnitMassKg, row.ConcurrencyToken, row.NoCurrentVersion)).ToArray(),
            page.Offset, page.Limit, page.HasMore);
    }

    /// <summary>
    /// Получает публичную карточку объекта и выбранной версии.
    /// </summary>
    /// <param name="objectId">Идентификатор объекта.</param>
    /// <param name="versionNumber">Номер версии для просмотра или <see langword="null"/> для текущей версии.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>Карточка объекта либо <see langword="null"/>, если объект или выбранная версия не найдены.</returns>
    public async Task<ObjectCardDto?> GetObjectAsync(Guid objectId, int? versionNumber, CancellationToken cancellationToken)
    {
        var row = await GetAsync(objectId, versionNumber, cancellationToken);
        if (row is null || (versionNumber.HasValue && row.SelectedVersion is null))
            return null;
        var selected = row.SelectedVersion;
        var name = row.Type == PdmObjectType.StandardPart ? row.StandardName : selected?.Name;
        var current = row.CurrentVersionId;
        var selectedDto = selected is null ? null : new ObjectVersionDto(selected.Id, selected.Version,
            selected.State.ToString(), row.Type == PdmObjectType.StandardPart ? row.StandardName : selected.Name,
            selected.Material, row.Type == PdmObjectType.Assembly ? null : selected.Mass,
            selected.SourceReference, selected.Id == current);
        var versions = row.Versions.Select(v => new ObjectVersionSummaryDto(v.Id, v.Version,
            v.State.ToString(), v.Id == current)).ToArray();
        var noCurrent = current is null;
        return new ObjectCardDto(row.Id, row.Type.ToString(), row.Designation, name, current,
            row.ConcurrencyToken, selectedDto, versions,
            noCurrent && versionNumber is null ? "NoCurrentVersion" : null,
            noCurrent && versionNumber is null ? "The object has no current non-cancelled version." : null);
    }

    /// <summary>
    /// Ищет объекты и возвращает внутреннюю страницу проекций.
    /// Запрос выбирает не более <paramref name="limit"/> строк и определяет наличие следующей страницы.
    /// </summary>
    /// <param name="search">Подстрока для поиска по обозначению и имени.</param>
    /// <param name="offset">Число строк, пропускаемых перед страницей.</param>
    /// <param name="limit">Максимальное число строк страницы.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>Внутренняя страница строк объектов.</returns>
    public async Task<ObjectSearchPage> SearchAsync(string search, int offset, int limit, CancellationToken cancellationToken)
    {
        var query = context.Objects.AsNoTracking();
        if (search.Length > 0)
        {
            if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                var folded = search.ToLowerInvariant();
                query = query.Where(o =>
                    (o.Designation != null && o.Designation.ToLower().Contains(folded)) ||
                    (o.StandardName != null && o.StandardName.ToLower().Contains(folded)) ||
                    o.Versions.Any(v => v.Id == o.CurrentVersionId && v.State != VersionState.Cancelled && v.Name != null && v.Name.ToLower().Contains(folded)));
            }
            else
            {
                var pattern = $"%{EscapeLike(search)}%";
                query = query.Where(o =>
                    (o.Designation != null && EF.Functions.ILike(o.Designation, pattern, "\\")) ||
                    (o.StandardName != null && EF.Functions.ILike(o.StandardName, pattern, "\\")) ||
                    o.Versions.Any(v => v.Id == o.CurrentVersionId && v.State != VersionState.Cancelled && v.Name != null && EF.Functions.ILike(v.Name, pattern, "\\")));
            }
        }

        var rows = await query
            .OrderBy(o => o.Designation ?? o.StandardName ?? string.Empty)
            .ThenBy(o => o.Id)
            .Skip(offset)
            .Take(limit + 1)
            .Select(o => new ObjectSearchProjection(
                o.Id,
                o.Type,
                o.Designation,
                o.StandardName,
                o.ConcurrencyToken,
                o.Versions
                    .Where(v => v.Id == o.CurrentVersionId && v.State != VersionState.Cancelled)
                    .Select(v => new CurrentVersionProjection(v.Id, v.Version, v.State, v.Name, v.Mass))
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        var hasMore = rows.Count > limit;
        var items = rows.Take(limit).Select(row => new ObjectSearchRow(
            row.Id,
            row.Type,
            row.Designation,
            row.Type == PdmObjectType.StandardPart ? row.StandardName : row.CurrentVersion?.Name,
            row.CurrentVersion?.Id,
            row.CurrentVersion?.Version,
            row.CurrentVersion?.State,
            row.Type == PdmObjectType.Assembly ? null : row.CurrentVersion?.Mass,
            row.ConcurrencyToken,
            row.CurrentVersion is null)).ToArray();

        return new ObjectSearchPage(items, offset, limit, hasMore);
    }

    /// <summary>
    /// Читает внутреннюю проекцию карточки объекта и его версий.
    /// </summary>
    /// <param name="objectId">Идентификатор объекта.</param>
    /// <param name="versionNumber">Номер версии для чтения или <see langword="null"/> для текущей версии.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>Проекция карточки либо <see langword="null"/>, если объект не найден.</returns>
    public async Task<ObjectCardReadRow?> GetAsync(Guid objectId, int? versionNumber, CancellationToken cancellationToken)
    {
        var row = await context.Objects
            .AsNoTracking()
            .AsSingleQuery()
            .Where(o => o.Id == objectId)
            .Select(o => new ObjectCardProjection(
                o.Id,
                o.Type,
                o.Designation,
                o.StandardName,
                o.Versions
                    .Where(v => v.Id == o.CurrentVersionId && v.State != VersionState.Cancelled)
                    .Select(v => (Guid?)v.Id)
                    .FirstOrDefault(),
                o.ConcurrencyToken,
                o.Versions.OrderByDescending(v => v.Version)
                    .Select(v => new VersionSummaryProjection(v.Id, v.Version, v.State))
                    .ToList(),
                o.Versions.Where(v => versionNumber.HasValue
                        ? v.Version == versionNumber.Value
                        : v.Id == o.CurrentVersionId && v.State != VersionState.Cancelled)
                    .Select(v => new VersionProjection(v.Id, v.Version, v.State, v.Name, v.Material, v.Mass, v.SourceReference))
                    .FirstOrDefault()))
            .SingleOrDefaultAsync(cancellationToken);

        return row is null ? null : new ObjectCardReadRow(
            row.Id,
            row.Type,
            row.Designation,
            row.StandardName,
            row.CurrentVersionId,
            row.ConcurrencyToken,
            row.Versions.Select(v => new ObjectVersionSummaryReadRow(v.Id, v.Version, v.State)).ToArray(),
            row.SelectedVersion is null ? null : new ObjectVersionReadRow(row.SelectedVersion.Id, row.SelectedVersion.Version, row.SelectedVersion.State,
                row.SelectedVersion.Name, row.SelectedVersion.Material, row.SelectedVersion.Mass, row.SelectedVersion.SourceReference));
    }

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed record CurrentVersionProjection(Guid Id, int Version, VersionState State, string? Name, decimal? Mass);
    private sealed record ObjectSearchProjection(Guid Id, PdmObjectType Type, string? Designation, string? StandardName, Guid ConcurrencyToken, CurrentVersionProjection? CurrentVersion);
    private sealed record VersionProjection(Guid Id, int Version, VersionState State, string? Name, string? Material, decimal? Mass, string? SourceReference);
    private sealed record VersionSummaryProjection(Guid Id, int Version, VersionState State);
    private sealed record ObjectCardProjection(Guid Id, PdmObjectType Type, string? Designation, string? StandardName, Guid? CurrentVersionId, Guid ConcurrencyToken,
        List<VersionSummaryProjection> Versions, VersionProjection? SelectedVersion);
}
