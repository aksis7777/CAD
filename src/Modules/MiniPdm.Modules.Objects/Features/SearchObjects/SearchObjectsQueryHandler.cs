using MediatR;
using MiniPdm.Contracts.Modules.Objects.DtoModels;
using MiniPdm.Domain.Objects;
using MiniPdm.Domain.Versions;
using MiniPdm.Storage.Abstractions.Objects;

namespace MiniPdm.Modules.Objects.Features.SearchObjects;

public sealed class SearchObjectsQueryHandler(IObjectReadQuery query) : IRequestHandler<SearchObjectsQuery, ObjectSearchPageDto>
{
    public async Task<ObjectSearchPageDto> Handle(SearchObjectsQuery request, CancellationToken cancellationToken)
    {
        var page = await query.SearchAsync(request.Search, request.Offset, request.Limit, cancellationToken);
        return new ObjectSearchPageDto(page.Items.Select(row => new ObjectSearchItemDto(
            row.Id, row.Type.ToString(), row.Designation, row.Name, row.CurrentVersionId, row.VersionNumber,
            row.State?.ToString(),
            row.UnitMassKg, row.ConcurrencyToken, row.NoCurrentVersion)).ToArray(), page.Offset, page.Limit, page.HasMore);
    }
}
