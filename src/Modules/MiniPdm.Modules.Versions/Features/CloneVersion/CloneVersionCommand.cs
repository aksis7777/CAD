using MediatR;
using MiniPdm.Domain.Versions.Mutations;

namespace MiniPdm.Modules.Versions.Features.CloneVersion;

public sealed record CloneVersionCommand(Guid ObjectId, int SourceVersion, Guid ExpectedConcurrencyToken)
    : IRequest<VersionMutationResult>;
