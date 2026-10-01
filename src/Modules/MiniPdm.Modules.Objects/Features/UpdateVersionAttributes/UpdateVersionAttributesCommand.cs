using MediatR;
using MiniPdm.Domain.Versions.Mutations;

namespace MiniPdm.Modules.Objects.Features.UpdateVersionAttributes;

public sealed record UpdateVersionAttributesCommand(Guid ObjectId, int Version, string? Name, string? Material,
    decimal? Mass, Guid ExpectedConcurrencyToken) : IRequest<VersionMutationResult>;
