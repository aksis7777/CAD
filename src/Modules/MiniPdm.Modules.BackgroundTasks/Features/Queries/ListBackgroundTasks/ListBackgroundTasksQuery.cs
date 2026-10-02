using MediatR;
using MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;

namespace MiniPdm.Modules.BackgroundTasks.Features.Queries.ListBackgroundTasks;

public sealed record ListBackgroundTasksQuery : IRequest<IReadOnlyList<BackgroundTaskDto>>;
