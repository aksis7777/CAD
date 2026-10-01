using MediatR;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Storage.Abstractions.Versions;

namespace MiniPdm.Modules.Objects.Features.UpdateVersionAttributes;

public sealed class UpdateVersionAttributesCommandHandler(IVersionWritePersistence persistence)
    : IRequestHandler<UpdateVersionAttributesCommand, VersionMutationResult>
{
    public Task<VersionMutationResult> Handle(UpdateVersionAttributesCommand request, CancellationToken cancellationToken) =>
        persistence.ExecuteAsync(
            new VersionWriteRequest(request.ObjectId, request.Version, request.ExpectedConcurrencyToken, []),
            snapshot => VersionMutationPlanner.UpdateAttributes(snapshot, request.Name, request.Material, request.Mass),
            cancellationToken);
}
