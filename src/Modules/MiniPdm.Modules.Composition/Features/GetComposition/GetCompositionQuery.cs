using MediatR;
using MiniPdm.Contracts.Modules.Composition.DtoModels;

namespace MiniPdm.Modules.Composition.Features.GetComposition;

public sealed record GetCompositionQuery(Guid ObjectId) : IRequest<CompositionTreeDto?>;
