namespace MiniPdm.Contracts.Modules.Versions.DtoModels;

/// <summary>Creates a new working version by copying the chosen version while preserving existing history.</summary>
public sealed record CloneVersionRequestDto(int SourceVersion, Guid ExpectedConcurrencyToken);

public sealed record ChangeVersionStateRequestDto(string State, Guid ExpectedConcurrencyToken);
