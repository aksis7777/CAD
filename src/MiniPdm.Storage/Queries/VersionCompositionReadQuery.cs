using Microsoft.EntityFrameworkCore;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using MiniPdm.Storage.Abstractions.Composition;

namespace MiniPdm.Storage.Queries;

public sealed class VersionCompositionReadQuery(PdmDbContext context) : IVersionCompositionReadQuery
{
    public async Task<VersionCompositionReadRow?> ReadAsync(Guid objectId, int version,
        CancellationToken cancellationToken)
    {
        var row = await context.Versions
            .AsNoTracking()
            .AsSingleQuery()
            .Where(x => x.ObjectId == objectId && x.Version == version)
            .Select(x => new VersionCompositionProjection(
                x.ObjectId,
                x.Version,
                x.Object!.ConcurrencyToken,
                x.Components.OrderBy(link => link.ChildObjectId)
                    .Select(link => new VersionCompositionItemProjection(
                        link.ChildObjectId,
                        link.Quantity,
                        link.ChildObject!.Type,
                        link.ChildObject.Designation,
                        link.ChildObject.Type == PdmObjectType.StandardPart
                            ? link.ChildObject.StandardName
                            : link.ChildObject.Versions
                                .Where(childVersion => childVersion.Id == link.ChildObject.CurrentVersionId &&
                                    childVersion.State != VersionState.Cancelled)
                                .Select(childVersion => childVersion.Name)
                                .FirstOrDefault(),
                        !link.ChildObject.Versions.Any(childVersion =>
                            childVersion.Id == link.ChildObject.CurrentVersionId &&
                            childVersion.State != VersionState.Cancelled)))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new VersionCompositionReadRow(row.ObjectId, row.Version, row.ConcurrencyToken,
                row.Items.Select(item => new VersionCompositionItemReadRow(item.ChildObjectId, item.Quantity,
                    item.Type, item.Designation, item.Name, item.NoCurrentVersion)).ToArray());
    }

    private sealed record VersionCompositionProjection(Guid ObjectId, int Version, Guid ConcurrencyToken,
        List<VersionCompositionItemProjection> Items);

    private sealed record VersionCompositionItemProjection(Guid ChildObjectId, int Quantity, PdmObjectType Type,
        string? Designation, string? Name, bool NoCurrentVersion);
}
