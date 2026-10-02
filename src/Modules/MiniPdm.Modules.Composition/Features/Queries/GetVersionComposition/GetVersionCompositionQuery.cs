using MediatR;
using MiniPdm.Contracts.Modules.Composition.DtoModels;
using MiniPdm.Modules.Composition.Services;

namespace MiniPdm.Modules.Composition.Features.Queries.GetVersionComposition;

/// <summary>
/// Запрашивает состав конкретной версии объекта.
/// Результат сохраняет номер версии и её токен конкурентности.
/// </summary>
/// <param name="ObjectId">Идентификатор объекта.</param>
/// <param name="Version">Положительный номер запрашиваемой версии.</param>
public sealed record GetVersionCompositionQuery(Guid ObjectId, int Version)
    : IRequest<VersionCompositionDto?>;

/// <summary>
/// Получает состав версии через сервис чтения истории состава.
/// </summary>
/// <param name="service">Сервис чтения составов версий.</param>
public sealed class GetVersionCompositionQueryHandler(VersionCompositionReadService service)
    : IRequestHandler<GetVersionCompositionQuery, VersionCompositionDto?>
{
    /// <summary>
    /// Выполняет запрос состава указанной версии.
    /// </summary>
    /// <param name="request">Идентификатор объекта и номер версии.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>Состав версии или <see langword="null"/>, если версия не найдена.</returns>
    public Task<VersionCompositionDto?> Handle(GetVersionCompositionQuery request, CancellationToken cancellationToken) =>
        service.GetVersionCompositionAsync(request.ObjectId, request.Version, cancellationToken);
}
