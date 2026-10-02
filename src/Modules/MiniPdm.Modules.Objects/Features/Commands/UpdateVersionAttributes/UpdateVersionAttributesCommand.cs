using MediatR;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Modules.Versions.Abstractions;

namespace MiniPdm.Modules.Objects.Features.Commands.UpdateVersionAttributes;

/// <summary>
/// Обновляет изменяемые атрибуты версии объекта.
/// Изменение выполняется только при совпадении ожидаемого токена конкурентности.
/// </summary>
/// <param name="ObjectId">Идентификатор объекта.</param>
/// <param name="Version">Положительный номер изменяемой версии.</param>
/// <param name="Name">Новое наименование версии или <see langword="null"/>, если оно не задано.</param>
/// <param name="Material">Новый материал версии или <see langword="null"/>, если он не задан.</param>
/// <param name="Mass">Новая масса версии или <see langword="null"/>, если она не задана.</param>
/// <param name="ExpectedConcurrencyToken">Токен объекта, прочитанный до отправки команды.</param>
public sealed record UpdateVersionAttributesCommand(Guid ObjectId, int Version, string? Name, string? Material,
    decimal? Mass, Guid ExpectedConcurrencyToken) : IRequest<VersionMutationResult>;

/// <summary>
/// Передаёт обновление атрибутов прикладному сервису мутаций версии.
/// </summary>
/// <param name="service">Сервис проверки и сохранения атрибутов.</param>
public sealed class UpdateVersionAttributesCommandHandler(IVersionMutationService service)
    : IRequestHandler<UpdateVersionAttributesCommand, VersionMutationResult>
{
    /// <summary>
    /// Обновляет атрибуты выбранной версии.
    /// </summary>
    /// <param name="request">Команда с новыми значениями и токеном конкурентности.</param>
    /// <param name="cancellationToken">Токен отмены запроса.</param>
    /// <returns>Итог мутации с новым состоянием или диагностикой отказа.</returns>
    public Task<VersionMutationResult> Handle(UpdateVersionAttributesCommand request, CancellationToken cancellationToken) =>
        service.UpdateAttributesAsync(request.ObjectId, request.Version, request.Name, request.Material, request.Mass,
            request.ExpectedConcurrencyToken, cancellationToken);
}
