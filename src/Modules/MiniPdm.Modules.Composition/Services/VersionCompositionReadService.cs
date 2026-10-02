using Microsoft.EntityFrameworkCore;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using MiniPdm.Modules.Composition.DtoModels;
using MiniPdm.Contracts.Modules.Composition.DtoModels;
using MiniPdm.Storage;

namespace MiniPdm.Modules.Composition.Services;

public sealed class VersionCompositionReadService(PdmDbContext context)
{
    public async Task<VersionCompositionDto?> GetVersionCompositionAsync(Guid objectId, int version,
        CancellationToken cancellationToken)
    {
        var row = await ReadAsync(objectId, version, cancellationToken);
        return row is null ? null : new VersionCompositionDto(row.ObjectId, row.Version, row.ConcurrencyToken,
            row.Items.Select(item => new VersionCompositionItemDto(item.ChildObjectId, item.Quantity,
                TypeName(item.Type), item.Designation, item.Name, item.NoCurrentVersion)).ToArray());
    }

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

    private static string TypeName(PdmObjectType type) => type switch
    {
        PdmObjectType.Assembly => "Assembly",
        PdmObjectType.Part => "Part",
        PdmObjectType.StandardPart => "StandardPart",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown PDM object type.")
    };

    private sealed record VersionCompositionProjection(Guid ObjectId, int Version, Guid ConcurrencyToken,
        List<VersionCompositionItemProjection> Items);

    private sealed record VersionCompositionItemProjection(Guid ChildObjectId, int Quantity, PdmObjectType Type,
        string? Designation, string? Name, bool NoCurrentVersion);
}
