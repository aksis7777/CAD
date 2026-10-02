using MediatR;
using MiniPdm.Contracts.Modules.Composition.DtoModels;
using MiniPdm.Modules.Composition.Services;

namespace MiniPdm.Modules.Composition.Features.Queries.GetComposition;

public sealed class GetCompositionQueryHandler(CompositionReadService service)
    : IRequestHandler<GetCompositionQuery, CompositionTreeDto?>
{
    public Task<CompositionTreeDto?> Handle(GetCompositionQuery request, CancellationToken cancellationToken) =>
        service.GetCompositionAsync(request.ObjectId, cancellationToken);
}
