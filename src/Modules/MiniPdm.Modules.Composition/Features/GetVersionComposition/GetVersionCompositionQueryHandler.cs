using MediatR;
using MiniPdm.Contracts.Modules.Composition.DtoModels;
using MiniPdm.Domain.Objects;
using MiniPdm.Storage.Abstractions.Composition;

namespace MiniPdm.Modules.Composition.Features.GetVersionComposition;

public sealed class GetVersionCompositionQueryHandler(IVersionCompositionReadQuery query)
    : IRequestHandler<GetVersionCompositionQuery, VersionCompositionDto?>
{
    public async Task<VersionCompositionDto?> Handle(GetVersionCompositionQuery request,
        CancellationToken cancellationToken)
    {
        var row = await query.ReadAsync(request.ObjectId, request.Version, cancellationToken);
        if (row is null) return null;

        return new VersionCompositionDto(row.ObjectId, row.Version, row.ConcurrencyToken,
            row.Items.Select(item => new VersionCompositionItemDto(item.ChildObjectId, item.Quantity,
                ToTypeName(item.Type), item.Designation, item.Name, item.NoCurrentVersion)).ToArray());
    }

    private static string ToTypeName(PdmObjectType type) => type switch
    {
        PdmObjectType.Assembly => "Assembly",
        PdmObjectType.Part => "Part",
        PdmObjectType.StandardPart => "StandardPart",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown PDM object type.")
    };
}
