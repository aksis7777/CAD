using MediatR;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Domain.Versions;
using MiniPdm.Modules.Versions.Abstractions;

namespace MiniPdm.Modules.Versions.Features.Commands.ChangeVersionState;

public sealed record ChangeVersionStateCommand(Guid ObjectId, int Version, VersionState State,
    Guid ExpectedConcurrencyToken) : IRequest<VersionMutationResult>;

public sealed class ChangeVersionStateCommandHandler(IVersionMutationService service)
    : IRequestHandler<ChangeVersionStateCommand, VersionMutationResult>
{
    public Task<VersionMutationResult> Handle(ChangeVersionStateCommand request, CancellationToken cancellationToken) =>
        service.ChangeStateAsync(request.ObjectId, request.Version, request.State,
            request.ExpectedConcurrencyToken, cancellationToken);
}
