using MediatR;
using MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;
using MiniPdm.Modules.BackgroundTasks.Abstractions;

namespace MiniPdm.Modules.BackgroundTasks.Features.Queries.ListBackgroundTasks;

public sealed record ListBackgroundTasksQuery : IRequest<IReadOnlyList<BackgroundTaskDto>>;

public sealed class ListBackgroundTasksQueryHandler(IBackgroundTaskCoordinator coordinator)
    : IRequestHandler<ListBackgroundTasksQuery, IReadOnlyList<BackgroundTaskDto>>
{
    public Task<IReadOnlyList<BackgroundTaskDto>> Handle(ListBackgroundTasksQuery request, CancellationToken cancellationToken) =>
        coordinator.ListAsync(cancellationToken);
}
