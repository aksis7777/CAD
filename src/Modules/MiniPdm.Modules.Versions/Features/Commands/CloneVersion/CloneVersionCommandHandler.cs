using MediatR;
using MiniPdm.Modules.Versions.Abstractions;
using MiniPdm.Domain.Versions.Mutations;

namespace MiniPdm.Modules.Versions.Features.Commands.CloneVersion;

public sealed class CloneVersionCommandHandler(IVersionMutationService service)
    : IRequestHandler<CloneVersionCommand, VersionMutationResult>
{
    public Task<VersionMutationResult> Handle(CloneVersionCommand request, CancellationToken cancellationToken) =>
        service.CloneAsync(request.ObjectId, request.SourceVersion, request.ExpectedConcurrencyToken, cancellationToken);
}
