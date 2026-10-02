using MiniPdm.Contracts.Modules.Calculations.DtoModels;
using MiniPdm.Domain.Calculations;
using MiniPdm.Modules.Composition.DtoModels;
using MiniPdm.Modules.Composition.Services;

namespace MiniPdm.Modules.Calculations.Services;

public sealed class CompositionCalculationService(CompositionReadService compositionReadService)
{
    public async Task<CompositionCalculationDto?> GetCalculationAsync(Guid objectId, CancellationToken cancellationToken)
    {
        var occurrences = await compositionReadService.ReadAsync(objectId, cancellationToken);
        return occurrences.Count == 0 ? null : MapCalculationDto(objectId, occurrences);
    }

    public static CompositionCalculationDto MapCalculationDto(Guid objectId, IReadOnlyList<CompositionOccurrence> occurrences)
    {
        var inputs = occurrences.Select(occurrence => new CompositionCalculationInput(
            occurrence.ObjectId, occurrence.ObjectPath, occurrence.ParentPath, occurrence.LocalQuantity,
            occurrence.Type, occurrence.Designation, occurrence.Name, occurrence.Material, occurrence.VersionId,
            occurrence.VersionNumber, occurrence.State, occurrence.UnitMassKg, occurrence.IsCycle));
        var result = CompositionCalculator.Calculate(objectId, inputs);
        return new CompositionCalculationDto(result.RootObjectId, result.TotalMassKg, result.IsComplete,
            result.Items.Select(item => new SpecificationItemDto(item.ObjectId, item.Type.ToString(),
                item.Designation, item.Name, item.Material, item.VersionId, item.VersionNumber, item.Quantity,
                item.UnitMassKg, item.TotalMassKg)).ToArray(),
            result.Diagnostics.Select(diagnostic => new CalculationDiagnosticDto(diagnostic.Code,
                diagnostic.ObjectId, diagnostic.ObjectPath.ToArray(), diagnostic.Message)).ToArray());
    }
}
