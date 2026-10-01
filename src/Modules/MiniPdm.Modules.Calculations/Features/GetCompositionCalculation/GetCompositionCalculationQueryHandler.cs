using MediatR;
using MiniPdm.Contracts.Modules.Calculations.DtoModels;
using MiniPdm.Domain.Calculations;
using MiniPdm.Storage.Abstractions.Composition;

namespace MiniPdm.Modules.Calculations.Features.GetCompositionCalculation;

public sealed class GetCompositionCalculationQueryHandler(ICompositionReadQuery compositionReadQuery)
    : IRequestHandler<GetCompositionCalculationQuery, CompositionCalculationDto?>
{
    public async Task<CompositionCalculationDto?> Handle(GetCompositionCalculationQuery request, CancellationToken cancellationToken)
    {
        var occurrences = await compositionReadQuery.ReadAsync(request.ObjectId, cancellationToken);
        if (occurrences.Count == 0) return null;

        var inputs = occurrences.Select(occurrence => new CompositionCalculationInput(
            occurrence.ObjectId,
            occurrence.ObjectPath,
            occurrence.ParentPath,
            occurrence.LocalQuantity,
            occurrence.Type,
            occurrence.Designation,
            occurrence.Name,
            occurrence.Material,
            occurrence.VersionId,
            occurrence.VersionNumber,
            occurrence.State,
            occurrence.UnitMassKg,
            occurrence.IsCycle));
        var result = CompositionCalculator.Calculate(request.ObjectId, inputs);

        return new CompositionCalculationDto(result.RootObjectId, result.TotalMassKg, result.IsComplete,
            result.Items.Select(item => new SpecificationItemDto(item.ObjectId, item.Type.ToString(),
                item.Designation, item.Name, item.Material, item.VersionId, item.VersionNumber,
                item.Quantity, item.UnitMassKg, item.TotalMassKg)).ToArray(),
            result.Diagnostics.Select(diagnostic => new CalculationDiagnosticDto(diagnostic.Code,
                diagnostic.ObjectId, diagnostic.ObjectPath.ToArray(), diagnostic.Message)).ToArray());
    }
}
