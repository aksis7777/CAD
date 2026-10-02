using MediatR;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Modules.Versions.Abstractions;

namespace MiniPdm.Modules.Objects.Features.Commands.UpdateVersionAttributes;

public sealed record UpdateVersionAttributesCommand(Guid ObjectId, int Version, string? Name, string? Material,
    decimal? Mass, Guid ExpectedConcurrencyToken) : IRequest<VersionMutationResult>;

public sealed class UpdateVersionAttributesCommandHandler(IVersionMutationService service)
    : IRequestHandler<UpdateVersionAttributesCommand, VersionMutationResult>
{
    public Task<VersionMutationResult> Handle(UpdateVersionAttributesCommand request, CancellationToken cancellationToken) =>
        service.UpdateAttributesAsync(request.ObjectId, request.Version, request.Name, request.Material, request.Mass,
            request.ExpectedConcurrencyToken, cancellationToken);
}
