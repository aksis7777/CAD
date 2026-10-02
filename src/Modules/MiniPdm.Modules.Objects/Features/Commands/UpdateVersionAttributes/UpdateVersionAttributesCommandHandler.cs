using MediatR;
using MiniPdm.Modules.Versions.Abstractions;
using MiniPdm.Domain.Versions.Mutations;

namespace MiniPdm.Modules.Objects.Features.Commands.UpdateVersionAttributes;

public sealed class UpdateVersionAttributesCommandHandler(IVersionMutationService service)
    : IRequestHandler<UpdateVersionAttributesCommand, VersionMutationResult>
{
    public Task<VersionMutationResult> Handle(UpdateVersionAttributesCommand request, CancellationToken cancellationToken) =>
        service.UpdateAttributesAsync(request.ObjectId, request.Version, request.Name, request.Material, request.Mass,
            request.ExpectedConcurrencyToken, cancellationToken);
}
