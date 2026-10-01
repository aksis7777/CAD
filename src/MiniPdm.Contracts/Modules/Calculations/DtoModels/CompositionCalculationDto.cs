namespace MiniPdm.Contracts.Modules.Calculations.DtoModels;

/// <summary>Calculated assembly mass and flat parts specification. Masses are in kilograms; quantities are pieces.</summary>
/// <param name="IsComplete">False when diagnostics make the assembly total mass incomplete or unknown.</param>
public sealed record CompositionCalculationDto(
    Guid RootObjectId,
    decimal? TotalMassKg,
    bool IsComplete,
    IReadOnlyList<SpecificationItemDto> Items,
    IReadOnlyList<CalculationDiagnosticDto> Diagnostics);

/// <summary>A specification row; quantities are whole pieces and masses are kilograms. Null values mean unknown.</summary>
public sealed record SpecificationItemDto(
    Guid ObjectId,
    string Type,
    string? Designation,
    string? Name,
    string? Material,
    Guid? VersionId,
    int? VersionNumber,
    decimal? Quantity,
    decimal? UnitMassKg,
    decimal? TotalMassKg);

public sealed record CalculationDiagnosticDto(string Code, Guid ObjectId, Guid[] ObjectPath, string Message);
