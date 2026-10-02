using MediatR;
using MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;
using MiniPdm.Modules.BackgroundTasks.Abstractions;

namespace MiniPdm.Modules.BackgroundTasks.Features.Commands.UpdateBackgroundTaskSchedule;

public sealed class UpdateBackgroundTaskScheduleCommandHandler(IBackgroundTaskCoordinator coordinator)
    : IRequestHandler<UpdateBackgroundTaskScheduleCommand, BackgroundTaskDto?>
{
    public Task<BackgroundTaskDto?> Handle(UpdateBackgroundTaskScheduleCommand request, CancellationToken cancellationToken) =>
        coordinator.UpdateScheduleAsync(request.TaskId, request.IntervalMinutes, cancellationToken);
}
