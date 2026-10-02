using MediatR;
using MiniPdm.Modules.BackgroundTasks.Abstractions;

namespace MiniPdm.Modules.BackgroundTasks.Features.Commands.RunBackgroundTask;

public sealed class RunBackgroundTaskCommandHandler(IBackgroundTaskCoordinator coordinator)
    : IRequestHandler<RunBackgroundTaskCommand, BackgroundTaskRunRequestResult>
{
    public Task<BackgroundTaskRunRequestResult> Handle(RunBackgroundTaskCommand request, CancellationToken cancellationToken) =>
        coordinator.RequestManualRunAsync(request.TaskId, cancellationToken);
}
