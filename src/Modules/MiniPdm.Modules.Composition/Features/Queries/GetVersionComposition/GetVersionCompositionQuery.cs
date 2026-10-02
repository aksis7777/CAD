using MediatR;
using MiniPdm.Contracts.Modules.Composition.DtoModels;

namespace MiniPdm.Modules.Composition.Features.Queries.GetVersionComposition;

public sealed record GetVersionCompositionQuery(Guid ObjectId, int Version)
    : IRequest<VersionCompositionDto?>;
