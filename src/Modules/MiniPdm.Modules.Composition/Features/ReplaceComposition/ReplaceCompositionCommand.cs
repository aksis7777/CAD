using MediatR;
using MiniPdm.Domain.Versions.Mutations;

namespace MiniPdm.Modules.Composition.Features.ReplaceComposition;

public sealed record ReplaceCompositionCommand(Guid ObjectId, int Version, IReadOnlyList<CompositionItem> Components,
    Guid ExpectedConcurrencyToken) : IRequest<VersionMutationResult>;
