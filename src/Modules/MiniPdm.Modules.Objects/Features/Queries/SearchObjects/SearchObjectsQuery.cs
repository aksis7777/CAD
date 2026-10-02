using MediatR;
using MiniPdm.Contracts.Modules.Objects.DtoModels;
using MiniPdm.Modules.Objects.Services;

namespace MiniPdm.Modules.Objects.Features.Queries.SearchObjects;

/// <summary>
/// Запрашивает страницу объектов, соответствующих строке поиска.
/// Параметры смещения и размера задают границы страницы.
/// </summary>
/// <param name="Search">Строка поиска по обозначению и имени объекта.</param>
/// <param name="Offset">Число результатов, пропускаемых перед страницей.</param>
/// <param name="Limit">Максимальное число результатов страницы.</param>
public sealed record SearchObjectsQuery(string Search, int Offset, int Limit) : IRequest<ObjectSearchPageDto>;

/// <summary>
/// Выполняет запрос поиска через службу чтения объектов.
/// </summary>
/// <param name="service">Служба поиска и чтения карточек объектов.</param>
public sealed class SearchObjectsQueryHandler(ObjectReadService service) : IRequestHandler<SearchObjectsQuery, ObjectSearchPageDto>
{
    /// <summary>
    /// Ищет объекты и возвращает сформированную страницу результатов.
    /// </summary>
    /// <param name="request">Строка поиска и параметры постраничной выдачи.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>Публичная страница поиска объектов.</returns>
    public Task<ObjectSearchPageDto> Handle(SearchObjectsQuery request, CancellationToken cancellationToken) =>
        service.SearchObjectsAsync(request.Search, request.Offset, request.Limit, cancellationToken);
}
