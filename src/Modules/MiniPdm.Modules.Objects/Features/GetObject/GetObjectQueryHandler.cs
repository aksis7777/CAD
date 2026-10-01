using MediatR;
using MiniPdm.Contracts.Modules.Objects.DtoModels;
using MiniPdm.Domain.Objects;
using MiniPdm.Storage.Abstractions.Objects;

namespace MiniPdm.Modules.Objects.Features.GetObject;

public sealed class GetObjectQueryHandler(IObjectReadQuery query) : IRequestHandler<GetObjectQuery, ObjectCardDto?>
{
    public async Task<ObjectCardDto?> Handle(GetObjectQuery request, CancellationToken cancellationToken)
    {
        var row = await query.GetAsync(request.ObjectId, request.VersionNumber, cancellationToken);
        if (row is null) return null;
        if (request.VersionNumber.HasValue && row.SelectedVersion is null) return null;

        var selected = row.SelectedVersion;
        var name = row.Type == PdmObjectType.StandardPart ? row.StandardName : selected?.Name;
        var current = row.CurrentVersionId;
        var selectedDto = selected is null ? null : new ObjectVersionDto(
            selected.Id, selected.Version, selected.State.ToString(), row.Type == PdmObjectType.StandardPart ? row.StandardName : selected.Name, selected.Material,
            row.Type == PdmObjectType.Assembly ? null : selected.Mass, selected.SourceReference, selected.Id == current);
        var versions = row.Versions.Select(v => new ObjectVersionSummaryDto(
            v.Id, v.Version, v.State.ToString(), v.Id == current)).ToArray();
        var noCurrent = current is null;

        return new ObjectCardDto(row.Id, row.Type.ToString(), row.Designation, name, current,
            row.ConcurrencyToken, selectedDto, versions,
            noCurrent && request.VersionNumber is null ? "NoCurrentVersion" : null,
            noCurrent && request.VersionNumber is null ? "The object has no current non-cancelled version." : null);
    }
}
