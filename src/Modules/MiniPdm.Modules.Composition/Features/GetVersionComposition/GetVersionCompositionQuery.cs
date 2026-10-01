using MediatR;
using MiniPdm.Contracts.Modules.Composition.DtoModels;

namespace MiniPdm.Modules.Composition.Features.GetVersionComposition;

public sealed record GetVersionCompositionQuery(Guid ObjectId, int Version)
    : IRequest<VersionCompositionDto?>;
