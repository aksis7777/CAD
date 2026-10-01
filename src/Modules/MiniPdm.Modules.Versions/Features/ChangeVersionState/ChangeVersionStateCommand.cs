using MediatR;
using MiniPdm.Domain.Versions;
using MiniPdm.Domain.Versions.Mutations;

namespace MiniPdm.Modules.Versions.Features.ChangeVersionState;

public sealed record ChangeVersionStateCommand(Guid ObjectId, int Version, VersionState State,
    Guid ExpectedConcurrencyToken) : IRequest<VersionMutationResult>;
