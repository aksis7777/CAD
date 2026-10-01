using MediatR;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Storage.Abstractions.Versions;

namespace MiniPdm.Modules.Composition.Features.ReplaceComposition;

public sealed class ReplaceCompositionCommandHandler(IVersionWritePersistence persistence)
    : IRequestHandler<ReplaceCompositionCommand, VersionMutationResult>
{
    public Task<VersionMutationResult> Handle(ReplaceCompositionCommand request, CancellationToken cancellationToken) =>
        persistence.ExecuteAsync(
            new VersionWriteRequest(request.ObjectId, request.Version, request.ExpectedConcurrencyToken,
                request.Components.Select(x => x.ChildObjectId).Distinct().ToArray()),
            snapshot => VersionMutationPlanner.ReplaceComposition(snapshot, request.Components),
            cancellationToken);
}
