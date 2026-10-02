using MediatR;
using MiniPdm.Contracts.Modules.Objects.DtoModels;
using MiniPdm.Modules.Objects.Services;

namespace MiniPdm.Modules.Objects.Features.Queries.SearchObjects;

public sealed record SearchObjectsQuery(string Search, int Offset, int Limit) : IRequest<ObjectSearchPageDto>;

public sealed class SearchObjectsQueryHandler(ObjectReadService service) : IRequestHandler<SearchObjectsQuery, ObjectSearchPageDto>
{
    public Task<ObjectSearchPageDto> Handle(SearchObjectsQuery request, CancellationToken cancellationToken) =>
        service.SearchObjectsAsync(request.Search, request.Offset, request.Limit, cancellationToken);
}
