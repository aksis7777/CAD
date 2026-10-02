using MediatR;
using MiniPdm.Contracts.Modules.Composition.DtoModels;
using MiniPdm.Modules.Composition.Services;

namespace MiniPdm.Modules.Composition.Features.Queries.GetVersionComposition;

public sealed class GetVersionCompositionQueryHandler(VersionCompositionReadService service)
    : IRequestHandler<GetVersionCompositionQuery, VersionCompositionDto?>
{
    public Task<VersionCompositionDto?> Handle(GetVersionCompositionQuery request, CancellationToken cancellationToken) =>
        service.GetVersionCompositionAsync(request.ObjectId, request.Version, cancellationToken);
}
