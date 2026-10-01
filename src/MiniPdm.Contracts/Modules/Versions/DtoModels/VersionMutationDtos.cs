namespace MiniPdm.Contracts.Modules.Versions.DtoModels;

/// <summary>Result of a version mutation. Existing version history is preserved, and creating a version may advance the current-version pointer.</summary>
public sealed record VersionMutationDto(Guid ObjectId, Guid VersionId, int VersionNumber, string State,
    Guid? CurrentVersionId, Guid ConcurrencyToken, IReadOnlyList<string> Warnings);

public sealed record VersionMutationErrorDto(string Code, string Message, Guid[]? CyclePath);
