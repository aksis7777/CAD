using MediatR;
using MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;
using MiniPdm.Modules.BackgroundTasks.Abstractions;

namespace MiniPdm.Modules.BackgroundTasks.Features.Commands.UpdateBackgroundTaskSchedule;

/// <summary>
/// Представляет запрос на изменение интервала запуска фоновой задачи.
/// </summary>
/// <param name="TaskId">Идентификатор задачи.</param>
/// <param name="IntervalMinutes">Новый интервал запуска в минутах.</param>
public sealed record UpdateBackgroundTaskScheduleCommand(string TaskId, int IntervalMinutes) : IRequest<BackgroundTaskDto?>;

/// <summary>
/// Передаёт запрос изменения расписания координатору.
/// </summary>
/// <param name="coordinator">Координатор фоновых задач.</param>
public sealed class UpdateBackgroundTaskScheduleCommandHandler(IBackgroundTaskCoordinator coordinator)
    : IRequestHandler<UpdateBackgroundTaskScheduleCommand, BackgroundTaskDto?>
{
    /// <summary>
    /// Обрабатывает запрос изменения интервала запуска.
    /// </summary>
    /// <param name="request">Запрос с идентификатором задачи и интервалом.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Обновлённые сведения о задаче или <see langword="null"/>, если задача неизвестна.</returns>
    public Task<BackgroundTaskDto?> Handle(UpdateBackgroundTaskScheduleCommand request, CancellationToken cancellationToken) =>
        coordinator.UpdateScheduleAsync(request.TaskId, request.IntervalMinutes, cancellationToken);
}
