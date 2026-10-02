using Microsoft.EntityFrameworkCore;
using MiniPdm.Domain.Composition;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;

namespace MiniPdm.Storage.Concurrency;

/// <summary>Projects the flat graph formed by assembly objects' current, non-cancelled versions.</summary>
public static class ActiveCompositionGraphQuery
{
    public static Task<List<CompositionGraphEdge>> LoadAsync(PdmDbContext context, CancellationToken ct) =>
        (from parent in context.Objects.AsNoTracking()
         where parent.Type == PdmObjectType.Assembly && parent.CurrentVersionId != null
         join version in context.Versions.AsNoTracking()
             on new { CurrentVersionId = parent.CurrentVersionId, ObjectId = parent.Id }
             equals new { CurrentVersionId = (Guid?)version.Id, version.ObjectId }
         where version.State != VersionState.Cancelled
         join link in context.BomLinks.AsNoTracking() on version.Id equals link.ParentVersionId
         select new CompositionGraphEdge(parent.Id, link.ChildObjectId))
        .ToListAsync(ct);
}
