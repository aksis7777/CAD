using MediatR;
using MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;
using MiniPdm.Modules.BackgroundTasks.Abstractions;

namespace MiniPdm.Modules.BackgroundTasks.Features.ListBackgroundTasks;

public sealed class ListBackgroundTasksQueryHandler(IBackgroundTaskCoordinator coordinator)
    : IRequestHandler<ListBackgroundTasksQuery, IReadOnlyList<BackgroundTaskDto>>
{
    public Task<IReadOnlyList<BackgroundTaskDto>> Handle(ListBackgroundTasksQuery request, CancellationToken cancellationToken) =>
        coordinator.ListAsync(cancellationToken);
}
