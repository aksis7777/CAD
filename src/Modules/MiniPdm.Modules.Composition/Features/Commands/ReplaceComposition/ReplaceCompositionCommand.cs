using MediatR;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Modules.Versions.Abstractions;

namespace MiniPdm.Modules.Composition.Features.Commands.ReplaceComposition;

public sealed record ReplaceCompositionCommand(Guid ObjectId, int Version, IReadOnlyList<CompositionItem> Components,
    Guid ExpectedConcurrencyToken) : IRequest<VersionMutationResult>;

public sealed class ReplaceCompositionCommandHandler(IVersionMutationService service)
    : IRequestHandler<ReplaceCompositionCommand, VersionMutationResult>
{
    public Task<VersionMutationResult> Handle(ReplaceCompositionCommand request, CancellationToken cancellationToken) =>
        service.ReplaceCompositionAsync(request.ObjectId, request.Version, request.Components,
            request.ExpectedConcurrencyToken, cancellationToken);
}
