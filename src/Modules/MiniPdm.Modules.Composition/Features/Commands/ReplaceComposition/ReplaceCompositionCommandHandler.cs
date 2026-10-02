using MediatR;
using MiniPdm.Modules.Versions.Abstractions;
using MiniPdm.Domain.Versions.Mutations;

namespace MiniPdm.Modules.Composition.Features.Commands.ReplaceComposition;

public sealed class ReplaceCompositionCommandHandler(IVersionMutationService service)
    : IRequestHandler<ReplaceCompositionCommand, VersionMutationResult>
{
    public Task<VersionMutationResult> Handle(ReplaceCompositionCommand request, CancellationToken cancellationToken) =>
        service.ReplaceCompositionAsync(request.ObjectId, request.Version, request.Components,
            request.ExpectedConcurrencyToken, cancellationToken);
}
