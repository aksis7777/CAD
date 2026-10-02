using MediatR;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Modules.Versions.Abstractions;

namespace MiniPdm.Modules.Versions.Features.Commands.CloneVersion;

/// <summary>
/// Запрашивает создание новой версии на основе выбранной версии объекта.
/// </summary>
/// <param name="ObjectId">Идентификатор объекта.</param>
/// <param name="SourceVersion">Номер версии-источника.</param>
/// <param name="ExpectedConcurrencyToken">Токен объекта, прочитанный клиентом до изменения.</param>
public sealed record CloneVersionCommand(Guid ObjectId, int SourceVersion, Guid ExpectedConcurrencyToken)
    : IRequest<VersionMutationResult>;

/// <summary>
/// Передаёт запрос клонирования сервису мутаций версий.
/// </summary>
/// <param name="service">Сервис выполнения мутаций версии.</param>
public sealed class CloneVersionCommandHandler(IVersionMutationService service)
    : IRequestHandler<CloneVersionCommand, VersionMutationResult>
{
    /// <summary>
    /// Создаёт версию из заданной версии объекта.
    /// </summary>
    /// <param name="request">Параметры клонирования.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача с результатом операции клонирования.</returns>
    public Task<VersionMutationResult> Handle(CloneVersionCommand request, CancellationToken cancellationToken) =>
        service.CloneAsync(request.ObjectId, request.SourceVersion, request.ExpectedConcurrencyToken, cancellationToken);
}
