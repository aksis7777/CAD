using MediatR;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Storage.Abstractions.Versions;

namespace MiniPdm.Modules.Versions.Features.CloneVersion;

public sealed class CloneVersionCommandHandler(IVersionWritePersistence persistence)
    : IRequestHandler<CloneVersionCommand, VersionMutationResult>
{
    public Task<VersionMutationResult> Handle(CloneVersionCommand request, CancellationToken cancellationToken) =>
        persistence.ExecuteAsync(
            new VersionWriteRequest(request.ObjectId, request.SourceVersion, request.ExpectedConcurrencyToken, []),
            VersionMutationPlanner.Clone,
            cancellationToken);
}
