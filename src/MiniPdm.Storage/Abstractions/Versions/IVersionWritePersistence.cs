using MiniPdm.Domain.Versions.Mutations;

namespace MiniPdm.Storage.Abstractions.Versions;

public interface IVersionWritePersistence
{
    Task<VersionMutationResult> ExecuteAsync(
        VersionWriteRequest request,
        Func<VersionMutationSnapshot, VersionMutationPlan> prepare,
        CancellationToken ct);
}
