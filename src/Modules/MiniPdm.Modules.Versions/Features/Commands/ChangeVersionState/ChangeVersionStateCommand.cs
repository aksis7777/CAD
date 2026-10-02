using MediatR;
using MiniPdm.Domain.Versions.Mutations;
using MiniPdm.Domain.Versions;
using MiniPdm.Modules.Versions.Abstractions;

namespace MiniPdm.Modules.Versions.Features.Commands.ChangeVersionState;

/// <summary>
/// Запрашивает изменение состояния существующей версии объекта.
/// </summary>
/// <param name="ObjectId">Идентификатор объекта.</param>
/// <param name="Version">Номер изменяемой версии.</param>
/// <param name="State">Новое состояние версии.</param>
/// <param name="ExpectedConcurrencyToken">Ожидаемый токен конкурентного изменения объекта.</param>
public sealed record ChangeVersionStateCommand(Guid ObjectId, int Version, VersionState State,
    Guid ExpectedConcurrencyToken) : IRequest<VersionMutationResult>;

/// <summary>
/// Передаёт запрос изменения состояния сервису мутаций версий.
/// </summary>
/// <param name="service">Сервис выполнения мутаций версии.</param>
public sealed class ChangeVersionStateCommandHandler(IVersionMutationService service)
    : IRequestHandler<ChangeVersionStateCommand, VersionMutationResult>
{
    /// <summary>
    /// Изменяет состояние указанной версии.
    /// </summary>
    /// <param name="request">Параметры изменения состояния.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача с результатом мутации.</returns>
    public Task<VersionMutationResult> Handle(ChangeVersionStateCommand request, CancellationToken cancellationToken) =>
        service.ChangeStateAsync(request.ObjectId, request.Version, request.State,
            request.ExpectedConcurrencyToken, cancellationToken);
}
