using MediatR;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Modules.Versions.Abstractions;

namespace MiniPdm.Modules.Versions.Features.Commands.CloneVersion;

public sealed record CloneVersionCommand(Guid ObjectId, int SourceVersion, Guid ExpectedConcurrencyToken)
    : IRequest<VersionMutationResult>;

public sealed class CloneVersionCommandHandler(IVersionMutationService service)
    : IRequestHandler<CloneVersionCommand, VersionMutationResult>
{
    public Task<VersionMutationResult> Handle(CloneVersionCommand request, CancellationToken cancellationToken) =>
        service.CloneAsync(request.ObjectId, request.SourceVersion, request.ExpectedConcurrencyToken, cancellationToken);
}
