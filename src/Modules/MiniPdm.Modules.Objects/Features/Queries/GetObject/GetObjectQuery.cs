using MediatR;
using MiniPdm.Contracts.Modules.Objects.DtoModels;
using MiniPdm.Modules.Objects.Services;

namespace MiniPdm.Modules.Objects.Features.Queries.GetObject;

public sealed record GetObjectQuery(Guid ObjectId, int? VersionNumber) : IRequest<ObjectCardDto?>;

public sealed class GetObjectQueryHandler(ObjectReadService service) : IRequestHandler<GetObjectQuery, ObjectCardDto?>
{
    public Task<ObjectCardDto?> Handle(GetObjectQuery request, CancellationToken cancellationToken) =>
        service.GetObjectAsync(request.ObjectId, request.VersionNumber, cancellationToken);
}
