using MediatR;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Storage.Abstractions.Versions;

namespace MiniPdm.Modules.Versions.Features.ChangeVersionState;

public sealed class ChangeVersionStateCommandHandler(IVersionWritePersistence persistence)
    : IRequestHandler<ChangeVersionStateCommand, VersionMutationResult>
{
    public Task<VersionMutationResult> Handle(ChangeVersionStateCommand request, CancellationToken cancellationToken) =>
        persistence.ExecuteAsync(
            new VersionWriteRequest(request.ObjectId, request.Version, request.ExpectedConcurrencyToken, []),
            snapshot => VersionMutationPlanner.ChangeState(snapshot, request.State),
            cancellationToken);
}
