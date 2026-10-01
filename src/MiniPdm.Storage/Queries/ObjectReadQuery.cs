using Microsoft.EntityFrameworkCore;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using MiniPdm.Storage.Abstractions.Objects;

namespace MiniPdm.Storage.Queries;

public sealed class ObjectReadQuery(PdmDbContext context) : IObjectReadQuery
{
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
