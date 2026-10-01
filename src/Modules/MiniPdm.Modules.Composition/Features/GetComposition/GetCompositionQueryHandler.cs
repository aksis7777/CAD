using MediatR;
using MiniPdm.Contracts.Modules.Composition.DtoModels;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using MiniPdm.Storage.Abstractions.Composition;

namespace MiniPdm.Modules.Composition.Features.GetComposition;

public sealed class GetCompositionQueryHandler(ICompositionReadQuery compositionReadQuery)
    : IRequestHandler<GetCompositionQuery, CompositionTreeDto?>
{
    public async Task<CompositionTreeDto?> Handle(GetCompositionQuery request, CancellationToken cancellationToken)
    {
        var occurrences = await compositionReadQuery.ReadAsync(request.ObjectId, cancellationToken);
        if (occurrences.Count == 0) return null;

        var nodes = occurrences.Select(occurrence =>
        {
            var (errorCode, error) = GetDiagnostic(occurrence);
            return new CompositionNodeDto(
                occurrence.ObjectId,
                occurrence.ObjectPath,
                occurrence.ParentPath,
                occurrence.LocalQuantity,
                ToTypeName(occurrence.Type),
                occurrence.Designation,
                occurrence.Name,
                occurrence.Material,
                occurrence.VersionId,
                occurrence.VersionNumber,
                occurrence.State is VersionState state ? ToStateName(state) : null,
                occurrence.UnitMassKg,
                errorCode,
                error);
        }).ToArray();

        return new CompositionTreeDto(request.ObjectId, nodes);
    }

    private static (string? ErrorCode, string? Error) GetDiagnostic(CompositionOccurrence occurrence)
    {
        if (occurrence.IsCycle)
        {
            var path = string.Join(" → ", occurrence.ObjectPath);
            return ("Cycle", $"Composition cycle detected along path {path}.");
        }

        if (occurrence.VersionId is null)
            return ("NoCurrentVersion", "The object has no current non-cancelled version.");

        return (null, null);
    }

    private static string ToTypeName(PdmObjectType type) => type switch
    {
        PdmObjectType.Assembly => "Assembly",
        PdmObjectType.Part => "Part",
        PdmObjectType.StandardPart => "StandardPart",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown PDM object type.")
    };

    private static string ToStateName(VersionState state) => state switch
    {
        VersionState.InWork => "InWork",
        VersionState.Approved => "Approved",
        VersionState.Cancelled => "Cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown version state.")
    };
}
