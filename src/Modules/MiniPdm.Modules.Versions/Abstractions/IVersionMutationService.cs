using MiniPdm.Domain.Composition;
using MiniPdm.Domain.Versions;
using MiniPdm.Domain.Versions.Mutations;

namespace MiniPdm.Modules.Versions.Abstractions;

public interface IVersionMutationService
{
    Task<VersionMutationResult> CloneAsync(Guid objectId, int sourceVersion,
        Guid expectedConcurrencyToken, CancellationToken ct);

    Task<VersionMutationResult> ChangeStateAsync(Guid objectId, int version, VersionState state,
        Guid expectedConcurrencyToken, CancellationToken ct);

    Task<VersionMutationResult> UpdateAttributesAsync(Guid objectId, int version, string? name,
        string? material, decimal? mass, Guid expectedConcurrencyToken, CancellationToken ct);

    Task<VersionMutationResult> ReplaceCompositionAsync(Guid objectId, int version,
        IReadOnlyList<CompositionItem> components, Guid expectedConcurrencyToken, CancellationToken ct);
}
