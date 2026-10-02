using MediatR;
using MiniPdm.Modules.Versions.Abstractions;
using MiniPdm.Domain.Versions.Mutations;

namespace MiniPdm.Modules.Versions.Features.Commands.ChangeVersionState;

public sealed class ChangeVersionStateCommandHandler(IVersionMutationService service)
    : IRequestHandler<ChangeVersionStateCommand, VersionMutationResult>
{
    public Task<VersionMutationResult> Handle(ChangeVersionStateCommand request, CancellationToken cancellationToken) =>
        service.ChangeStateAsync(request.ObjectId, request.Version, request.State,
            request.ExpectedConcurrencyToken, cancellationToken);
}
